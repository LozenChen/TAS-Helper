//#define OoO_Debug

using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;
using TAS.EverestInterop;
using TAS.EverestInterop.Hitboxes;
using static Celeste.Mod.TASHelper.OrderOfOperation.OoO_Core;

namespace Celeste.Mod.TASHelper.OrderOfOperation;


internal static class ForEachBreakPoints_EntityList {

    private static readonly HashSet<string> targets = new();

    private static readonly HashSet<string> targets_withBreakpoints = new();

    private static readonly HashSet<string> targets_removed_from_each = new();

    private static readonly HashSet<Entity> removed_targets = new();

    internal static string curr_target_withBreakpoint;

    internal static string curr_target_withoutBreakpoint;

    private static int passed_targets = 0;

    private static int expected_passed_targets = 0;

    internal static bool ultraFastForwarding = false;

    public const string eachString = "Each";

    private static bool checkEach = false;

    public const string autoStopString = "Auto";

    private static bool autoStop = true;

    private static void OnEntityListUpdate(On.Monocle.EntityList.orig_Update orig, EntityList self) {
        MainBody(self); // we assume no other mod hooks here (except CelesteTAS)
        return;
    }

    internal static bool firstEnter = true;

    private static void MainBody(EntityList entityList) {
        if (firstEnter) {
            ActualEntityCollideHitbox.Clear();
            firstEnter = false;
            return;
        }

        PlayerData? oldData = null;
        Player player = playerInstance;
        if (!ultraFastForwarding && autoStop && player is not null) {
            oldData = new PlayerData(player);
        }

        foreach (Entity entity in entityList.entities) {
            if (IsGotoContinue(entity)) {
                continue;
            }
            if (IsRunNormally(entity)) {
                if (oldData is not null) {
                    entity._PreUpdate();
                    if (entity.Active) {
                        entity.Update();
                    }
                    entity._PostUpdate();
#pragma warning disable CS8604
                    PlayerData newData = new PlayerData(player);
#pragma warning restore CS8604

                    if (!newData.Equals(oldData.Value)) {
                        trackedEntity = entity;
                        SendText($"{localvar_entityId} Update");
                        removed_targets.Add(entity);
                        if (prepareToUltraFastForwarding) {
                            ultraFastForwarding = true;
                        }
                        return;
                    }
                }
                else {
                    entity._PreUpdate();
                    if (entity.Active) {
                        entity.Update();
                    }
                    entity._PostUpdate();
                }
            }
            else {
                bool gotoReturn = TargetEntityUpdate(entity);
                if (gotoReturn) {
                    return;
                }
            }
        }

        CompleteEvent();
    }

    private static string localvar_entityId;

    private static string localvar_entityType;

    private static bool localvar_contain;

    private static void CompleteEvent() {
        SendText("SceneUpdate_EntityListUpdate end");
        EntityListUpdate_Entry.SubMethodPassed = true;
        Reset();
    }

    public static string GetID(Entity entity) {
        return entity.GetType().Name;
    }

    public static string GetUID(Entity entity) {
        if (entity.GetEntityData()?.ToEntityId().ToString() is { } id) {
            return $"{entity.GetType().Name}[{id}]";
        }
        return $"{entity.GetType().Name}";
    }

    public static void AddTarget(string entityUID, bool hasBreakPoints = false) {
        if (entityUID == autoStopString) {
            autoStop = true;
            return;
        }

        if (entityUID == eachString) {
            checkEach = true;
            return;
        }

        if (targets_removed_from_each.Contains(entityUID)) {
            targets_removed_from_each.Remove(entityUID);
            return;
        }

        targets.Add(entityUID);
        if (hasBreakPoints) {
            targets_withBreakpoints.Add(entityUID);
        }
    }

    [Command_StringParameter("ooo_add_target", "Add the entity as a for-each breakpoint in EntityList.Update() (TAS Helper OoO Stepping)")]
    public static void CmdAddTarget(string UID) {
        if (UID.StartsWith("Player") && (UID == "Player" || (playerInstance is not null && UID == GetUID(playerInstance)))) {
            AddTarget("Player", true);
        }
        else {
            AddTarget(UID, false);
        }
        // it's not easy to add a target with breakpoints via cmd (and unncessary), so i only provide this
    }

    [Command_StringParameter("ooo_remove_target", "Remove a for-each breakpoint in EntityList.Update() (TAS Helper OoO Stepping)")]
    public static void RemoveTarget(string UID) {
        if (UID == autoStopString) {
            autoStop = false;
            return;
        }
        if (UID == eachString) {
            checkEach = false;
            targets_removed_from_each.Clear();
            return;
        }
        bool b = targets.Remove(UID);
        targets_withBreakpoints.Remove(UID);
        if (!b && checkEach) {
            targets_removed_from_each.Add(UID);
        }
    }

    [Command("ooo_show_target", "Show all for-each breakpoints in EntityList.Update() (TAS Helper OoO Stepping)")]
    public static void ShowTarget() {
        foreach (string s in targets) {
            Celeste.Commands.Log(s);
        }
        if (checkEach) {
            if (targets_removed_from_each.IsNullOrEmpty()) {
                Celeste.Commands.Log(eachString);
            }
            else {
                Celeste.Commands.Log(eachString + ", except:");
                foreach (string str in targets_removed_from_each) {
                    Celeste.Commands.Log($"  {str}");
                }
            }
        }
        if (autoStop) {
            Celeste.Commands.Log(autoStopString);
        }
    }

    public static void Apply() {
        Reset();
    }

    public static void Undo() {
        Reset();
    }

    // no [Load] attribute here
    internal static void Load() {
        // it seems OnHook and ILHook works on different levels? OnHook (refers to On/IL.Celeste....+= ...) will always be after ILHook?
        // OnHook works via MonoMod.RuntimeDetour.HookGen.HookEndpointManager
        // ILHook works in MonoMod.RuntimeDetour namespace

        if (!hookBuild) {
            using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper" }, ID = "TAS Helper OoO_Core ForEachBreakPoints_EntityList" }) {
                On.Monocle.EntityList.Update += OnEntityListUpdate;
            }
            hookBuild = true;
        }
    }

    internal static bool hookBuild = false;

    [Unload]
    internal static void Unload() {
        if (hookBuild) {
            On.Monocle.EntityList.Update -= OnEntityListUpdate;
            hookBuild = false;
        }
    }

    public static void Reset() {
        removed_targets.Clear();
        expected_passed_targets = 0;
        passed_targets = 0;
        ultraFastForwarding = false;
        firstLoop = true;
        flag1 = flag2 = true;
        firstEnter = true;
    }

    public static void ResetTemp() {
        passed_targets = 0;
#pragma warning disable CS8625
        trackedEntity = null;
#pragma warning restore CS8625
    }

    private static bool EntityUpdateWithBreakPoints(Entity entity) {
        if (flag1) {
            trackedEntity = entity;
            expected_passed_targets++;
            entity._PreUpdate();
            flag1 = false;
        }
        if (entity.Active) {
            entity.Update();
            if (flag2) {
                return true;
            }
        }
        entity._PostUpdate();
        /*
         * EntityUpdate with BreakPoints are added manually, and will have a MarkEnding to call MarkSubMethodPassed()
         * So there's already a SendText call
        */
        flag1 = flag2 = true;
        removed_targets.Add(entity);
        return false;
    }

    private static bool flag1 = true;
    private static bool flag2 = true;

    public static void MarkSubMethodPassed() {
        flag2 = false;
    }

    private static bool firstLoop = true;
    private static void EntityUpdateWithoutBreakPoints(Entity entity) {
        if (firstLoop) {
            expected_passed_targets++;
            if (!checkEach) {
                SendText($"{curr_target_withoutBreakpoint} Update begin");
                firstLoop = false;
                return;
            }
        }

        entity._PreUpdate();
        if (entity.Active) {
            entity.Update();
        }
        entity._PostUpdate();


        firstLoop = true;
        if (prepareToUltraFastForwarding) {
            ultraFastForwarding = true;
        }
        if (checkEach) {
            SendText($"{curr_target_withoutBreakpoint} Update");
        }
        else {
            SendText($"{curr_target_withoutBreakpoint} Update end");
        }
        removed_targets.Add(entity);
    }

    public static bool CheckContain(Entity entity, out bool contain, out string shortID, out string longID) {
        shortID = GetID(entity);
        longID = shortID;
        if (entity.GetEntityData()?.ToEntityId().ToString() is { } entityID) {
            longID = $"{entity.GetType().Name}[{entityID}]";
        }
        contain = targets.Contains(longID) || targets.Contains(shortID);
        return checkEach || contain;
    }

    private static bool IsGotoContinue(Entity entity) {
        if (CheckContain(entity, out localvar_contain, out localvar_entityType, out localvar_entityId)) {
            passed_targets++;
        }
        if (removed_targets.Contains(entity)) {
            return true;
        }
        return passed_targets < expected_passed_targets;
    }

    private static bool IsRunNormally(Entity entity) {
        if (ultraFastForwarding) {
            return true;
        }
        if (checkEach) {
            if (targets_removed_from_each.Contains(localvar_entityType) || targets_removed_from_each.Contains(localvar_entityId)) {
                expected_passed_targets++;
                removed_targets.Add(entity);
                return true;
            }
            return false;
        }
        return !localvar_contain;
    }

    private static bool TargetEntityUpdate(Entity entity) {
        if (targets_withBreakpoints.Contains(localvar_entityId) || targets_withBreakpoints.Contains(localvar_entityType)) {
            /*
             * EntityUpdate with BreakPoints are added manually, and will usually mark the beginning
             * so we don't need to send text here
             */
            curr_target_withBreakpoint = localvar_entityId;
            return EntityUpdateWithBreakPoints(entity);
        }
        else {
            trackedEntity = entity;
            curr_target_withoutBreakpoint = localvar_entityId;
            EntityUpdateWithoutBreakPoints(entity);
            return true;
        }
    }

    internal static Entity trackedEntity;

    public struct PlayerData {
        public Vector2 Position;

        public Vector2 movementCounter;

        public bool Dead;

        public Vector2 LiftSpeed;

        public float liftSpeedTimer;

        public bool Ducking;

        public int StateMachineState;

        public Vector2 Speed;

        public int Dashes;

        public PlayerData(Player player) {
            Position = player.Position;
            movementCounter = player.movementCounter;
            Dead = player.Dead;
            LiftSpeed = player.LiftBoost;
            liftSpeedTimer = player.liftSpeedTimer;
            Ducking = player.Ducking;
            StateMachineState = player.StateMachine.State;
            Speed = player.Speed;
            Dashes = player.Dashes;
        }

        public bool Equals(PlayerData data) {
            return base.Equals(data);
        }
    }

}

