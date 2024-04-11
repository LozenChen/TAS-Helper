using TAS;
using TAS.EverestInterop;
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
                CenterCamera.RestoreTheCamera();
                Savestates.Load();
                CenterCamera.CenterTheCamera();
            }
            else {
                Controller.RefreshInputs(true);
            }
            if (delayedClear) {
                Savestates.Clear(); // clear it after RefreshInputs
            }

            Controller.NextCommentFastForward = new FastForward(frame, "", 0);
            Manager.States &= ~StudioCommunication.States.FrameStep;
            Manager.NextStates &= ~StudioCommunication.States.FrameStep;

        }
    }
}