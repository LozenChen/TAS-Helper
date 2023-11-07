//#define OoO_Debug

using Celeste.Mod.TASHelper.Utils;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Runtime.CompilerServices;
using TAS.EverestInterop;
using TAS.EverestInterop.Hitboxes;
using static Celeste.Mod.TASHelper.OrderOfOperation.OoO_Core;

namespace Celeste.Mod.TASHelper.OrderOfOperation;
internal static class ForEachBreakPoints_PlayerCollider {

    private static readonly HashSet<string> targets = new();

    private static readonly HashSet<string> targets_removed_from_each = new();

    private static readonly HashSet<PlayerCollider> removed_targets = new();

    private static int passed_targets = 0;

    private static int expected_passed_targets = 0;

    internal static bool ultraFastForwarding = false;

    public const string eachString = "Each";

    public const string autoStopString = "Auto";

    private static IDetour detour;

    internal static void Create() {
        using (new DetourContext { Before = new List<string> { "*", "CelesteTAS-EverestInterop", "TASHelper" }, ID = "TAS Helper OoO_Core ForEachBreakPoints_PlayerCollider" }) {
            detour = new ILHook(PlayerOrigUpdate, il => {
                ILCursor cursor = new ILCursor(il);
                cursor.TryGotoNext(
                ins => ins.OpCode == OpCodes.Ldarg_0,
                ins => ins.MatchCallOrCallvirt<Player>("get_Dead"),
                ins => ins.OpCode == OpCodes.Brtrue,
                ins => ins.OpCode == OpCodes.Ldarg_0,
                ins => ins.OpCode == OpCodes.Ldfld,
                ins => ins.MatchCallOrCallvirt<StateMachine>("get_State"),
                ins => ins.MatchLdcI4(21));
                cursor.Index += 7;
                cursor.Next.MatchBeq(out ILLabel normalExitTarget);

                if (cursor.TryGotoNext(MoveType.AfterLabel,
                    ins => ins.OpCode == OpCodes.Ldarg_0,
                    ins => ins.MatchCallOrCallvirt<Entity>("get_Collider"),
                    ins => ins.OpCode == OpCodes.Stloc_S,
                    ins => ins.OpCode == OpCodes.Ldarg_0,
                    ins => ins.OpCode == OpCodes.Ldarg_0,
                    ins => ins.MatchLdfld<Player>("hurtbox"),
                    ins => ins.MatchCallOrCallvirt<Entity>("set_Collider")
                )) {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate(PCCheck);
                    cursor.Emit(OpCodes.Brtrue, normalExitTarget);
                    cursor.Emit(OpCodes.Ret);
                }
#if OoO_Debug
                        else {
                            failedHooks.Add($"\n ForEachBreakPoints_PlayerCollider");
                        }
#endif
            }, manualConfig);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool PCCheck(Player player) {
        if (leaveInNextLoop) {
            leaveInNextLoop = false;
            return true;
        }
        switch (PCCheckCore(player)) {
            case ReturnType.EarlyReturn: {
                    RecoverHitbox(player);
                    return false;
                }
            case ReturnType.DeathReturn: {
                    ReportKillerEvent();
                    return false;
                }
            case ReturnType.LateReturn: {
                    CompleteEvent();
                    return false;
                }
        }
        return true;
    }

    private static bool leaveInNextLoop = false;

    private static bool firstEnter = true;

    private static string killerID = "";

    private static void ReportKillerEvent() {
        SendText($"PlayerOrigUpdate_PlayerColliderCheck end. Killer is {killerID}");
        Reset();
        leaveInNextLoop = true;
    }
    private static void CompleteEvent() {
        SendText("PlayerOrigUpdate_PlayerColliderCheck end");
        Reset();
        leaveInNextLoop = true;
    }

    private static void RecoverHitbox(Player player) {
        if (player.Collider == player.hurtbox) {
            player.Collider = stored_Hitbox;
            recoverHurtbox = true;
        }
    }

    private static bool recoverHurtbox = false;

    private enum ReturnType { EarlyReturn, LateReturn, DeathReturn };
    private static ReturnType PCCheckCore(Player player) {
        if (firstEnter) {
            ActualEntityCollideHitbox.SavePlayerPosition(player);
            firstEnter = false;
            stored_Hitbox = player.Collider;
            player.Collider = player.hurtbox;
        }
        else if (recoverHurtbox) {
            player.Collider = player.hurtbox;
            recoverHurtbox = false;
        }

        foreach (PlayerCollider pc in player.Scene.Tracker.GetComponents<PlayerCollider>()) {
            if (IsGotoContinue(pc, out bool contain, out string entityType, out string entityId)) {
                continue;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            ReturnType AfterCheckInOneStep() {
                SendText($"{entityId}'s PlayerCollider check");
                removed_targets.Add(pc);
                if (prepareToUltraFastForwarding) {
                    ultraFastForwarding = true;
                }
                return ReturnType.EarlyReturn;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            ReturnType CheckInTwoSteps() {
                if (waitForNextLoop) {
                    expected_passed_targets++;
                    SendText($"{entityId}'s PlayerCollider check begin");
                    waitForNextLoop = false;
                    return ReturnType.EarlyReturn;
                }
                else {
                    waitForNextLoop = true;
                    if (pc.Check(player) && player.Dead) {
                        player.Collider = stored_Hitbox;
                        killerID = entityId;
                        return ReturnType.DeathReturn;
                    }
                    else {
                        SendText($"{entityId}'s PlayerCollider check end");
                        removed_targets.Add(pc);
                        if (prepareToUltraFastForwarding) {
                            ultraFastForwarding = true;
                        }
                        return ReturnType.EarlyReturn;
                    }
                }
            }

            ActualEntityCollideHitbox.SaveEntityPosition(pc);

            if (ultraFastForwarding) {
                if (pc.Check(player) && player.Dead) {
                    player.Collider = stored_Hitbox;
                    trackedPC = pc;
                    killerID = entityId;
                    return ReturnType.DeathReturn;
                }
            }
            else if (checkEach && (targets_removed_from_each.Contains(entityType) || targets_removed_from_each.Contains(entityId))) {
                expected_passed_targets++;
                removed_targets.Add(pc);
                if (pc.Check(player)) {
                    trackedPC = pc;
                    if (player.Dead) {
                        player.Collider = stored_Hitbox;
                        killerID = entityId;
                        return ReturnType.DeathReturn;
                    }
                    else if (autoStop) {
                        return AfterCheckInOneStep();
                    }
                }
            }
            else if (!checkEach && !contain) {
                if (pc.Check(player)) {
                    trackedPC = pc;
                    if (player.Dead) {
                        player.Collider = stored_Hitbox;
                        killerID = entityId;
                        return ReturnType.DeathReturn;
                    }
                    else if (autoStop) {
                        return AfterCheckInOneStep();
                    }
                }
            }
            else {
                trackedPC = pc;
                if (checkEach) {
                    expected_passed_targets++;
                    if (pc.Check(player) && player.Dead) {
                        player.Collider = stored_Hitbox;
                        killerID = entityId;
                        return ReturnType.DeathReturn;
                    }
                    else {
                        return AfterCheckInOneStep();
                    }
                }
                else {
                    return CheckInTwoSteps();
                }
            }
        }
        if (player.Collider == player.hurtbox) {
            player.Collider = stored_Hitbox;
        }
        return ReturnType.LateReturn;
    }

    private static Collider stored_Hitbox = new Hitbox(8f, 11f, -4f, -11f);

    private static bool waitForNextLoop = true;

    private static bool autoStop = false;

    private static bool checkEach = false;

    internal static PlayerCollider trackedPC;

    public static bool CheckContain(Entity entity, out string id, out string type) {
        type = entity.GetType().Name;
        id = type;
        if (entity.GetEntityData()?.ToEntityId().ToString() is { } entityID) {
            id = $"{entity.GetType().Name}[{entityID}]";
        }
        return targets.Contains(id) || targets.Contains(type);
    }

    private static bool IsGotoContinue(PlayerCollider pc, out bool contain, out string entityType, out string entityId) {
        if (pc.Entity is not Entity entity) {
            entityId = "";
            entityType = "";
            contain = false;
            return true;
        }
        contain = CheckContain(entity, out entityId, out entityType);
        if (checkEach || contain) {
            passed_targets++;
        }
        if (removed_targets.Contains(pc)) { // removed_targets may be added by autostop (thus may not pass CheckContain), so we move it out here
            return true;
        }
        return passed_targets < expected_passed_targets;
    }


    [Command_StringParameter("ooo_add_target_pc", "Add the entity as a for-each breakpoint in PlayerCollider checks (TAS Helper OoO Stepping)")]
    public static void AddTarget(string UID) {
        if (UID == autoStopString) {
            autoStop = true;
        }
        else if (UID == eachString) {
            checkEach = true;
        }
        else if (targets_removed_from_each.Contains(UID)) {
            targets_removed_from_each.Remove(UID);
        }
        else {
            targets.Add(UID);
        }

        if (haveTryToApply && !applied) {
            detour.Apply();
            Reset();
            applied = true;
        }
    }

    [Command_StringParameter("ooo_remove_target_pc", "Remove a for-each breakpoint in PlayerCollider checks (TAS Helper OoO Stepping)")]
    public static void RemoveTarget(string UID) {
        if (UID == autoStopString) {
            autoStop = false;
        }
        else if (UID == eachString) {
            checkEach = false;
            targets_removed_from_each.Clear();
        }
        else {
            bool b = targets.Remove(UID);
            if (!b && checkEach) {
                targets_removed_from_each.Add(UID);
            }
        }

        if (!ShouldApply && applied) {
            detour.Undo();
            Reset();
            applied = false;
        }
    }

    [Command("ooo_show_target_pc", "Show all for-each breakpoints in PlayerCollider checks (TAS Helper OoO Stepping)")]
    public static void ShowTarget() {
        foreach (string s in targets) {
            Celeste.Commands.Log(s);
        }
        if (checkEach) {
            Celeste.Commands.Log(eachString);
        }
        if (autoStop) {
            Celeste.Commands.Log(autoStopString);
        }
    }

    private static bool haveTryToApply = false;
    internal static bool applied = false;

    public static bool ShouldApply => targets.IsNotNullOrEmpty() || checkEach || autoStop;

    public static void Apply() {
        if (ShouldApply) {
            detour.Apply();
            applied = true;
        }
        haveTryToApply = true;
        Reset();
    }

    public static void Undo() {
        haveTryToApply = false;
        applied = false;
        detour.Undo();
        Reset();
    }

    [Unload]
    public static void Dispose() {
        detour?.Dispose();
    }

    public static void Reset() {
        removed_targets.Clear();
        expected_passed_targets = 0;
        passed_targets = 0;
        ultraFastForwarding = false;
        stored_Hitbox = null;
        waitForNextLoop = true;
        leaveInNextLoop = false;
        firstEnter = true;
        recoverHurtbox = false;
    }

    public static void ResetTemp() {
        passed_targets = 0;
        trackedPC = null;
    }
}
