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
    private static readonly System.Diagnostics.Stopwatch pumpClock = System.Diagnostics.Stopwatch.StartNew();
    private static long nextDrawAt;
    private static bool pumpDrawDisabled;

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
        // Draw at ~60fps: the highlight pulse is wall-clock based, and redrawing/Presenting the
        // whole scene hundreds of times per second while stopped is just wasted CPU/GPU.
        long now = pumpClock.ElapsedMilliseconds;
        if (!pumpDrawDisabled && now >= nextDrawAt) {
            nextDrawAt = now + 16;
            try {
                Game game = Engine.Instance;
                FNAPlatform.PollEvents?.Invoke(game, ref game.currentAdapter, game.textInputControlDown, ref game.textInputSuppress);
                ClearUpdateAtDrawLists();
                GameTime gameTime = new(game.gameTime.TotalGameTime, TimeSpan.Zero);
                game.Draw(gameTime);
                game.GraphicsDevice.Present();
                game.accumulatedElapsedTime = TimeSpan.Zero;
                pumps++;
            }
            catch (System.Exception e) {
                // Pumping Draw from inside Engine.Update is re-entrant: some mods (e.g. MotionSmoothing's
                // frame uncap) update scene objects during Draw and can throw. Stepping itself does not
                // need Draw, so disable visual pumping instead of aborting the OoO run.
                pumpDrawDisabled = true;
                Logger.Log(LogLevel.Warn, "OoO", "pump Draw disabled after exception: " + e);
            }
        }
        PollKeys();
        Thread.Sleep(4);
    }

    // MotionSmoothing's FrameUncap draws by updating the renderers/entities recorded during the previous
    // EngineUpdate ("update at draw"). Re-entering that from our pump while a TAS frame is frozen lets
    // those entity updates mutate the lists being enumerated -> "Collection was modified". Clearing the
    // recorded lists before the pumped draw makes it a pure draw; MotionSmoothing repopulates them on the
    // next real EngineUpdate.
    private static bool updateAtDrawChecked;
    private static object updateAtDrawInstance;
    private static System.Reflection.FieldInfo updateAtDrawRenderers;
    private static System.Reflection.FieldInfo updateAtDrawEntities;

    private static void ClearUpdateAtDrawLists() {
        if (!updateAtDrawChecked) {
            updateAtDrawChecked = true;
            try {
                Type? updateAtDraw = ModUtils.GetType("MotionSmoothing", "Celeste.Mod.MotionSmoothing.Utilities.UpdateAtDraw");
                Type? toggleable = updateAtDraw?.BaseType;
                updateAtDrawInstance = toggleable?.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
                if (updateAtDrawInstance is not null) {
                    updateAtDrawRenderers = updateAtDraw!.GetField("_renderersToUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    updateAtDrawEntities = updateAtDraw.GetField("_entitiesToUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                }
            }
            catch (System.Exception e) {
                Logger.Log(LogLevel.Debug, "OoO", "MotionSmoothing update-at-draw hook not available: " + e.Message);
            }
        }
        try {
            if (updateAtDrawRenderers?.GetValue(updateAtDrawInstance) is System.Collections.IList renderers) {
                renderers.Clear();
            }
            if (updateAtDrawEntities?.GetValue(updateAtDrawInstance) is System.Collections.IList entities) {
                entities.Clear();
            }
        }
        catch {
            // optional mod compatibility must never break stepping
        }
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
