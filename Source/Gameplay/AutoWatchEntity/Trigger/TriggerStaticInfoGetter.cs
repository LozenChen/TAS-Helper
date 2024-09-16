using Celeste.Mod.Entities;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class TriggerStaticInfoGetter {

    public static string FlagTrigger(FlagTrigger flagTrigger, Level level) {
        if (level.Session.GetFlag(flagTrigger.flag) ^ flagTrigger.state) {
            return (flagTrigger.state ? "Add: " : "Remove: ") + flagTrigger.flag;
        }
        return "";
    }

    public static string EventTrigger(EventTrigger eventTrigger) {
        return eventTrigger.Event;
    }

    public static string CameraOffsetTrigger(CameraOffsetTrigger cameraOffsetTrigger) {
        return cameraOffsetTrigger.CameraOffset.IntVector2ToString();
    }

    public static string SmoothCameraOffsetTrigger(SmoothCameraOffsetTrigger smoothCameraOffsetTrigger) {
        if (smoothCameraOffsetTrigger.xOnly) {
            if (smoothCameraOffsetTrigger.yOnly) {
                return "";
            }
            return $"X: {smoothCameraOffsetTrigger.offsetFrom.X.SignedIntToString()} -> {smoothCameraOffsetTrigger.offsetTo.X.SignedIntToString()}";
        }
        if (smoothCameraOffsetTrigger.yOnly) {
            return $"Y: {smoothCameraOffsetTrigger.offsetFrom.Y.SignedIntToString()} -> {smoothCameraOffsetTrigger.offsetTo.Y.SignedIntToString()}";
        }
        return $"{smoothCameraOffsetTrigger.offsetFrom.IntVector2ToString()} -> {smoothCameraOffsetTrigger.offsetTo.IntVector2ToString()}";
    }

    public static string ChangeRespawnTrigger(ChangeRespawnTrigger changeRespawnTrigger) {
        return changeRespawnTrigger.Target.IntVector2ToString();
    }

    public static string CameraTargetTrigger(CameraTargetTrigger cameraTargetTrigger) {
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

    public static string CameraAdvanceTargetTrigger(CameraAdvanceTargetTrigger cameraAdvanceTargetTrigger) {
        if (cameraAdvanceTargetTrigger.XOnly) {
            if (cameraAdvanceTargetTrigger.YOnly) {
                return "";
            }
            return "X: " + cameraAdvanceTargetTrigger.X.SignedIntToString();
        }
        else if (cameraAdvanceTargetTrigger.YOnly) {
            return "Y: " + cameraAdvanceTargetTrigger.Y.SignedIntToString();
        }
        return cameraAdvanceTargetTrigger.Target.IntVector2ToString();
    }

    public static string NoRefillTrigger(NoRefillTrigger noRefillTrigger) {
        return StateToString(noRefillTrigger.State);
    }

    public static string OshiroTrigger(OshiroTrigger oshiroTrigger) {
        return StateToString(oshiroTrigger.State);
    }

    public static string WindTrigger(WindTrigger windTrigger) {
        return windTrigger.Pattern.ToString();
    }

    public static string ChangeInventoryTrigger(ChangeInventoryTrigger changeInventoryTrigger) {
        string dash = changeInventoryTrigger.inventory.Dashes switch {
            0 => "Dashless",
            1 => "SingleDash",
            2 => "TwoDashes",
            _ => $"{changeInventoryTrigger.inventory.Dashes} Dashes"
        };
        if (changeInventoryTrigger.inventory.NoRefills) {
            return dash + " + NoRefill";
        }
        return dash;
        // we don't care if maddy has dreamdash or has backpack
    }

    public static string CoreModeTrigger(CoreModeTrigger coreModeTrigger) {
        return coreModeTrigger.mode.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string StateToString(bool state) {
        return state ? "ON" : "OFF";
    }
}