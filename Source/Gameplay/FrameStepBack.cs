using Celeste.Mod.TASHelper.ModInterop;
using Celeste.Mod.TASHelper.Module.Menu;
using TAS;
using TAS.Input;

namespace Celeste.Mod.TASHelper.Gameplay;

// Called [Inverse Frame Advance] in menu
public static class FrameStepBack {

    public static InputController Controller => Manager.Controller;

    public static void StepBackOneFrame() {
        SetupNextFastForward(-1);
    }
    public static void SetupNextFastForward(int relativeMove) {
        // todo: fix the random camera issue

        if (Manager.Running && !CelesteTasImports.IsTasRecording()) {
            int frame = Controller.CurrentFrameInTas + relativeMove;
            if (frame <= 0) {
                return;
            }
            if (CelesteTasImports.GetLatestSavestateForFrame(frame) is { } state) {
                CelesteTasImports.LoadSavestate(state); // only the nearest savestate breakpoint will work
            }
            else {
                Controller.Stop(); // Controller.Stop() is no longer contained in current version of RefreshInputs(true)
                Controller.RefreshInputs(true);
            }
            Controller.NextLabelFastForward = new FastForward(frame, 0, true, false);
            // doesn't work well
            ForwardTarget = frame;
        }
    }


    internal static int ForwardTarget = 0;

    public static bool CheckOnHotkeyHold() {
        return OnInterval((int)Math.Round(1 / TasSettings.SlowForwardSpeed)) && frameStepBackHoldTimer > 60;
    }

    private static int frameCounter = 0;

    private static int frameStepBackHoldTimer = 0;

    internal static void OnHotkeyUpdate(bool check) {
        frameCounter++;
        if (check) {
            frameStepBackHoldTimer++;
        }
        else {
            frameStepBackHoldTimer = 0;
            if (!Manager.FastForwarding) {
                ForwardTarget = 0;
            }
        }
        if (!ForwardingIsDone()) {
            TH_Hotkeys.FrameStepBack.Update(false, false);
            TH_Hotkeys.FrameStepBack.Update(false, false);
        }
    }
    private static bool ForwardingIsDone() {
        return Controller.CurrentFrameInTas >= ForwardTarget;
    }

    private static bool OnInterval(int period) {
        return frameCounter % period == 0u;
    }
}