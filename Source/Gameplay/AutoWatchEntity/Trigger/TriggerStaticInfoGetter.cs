using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity.TriggerInfoHelper;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class TriggerStaticInfoGetter {

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
        HandleMemorialHelper();
        HandleSardine7();
        HandleContortHelper();
        HandleAurorasHelper();
        HandleCollabUtils2();
    }

    public static void Add(Type type, TriggerStaticHandler handler) {
        TriggerInfoHelper.StaticInfoGetters.TryAdd(type, handler);
    }

    public static void HandleVivHelper() {

        if (ModUtils.GetType("VivHelper", "VivHelper.Triggers.TeleportTarget") is not { } teleportTargetType) {
            return;
        }
        if (ModUtils.GetType("VivHelper", "VivHelper.Triggers.InstantTeleportTrigger") is not { } instantTeleportType) {
            return;
        }
        if (ModUtils.GetType("VivHelper", "VivHelper.Triggers.InstantTeleportTrigger1Way") is not { } teleport1wayType) {
            return;
        }

        Add(teleportTargetType, (trigger, _) => {
            return "id: " + trigger.GetFieldValue<string>("targetID");
        });

        Add(instantTeleportType, (trigger, level) => {
            string newRoom = trigger.GetFieldValue<string>("newRoom");
            Vector2 newPos = trigger.GetFieldValue<Vector2>("newPos");
            Vector2 array0 = trigger.Position - level.LevelOffset; // assume player's position = trigger's position
            LevelData targetLevel = level.Session.MapData.Get(newRoom);
            if (targetLevel is null) {
                return "";
            }
            Rectangle Bounds = targetLevel.Bounds;
            Vector2 LevelOffset = new Vector2(Bounds.Left, Bounds.Top);
            Vector2 vector;
            if (newPos.X < 0f || (newPos.X > (float)(Bounds.X + Bounds.Width) - LevelOffset.X) || newPos.Y < 0f || (newPos.Y > (float)(Bounds.Y + Bounds.Height) - LevelOffset.Y)) {
                vector = ((array0.X < 0f) || (array0.X > (float)(Bounds.X + Bounds.Width) - LevelOffset.X) || (array0.Y < 0f) || (array0.Y > (float)(Bounds.Y + Bounds.Height) - LevelOffset.Y)) ? (LevelOffset + new Vector2(1f, 1f)) : (LevelOffset + array0);
            }
            else {
                vector = LevelOffset + newPos; // we ignore "triggerAddOffset"
            }
            string result = vector.IntVector2ToString();
            string[] flags = trigger.GetFieldValue<string[]>("flags");
            if (flags.IsNotNullOrEmpty() && string.Join("", flags).IsNotNullOrEmpty()) {
                result += $"\nNeedFlag: {string.Join(", ", flags)}"; // techinically we need string.Join(" && ", flags)
            }
            return result;
        });

        Add(teleport1wayType, (trigger, level) => {
            if (!level.Tracker.Entities.TryGetValue(teleportTargetType, out List<Entity> list)) {
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
                foreach (Entity e in list) {
                    if (e.GetFieldValue<string>("targetID") == targetID) {
                        if (e.GetFieldValue<bool>("addTriggerOffset")) {
                            // we assume maddy's position = trigger's position
                            teleportTarget = (e.TopLeft + new Vector2(4f, 11f) + (trigger.Center - trigger.TopLeft)).IntVector2ToString();
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
        /* just for test
        if (ModUtils.GetType("XaphanHelper", "Celeste.Mod.XaphanHelper.Triggers.TextTrigger") is { } textTrigger) {
            Add(textTrigger, (trigger, _) => {
                return "-" + Dialog.Clean(trigger.GetFieldValue<string>("dialogID")) + "-";
            });
        }
        */
    }

    public static void HandleFlagslinesAndSuch() {
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
                (false, false, false, true) => $"Flag1: {flag1}\nFlag2: {flag2}\nIf: Flag1 && Flag2",
                (false, false, true, false) => $"Flag1: {flag1}\nFlag2: {flag2}\nIf: Flag1 && !Flag2",
                (false, true, false, false) => $"Flag1: {flag1}\nFlag2: {flag2}\nIf: !Flag1 && Flag2",
                (true, false, false, false) => $"Flag1: {flag1}\nFlag2: {flag2}\nIf: !Flag1 && !Flag2",
                (true, true, false, false) => $"Flag: {flag1}\nIf: !Flag",
                (false, false, true, true) => $"Flag: {flag1}\nIf: Flag",
                (true, false, true, false) => $"Flag: {flag2}\nIf: !Flag",
                (false, true, false, true) => $"Flag: {flag2}\nIf: Flag",
                (true, false, false, true) => $"Flag1: {flag1}\nFlag2: {flag2}\nIf: Flag1 == Flag2",
                (false, true, true, false) => $"Flag1: {flag1}\nFlag2: {flag2}\nIf: Flag1 Xor Flag2",
                (true, true, true, false) => $"Flag1: {flag1}\nFlag2: {flag2}\nIf: !Flag1 || !Flag2",
                (true, true, false, true) => $"Flag1: {flag1}\nFlag2: {flag2}\nIf: !Flag1 || Flag2",
                (true, false, true, true) => $"Flag1: {flag1}\nFlag2: {flag2}\nIf: Flag1 || !Flag2",
                (false, true, true, true) => $"Flag1: {flag1}\nFlag2: {flag2}\nIf: Flag1 || Flag2",
                (true, true, true, true) => "Always",
            };
        }

        if (ModUtils.GetType("FlaglinesAndSuch", "FlaglinesAndSuch.FlagIfFlag") is { } flagIfFlag) {
            Add(flagIfFlag, (trigger, _) => {
                string ifFlag = trigger.GetFieldValue<string>("ifFlag");
                string setFlag = trigger.GetFieldValue<string>("setFlag");
                bool ifState = trigger.GetFieldValue<bool>("ifState");
                bool setState = trigger.GetFieldValue<bool>("setState");
                return $"{(ifState ? "If: " : "If not: ")}{ifFlag}\n{(setState ? "Add: " : "Remove: ")}{setFlag}";
            });
        }
    }

    public static void HandleMemorialHelper() {
        if (ModUtils.GetType("memorialHelper", "Celeste.Mod.MemorialHelper.DashSequenceFlagTrigger") is { } dashSequenceFlagTrigger) {
            Add(dashSequenceFlagTrigger, (trigger, _) => {
                return "DashCode: " + string.Join(",", trigger.GetFieldValue<List<int>>("dashList").Select(x => DashCode.ToCode(x, DashCode.MemorialHelperOffset)));
            });
        }
    }

    public static void HandleSardine7() {
        if (ModUtils.GetType("Sardine7", "Celeste.Mod.Sardine7.Triggers.DashCodeTrigger") is { } dashCodeTrigger) {
            Add(dashCodeTrigger, (trigger, _) => {
                return "DashCode: " + string.Join(",", trigger.GetFieldValue<string[]>("code").Select(DashCode.ToCode));
            });
        }
    }

    public static void HandleContortHelper() {
        // not finished

        if (ModUtils.GetType("ContortHelper", "ContortHelper.TeleportationTrigger") is { } teleportationTrigger) {
            Add(teleportationTrigger, (trigger, _) => {
                Vector2? toTeleportTo = trigger.GetFieldValue<Vector2?>("toTeleportTo");
                if (toTeleportTo.HasValue) {
                    return ""; // when in same room, the mod itself already debugrender its target
                }
                string roomName = trigger.GetFieldValue<string>("roomName");
                string roomNameForGolden = trigger.GetFieldValue<string>("roomNameForGolden");
                string TargetTag = trigger.GetFieldValue<string>("TargetTag");
                string result;
                if (string.IsNullOrWhiteSpace(TargetTag)) {
                    result = (string.IsNullOrWhiteSpace(roomNameForGolden) || roomName == roomNameForGolden) ? $"[{roomName}]" : $"[{roomName}]\nIfGolden: [{roomNameForGolden}]";
                }
                else {
                    result = (string.IsNullOrWhiteSpace(roomNameForGolden) || roomName == roomNameForGolden) ? $"[{roomName}] {TargetTag}" : $"[{roomName}] {TargetTag}\nIfGolden: [{roomNameForGolden}]";
                }
                string[] flags = trigger.GetFieldValue<string[]>("neededFlags");
                if (flags.IsNotNullOrEmpty() && string.Join("", flags).IsNotNullOrEmpty()) {
                    result += $"\nNeedFlag: {string.Join(", ", flags)}";
                }
                return result;
            });
        }
    }

    public static void HandleAurorasHelper() {
        // not finished

        if (ModUtils.GetType("AurorasHelper", "Celeste.Mod.AurorasHelper.DashcodeHashTrigger") is { } hashedDashCode) {
            Add(hashedDashCode, (trigger, _) => {
                string hashedCode = trigger.GetFieldValue<string>("hashedCode");
                int length = trigger.GetFieldValue<int>("codeLength");
                if (DashCode.AurorasDashCode.TryGetInputs(hashedCode, length, out string inputs)) {
                    return "DashCode: " + inputs;
                }
                return "";
            });
        }
    }

    public static void HandleCollabUtils2() {
        // not finished

        if (ModUtils.GetType("CollabUtils2", "Celeste.Mod.CollabUtils2.Triggers.ChapterPanelTrigger") is { } chapterPanel) {
            Add(chapterPanel, (trigger, _) => {
                if (trigger.SourceData is EntityData data) {
                    string map = data.Attr("map");
                    bool exitFromGym = data.Name == "CollabUtils2/ExitFromGymTrigger";
                    return exitFromGym ? "GymExit" : map;
                }
                return "";
            });
        }
    }
}