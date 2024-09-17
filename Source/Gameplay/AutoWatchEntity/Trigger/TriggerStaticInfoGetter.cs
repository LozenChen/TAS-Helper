using Celeste.Mod.Entities;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Runtime.CompilerServices;
using static Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity.TriggerInfoHelper;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class TriggerStaticInfoGetter {

    public static string FlagTrigger(FlagTrigger flagTrigger) {
        return (flagTrigger.state ? "Add: " : "Remove: ") + flagTrigger.flag;
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
        return noRefillTrigger.State ? "ON" : "OFF"; ;
    }

    public static string OshiroTrigger(OshiroTrigger oshiroTrigger) {
        return oshiroTrigger.State ? "ON" : "OFF"; ;
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
}

internal static class ModTriggerStaticInfo {

    public static void AddToDictionary() {
        HandleVivHelper();
        HandleXaphanHelper();
        HandleFlagslinesAndSuch();
    }

    public static void Add(Type type, TriggerStaticHandler handler) {
        TriggerInfoHelper.StaticInfoGetters.TryAdd(type, handler);
    }

    public static void HandleVivHelper() {
        // not finished

        if (ModUtils.GetType("VivHelper", "VivHelper.Triggers.TeleportTarget") is not { } teleportTargetType) {
            return;
        }
        if (ModUtils.GetType("VivHelper", "VivHelper.Triggers.InstantTeleportTrigger1Way") is not { } teleport1wayType) {
            return;
        }

        Add(teleport1wayType, (trigger, level) => {
            if (!level.Tracker.Entities.ContainsKey(teleportTargetType)) {
                return "";
            }
            string targetLevel = trigger.GetFieldValue<string>("specificRoom");
            string targetID = trigger.GetFieldValue<string>("targetID");
            if (targetLevel is null || targetID is null) {
                return ""; // possible if it's a delayed awake? ... i don't want to deal with this case
            }

            string flagsSet = string.Join(", ", trigger.GetFieldValue<string[]>("flagsSet")?.Where(x => !string.IsNullOrWhiteSpace(x)) ?? new string[] { });
            string setFlag = string.IsNullOrEmpty(flagsSet) ? "" : $"SetFlag: {flagsSet}";
            string flagsNeeded = string.Join(", ", trigger.GetFieldValue<string[]>("flagsNeeded")?.Where(x => !string.IsNullOrWhiteSpace(x)) ?? new string[] { });
            string needFlag = string.IsNullOrEmpty(flagsNeeded) ? "" : $"NeedFlag: {flagsNeeded}";

            string teleportTarget;
            if (level.Session.Level == targetLevel) {
                foreach (Entity e in level.Tracker.Entities[teleportTargetType]) {
                    if (e.GetFieldValue<string>("targetID") == targetID) {
                        if (e.GetFieldValue<bool>("addTriggerOffset")) {
                            // we align to the bottom of maddy, which is, the position of maddy
                            teleportTarget = (e.TopLeft + new Vector2(4f, 11f) + (trigger.Center - trigger.TopLeft)).IntVector2ToString() + " + offset";
                        }
                        else {
                            teleportTarget = (e.Center + new Vector2(0f, 5.5f)).IntVector2ToString();
                        }
                        break;
                    }
                }
                teleportTarget = targetID;
            }
            else {
                teleportTarget = $"[{targetLevel}] {targetID}";
            }
            if (setFlag != "") {
                teleportTarget += "\n" + setFlag;
            }
            if (needFlag != "") {
                teleportTarget += "\n" + needFlag;
            }
            return teleportTarget;
        });
    }
    public static void HandleXaphanHelper() {
        // not finished

        /* just for test
        if (ModUtils.GetType("XaphanHelper", "Celeste.Mod.XaphanHelper.Triggers.TextTrigger") is { } textTrigger) {
            Add(textTrigger, (trigger, _) => {
                return "-" + Dialog.Clean(trigger.GetFieldValue<string>("dialogID")) + "-";
            });
        }
        */
    }

    public static void HandleFlagslinesAndSuch() {
        // not finished

        if (ModUtils.GetType("FlaglinesAndSuch", "FlaglinesAndSuch.FlagLogicGate") is { } flagLogicGate) {
            Add(flagLogicGate, (trigger, _) => {
                string flag1 = trigger.GetFieldValue<string>("flag1");
                string flag2 = trigger.GetFieldValue<string>("flag2");
                bool[] logicTable = trigger.GetFieldValue<bool[]>("WorkableCases");
                string setFlag = (trigger.GetFieldValue<bool>("setState") ? "Add: " : "Remove: ") + trigger.GetFieldValue<string>("setFlag");
                return ParseLogicGate(logicTable, flag1, flag2) + "\n" + setFlag;
            });
        }

        static string ParseLogicGate(bool[] logicTable, string flag1, string flag2) {
            bool case00 = logicTable[0];
            bool case01 = logicTable[1];
            bool case10 = logicTable[2];
            bool case11 = logicTable[3];
            return (case00, case01, case10, case11) switch {
                (false, false, false, false) => "Never",
                (false, false, false, true) => $"Flag1: {flag1}\nFlag2: {flag2}\nWhen: Flag1 && Flag2",
                (false, false, true, false) => $"Flag1: {flag1}\nFlag2: {flag2}\nWhen: Flag1 && !Flag2",
                (false, true, false, false) => $"Flag1: {flag1}\nFlag2: {flag2}\nWhen: !Flag1 && Flag2",
                (true, false, false, false) => $"Flag1: {flag1}\nFlag2: {flag2}\nWhen: !Flag1 && !Flag2",
                (true, true, false, false) => $"Flag: {flag1}\nWhen: !Flag",
                (false, false, true, true) => $"Flag: {flag1}\nWhen: Flag",
                (true, false, true, false) => $"Flag: {flag2}\nWhen: !Flag",
                (false, true, false, true) => $"Flag: {flag2}\nWhen: Flag",
                (true, false, false, true) => $"Flag1: {flag1}\nFlag2: {flag2}\nWhen: Flag1 == Flag2",
                (false, true, true, false) => $"Flag1: {flag1}\nFlag2: {flag2}\nWhen: Flag1 Xor Flag2",
                (true, true, true, false) => $"Flag1: {flag1}\nFlag2: {flag2}\nWhen: !Flag1 || !Flag2",
                (true, true, false, true) => $"Flag1: {flag1}\nFlag2: {flag2}\nWhen: !Flag1 || Flag2",
                (true, false, true, true) => $"Flag1: {flag1}\nFlag2: {flag2}\nWhen: Flag1 || !Flag2",
                (false, true, true, true) => $"Flag1: {flag1}\nFlag2: {flag2}\nWhen: Flag1 || Flag2",
                (true, true, true, true) => "Always",
            };
        }
    }
}