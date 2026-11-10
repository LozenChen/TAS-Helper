using Microsoft.Xna.Framework;
using Monocle;
using System.Threading;
using TAS;

namespace Celeste.Mod.TASHelper.OrderOfOperation;

// New OoO engine: one OoO frame == one normal CelesteTAS FrameAdvance.
// Sub-steps are breakpoints injected as plain calls; when a breakpoint wants to stop,
// OoO_Suspender blocks the main thread and pumps events + drawing itself.
internal static class OoO_Suspender {
    internal static bool Blocking;
    internal static bool StepRequested;
    internal static bool FastForwardRequested;
    internal static bool AutoStepping;

    private static bool lastStepKey;
    private static bool lastFastForwardKey;
    private static int lastPauseKey;
    private static long nextAutoStepAt;
    private static int pumps;

    internal static void Block(string label) {
        global::Celeste.Mod.TASHelper.Entities.HotkeyWatcher.Refresh(label);
        Blocking = true;
        try {
            while (true) {
                PumpOnce();
                if (!OoO_Controller.Active) {
                    return;
                }
                if (FastForwardRequested) {
                    FastForwardRequested = false;
                    OoO_Controller.Skipping = true;
                    return;
                }
                if (StepRequested) {
                    StepRequested = false;
                    return;
                }
                AutoStepIfNeeded();
                Thread.Sleep(1);
            }
        }
        finally {
            Blocking = false;
        }
    }

    private static void AutoStepIfNeeded() {
        if (!AutoStepping) {
            return;
        }
        long now = System.Environment.TickCount64;
        if (now < nextAutoStepAt) {
            return;
        }
        nextAutoStepAt = now + 700;
        StepRequested = true;
    }

    private static void PumpOnce() {
        try {
            Game game = Engine.Instance;
            FNAPlatform.PollEvents?.Invoke(game, ref game.currentAdapter, game.textInputControlDown, ref game.textInputSuppress);
            GameTime gameTime = new(game.gameTime.TotalGameTime, TimeSpan.Zero);
            game.Draw(gameTime);
            game.GraphicsDevice.Present();
            game.accumulatedElapsedTime = TimeSpan.Zero;
            PollKeys();
            pumps++;
        }
        catch (System.Exception e) {
            Logger.Log("OoO2Probe", "pump error: " + e);
            OoO_Controller.Exit();
        }
        Thread.Sleep(1);
    }

    private static void PollKeys() {
        var kb = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        bool ctrl = kb.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl);
        bool stepKey = ctrl && kb.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.G);
        bool fastForwardKey = ctrl && kb.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Y);
        if (stepKey && !lastStepKey) {
            StepRequested = true;
        }
        if (fastForwardKey && !lastFastForwardKey) {
            FastForwardRequested = true;
        }
        lastStepKey = stepKey;
        lastFastForwardKey = fastForwardKey;

        // While blocked the normal TAS hotkeys are not polled (Level.Render of the game loop is stuck);
        // read the TAS bindings directly so frame advance / pause still exits OoO safely.
        if (TasHelperSettings.EnableOoO) {
            if (HotkeyPressed(TasSettings.KeyPause) ||
                HotkeyPressed(TasSettings.KeyFrameAdvance)) {
                FastForwardRequested = true;
                OoO_Controller.ExitAfterFrame = true;
            }
        }
    }

    private static bool HotkeyPressed(ButtonBinding binding) {
        if (binding is null || binding.Keys is null) {
            return false;
        }
        var kb = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        foreach (var key in binding.Keys) {
            if (!kb.IsKeyDown(key)) {
                return false;
            }
        }
        return true;
    }
}
