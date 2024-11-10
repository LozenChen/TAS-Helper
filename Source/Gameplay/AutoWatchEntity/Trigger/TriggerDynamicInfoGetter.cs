using Celeste.Mod.Entities;
using Celeste.Mod.TASHelper.Utils;
using static Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity.TriggerInfoHelper;

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


internal static class ModTriggerDynamicInfo {

    public static void AddToDictionary() {
        HandleMemorialHelper();
        HandleSardine7();
        HandleAurorasHelper();
    }
    public static void Add(Type type, TriggerDynamicPlayerlessHandler handler) {
        TriggerInfoHelper.DynamicInfoPlayerlessGetters.TryAdd(type, handler);
    }

    public static void Add(Type type, TriggerDynamicPlayerHandler handler) {
        TriggerInfoHelper.DynamicInfoPlayerGetters.TryAdd(type, handler);
    }

    public static void HandleMemorialHelper() {
        if (ModUtils.GetType("memorialHelper", "Celeste.Mod.MemorialHelper.DashSequenceFlagTrigger") is { } dashSequenceFlagTrigger) {
            Add(dashSequenceFlagTrigger, (trigger, level, _) => {
                string flag = trigger.GetFieldValue<string>("flag");
                if (trigger.GetFieldValue<bool>("triggered")) {
                    if (trigger.GetFieldValue<bool>("persistent")) {
                        return "Added: " + flag + "_dashFlag";
                    }
                    else {
                        return "Added: " + flag;
                    }
                }
                int currentPoint = trigger.GetFieldValue<int>("currentPoint");
                List<int> dashList = trigger.GetFieldValue<List<int>>("dashList");
                string codeState = (currentPoint >= dashList.Count || currentPoint < 0)
                    ? $"[{currentPoint}/{dashList.Count}], Next: ?"
                    : $"[{currentPoint}/{dashList.Count}], Next: {DashCode.ToCode(dashList[currentPoint], DashCode.MemorialHelperOffset)}";

                if (trigger.GetFieldValue<bool>("repeatable")) {
                    if (level.Session.GetFlag(flag)) {
                        return codeState + "\nRemove: " + flag;
                    }
                    else {
                        return codeState + "\nAdd: " + flag;
                    }
                }
                else {
                    if (trigger.GetFieldValue<bool>("persistent")) {
                        return codeState + "\nAdd: " + flag + "_dashFlag";
                    }
                    else {
                        return codeState + "\nAdd: " + flag;
                    }
                }
            });
        }
    }

    public static void HandleSardine7() {
        if (ModUtils.GetType("Sardine7", "Celeste.Mod.Sardine7.Triggers.DashCodeTrigger") is { } dashCodeTrigger) {
            Add(dashCodeTrigger, (trigger, level) => {
                string flag = trigger.GetFieldValue<string>("flag");
                bool flagValue = trigger.GetFieldValue<bool>("flagValue");
                if (level.Session.GetFlag(flag) == flagValue) {
                    return $"{(flagValue ? "Added: " : "Removed: ")}{flag}";
                }
                List<string> currentInputs = trigger.GetFieldValue<List<string>>("currentInputs");
                // techinically, your last dash need to be inside the trigger (so the trigger will be enabled)
                if (flagValue) {
                    return "Current: " + string.Join(",", currentInputs.Select(DashCode.ToCode)) + "\nAdd: " + flag;
                }
                else {
                    return "Current: " + string.Join(",", currentInputs.Select(DashCode.ToCode)) + "\nRemove: " + flag;
                }
            });
        }
    }

    public static void HandleAurorasHelper() {
        if (ModUtils.GetType("AurorasHelper", "Celeste.Mod.AurorasHelper.DashcodeHashTrigger") is { } hashedDashCode) {
            Add(hashedDashCode, (trigger, level) => {
                string flag = trigger.GetFieldValue<string>("flag");
                bool flagState = trigger.GetFieldValue<bool>("flag_state");
                if (level.Session.GetFlag(flag) == flagState) {
                    return $"{(flagState ? "Added: " : "Removed: ")}{flag}";
                }
                List<string> currentInputs = trigger.GetFieldValue<List<string>>("currentInputs");
                // techinically, your last dash need to be inside the trigger (so the trigger will be enabled)
                if (flagState) {
                    return "Current: " + string.Join(",", currentInputs.Select(DashCode.ToCode)) + "\nAdd: " + flag;
                }
                else {
                    return "Current: " + string.Join(",", currentInputs.Select(DashCode.ToCode)) + "\nRemove: " + flag;
                }
            });
        }
    }
}