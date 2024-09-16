using Celeste.Mod.Entities;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class TriggerDynamicInfoGetter {
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