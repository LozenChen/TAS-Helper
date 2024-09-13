using Celeste.Mod.Entities;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class TriggerHelper {

    // todo : use dictionary instead
    public static string GetStaticInfo(Trigger trigger) {
        if (trigger.Scene is not Level level) {
            return "";
        }
        if (trigger is FlagTrigger flagTrigger) {
            if (level.Session.GetFlag(flagTrigger.flag) ^ flagTrigger.state) {
                return (flagTrigger.state ? "Add: " : "Remove: ") + flagTrigger.flag;
            }
        }
        else if (trigger is EventTrigger eventTrigger) {
            return eventTrigger.Event;
        }
        else if (trigger is CameraOffsetTrigger cameraOffsetTrigger) {
            return cameraOffsetTrigger.CameraOffset.IntVector2ToString();
        }
        else if (trigger is ChangeRespawnTrigger changeRespawnTrigger) {
            return changeRespawnTrigger.Target.IntVector2ToString();
        }
        else if (trigger is CameraTargetTrigger cameraTargetTrigger) {
            if (cameraTargetTrigger.XOnly) {
                if (cameraTargetTrigger.YOnly) {
                    return "";
                }
                return "X: " + cameraTargetTrigger.X.SignedIntToString();
            }
            else if (cameraTargetTrigger.YOnly) {
                return "Y: " + cameraTargetTrigger.Y.SignedIntToString();
            }
            return cameraTargetTrigger.Target.IntVector2ToString();
        }
        return "";
    }

    public static string GetDynamicInfo(Trigger trigger) {
        if (trigger.Scene is not Level level || playerInstance is not { } player || player.StateMachine.State == 18 || !trigger.CollideCheck(player)) {
            return "";
        }
        if (trigger is CameraTargetTrigger cameraTargetTrigger) {
            if (cameraTargetTrigger.XOnly && cameraTargetTrigger.YOnly) {
                return "";
            }
            return "Lerp: " + player.CameraAnchorLerp.X.AbsoluteFloatToString();
        }
        return "";
    }
}