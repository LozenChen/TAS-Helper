using Celeste.Mod.Entities;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class TriggerDynamicInfoGetter {

    // rule : if it does not have a "Player" parameter, then it will show even if player is not inside
    public static string FlagTrigger(FlagTrigger flagTrigger, Level level) {
        string str;
        if (level.Session.GetFlag(flagTrigger.flag) == flagTrigger.state) {
            str = (flagTrigger.state ? "Added: " : "Removed: ") + flagTrigger.flag;
        }
        else {
            str = (flagTrigger.state ? "Add: " : "Remove: ") + flagTrigger.flag;
        }
        if (flagTrigger.deathCount >= 0) {
            str += $"\nNeedDeath: {level.Session.DeathsInCurrentLevel} == {flagTrigger.deathCount}";
        }
        return str;
    }
    public static string CameraTargetTrigger(CameraTargetTrigger cameraTargetTrigger, Player player) {
        if (cameraTargetTrigger.XOnly && cameraTargetTrigger.YOnly) {
            return "";
        }
        return "Lerp: " + player.CameraAnchorLerp.X.AbsoluteFloatToString();
    }

    public static string CameraAdvanceTargetTrigger(CameraAdvanceTargetTrigger cameraAdvanceTargetTrigger, Player player) {
        if (cameraAdvanceTargetTrigger.XOnly && cameraAdvanceTargetTrigger.YOnly) {
            return "";
        }
        if (cameraAdvanceTargetTrigger.XOnly) {
            return "Lerp.X: " + player.CameraAnchorLerp.X.AbsoluteFloatToString();
        }
        if (cameraAdvanceTargetTrigger.YOnly) {
            return "Lerp.Y: " + player.CameraAnchorLerp.Y.AbsoluteFloatToString();
        }
        return "Lerp: " + player.CameraAnchorLerp.FloatVector2ToString();
    }

    public static string SmoothCameraOffsetTrigger(SmoothCameraOffsetTrigger smoothCameraOffsetTrigger, Player player) {
        if (smoothCameraOffsetTrigger.xOnly && smoothCameraOffsetTrigger.yOnly) {
            return "";
        }
        return "Lerp: " + smoothCameraOffsetTrigger.GetPositionLerp(player, smoothCameraOffsetTrigger.positionMode).AbsoluteFloatToString();
    }
}