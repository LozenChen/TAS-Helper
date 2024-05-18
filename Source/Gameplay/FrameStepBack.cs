using Celeste.Mod.TASHelper.Module.Menu;
using TAS;
using TAS.Input;

namespace Celeste.Mod.TASHelper.Gameplay;
public static class FrameStepBack {

    public static InputController Controller => Manager.Controller;

    public static void StepBackOneFrame() {
        SetupNextFastForward(-1);
    }
    public static void SetupNextFastForward(int relativeMove) {
        if (Manager.Running && !Manager.Recording) {
            int frame = Controller.CurrentFrameInTas + relativeMove;
            if (frame <= 0) {
                return;
            }
            bool isLoad = false;
            bool delayedClear = false;
            if (Savestates.IsSaved_Safe()) {
                isLoad = Savestates.SavedCurrentFrame <= frame;
                delayedClear = true;
            }
            if (isLoad) {
                Savestates.Load();
            }
            else {
                Controller.RefreshInputs(true);
            }
            if (delayedClear) {
                Savestates.Clear(); // the savestate is after us, clear it after RefreshInputs, so we will not run to the savestate breakpoint instead
            }

            Controller.NextCommentFastForward = new FastForward(frame, "", 0);
            Manager.States &= ~StudioCommunication.States.FrameStep;
            Manager.NextStates &= ~StudioCommunication.States.FrameStep;
            ForwardTarget = frame;
        }
    }

    internal static int ForwardTarget = 0;

    public static bool CheckOnHotkeyHold() {
        return OnInterval((int)Math.Round(4 / TasSettings.SlowForwardSpeed)) && frameStepBackHoldTimer > 60;
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
            if (!Manager.UltraFastForwarding) {
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