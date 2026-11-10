using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.TASHelper.OrderOfOperation;

// Sub-step hooks inside the player update pipeline (the part of OoO that needs
// a few additive IL injection sites, because MMHOOK has no hook for them).
internal static class OoO_PlayerHooks {
    private static bool insidePlayerOrigUpdate;
    private static MonoMod.RuntimeDetour.ILHook playerOrigUpdateHook;
    private static bool attached;

    internal static void Attach() {
        if (attached) {
            return;
        }
        using (DetourContextHelper.ManagedHook("TAS Helper OoO Player").AppliedAfter(DetourContextHelper.CelesteTAS).Use()) {
            On.Celeste.Actor.Update += OnActorUpdate;
            On.Celeste.Actor.MoveH += OnActorMoveH;
            On.Celeste.Actor.MoveV += OnActorMoveV;
            On.Celeste.Player.ClimbHop += OnPlayerClimbHop;
            On.Celeste.Level.EnforceBounds += OnLevelEnforceBounds;
        }

        MonoMod.RuntimeDetour.DetourConfig config = DetourContextHelper.ILHook("TAS Helper OoO Player IL")
            .AppliedAfter(DetourContextHelper.CelesteTAS)
            .ToConfig();

        playerOrigUpdateHook = HookHelper.ManualAppliedILHook(typeof(Player).GetMethod("orig_Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic), il => {
            ILCursor cursor = new(il);
            // entry: player orig_Update begin
            cursor.Goto(0);
            cursor.EmitDelegate<System.Action>(() => insidePlayerOrigUpdate = true);
            cursor.Emit(OpCodes.Ldstr, "PlayerOrigUpdate begin");
            cursor.EmitDelegate<System.Action<string>>(OoO_Controller.BreakExternal);

            // camera block (same anchor as the old implementation)
            cursor = new ILCursor(il);
            cursor.Goto(0);
            if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Ldarg_0,
                    ins => ins.MatchLdfld<Player>("ForceCameraUpdate"),
                    ins => ins.OpCode == OpCodes.Brfalse)) {
                cursor.Index += 3;
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldstr, "PlayerOrigUpdate_CameraUpdate");
                cursor.EmitDelegate<System.Action<string>>(OoO_Controller.BreakExternal);
            }

            // player collider check begin
            cursor = new ILCursor(il);
            cursor.Goto(0);
            if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Ldarg_0,
                    ins => ins.MatchCallOrCallvirt<Player>("get_Dead"),
                    ins => ins.OpCode == OpCodes.Brtrue,
                    ins => ins.OpCode == OpCodes.Ldarg_0,
                    ins => ins.OpCode == OpCodes.Ldfld,
                    ins => ins.MatchCallOrCallvirt<StateMachine>("get_State"),
                    ins => ins.MatchLdcI4(21))) {
                cursor.Index += 8;
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldstr, "PlayerOrigUpdate_PlayerColliderCheck begin");
                cursor.EmitDelegate<System.Action<string>>(OoO_Controller.BreakExternal);
            }

            // per-PlayerCollider loop (for-each breakpoints inside the collider check loop)
            cursor = new ILCursor(il);
            cursor.Goto(0);
            if (cursor.TryGotoNext(MoveType.After, ins => ins.Operand is MethodReference mr && mr.Name == "get_Current" && mr.ReturnType.FullName.Contains("PlayerCollider"))) {
                Instruction stloc = cursor.Next;
                VariableDefinition pcLocal = stloc.OpCode.Code switch {
                    Code.Stloc_0 => il.Body.Variables[0],
                    Code.Stloc_1 => il.Body.Variables[1],
                    Code.Stloc_2 => il.Body.Variables[2],
                    Code.Stloc_3 => il.Body.Variables[3],
                    _ => stloc.Operand as VariableDefinition
                };
                if (pcLocal is not null) {
                    cursor.Index++;
                    cursor.Emit(OpCodes.Ldloc, pcLocal);
                    cursor.EmitDelegate<System.Action<PlayerCollider>>(OoO_PlayerCollider.OnLoopHead);
                }
            }

            // end: before every ret
            cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.Before, ins => ins.OpCode == OpCodes.Ret)) {
                cursor.Emit(OpCodes.Ldstr, "PlayerOrigUpdate end");
                cursor.EmitDelegate<System.Action<string>>(OoO_Controller.BreakExternal);
                cursor.EmitDelegate<System.Action>(() => insidePlayerOrigUpdate = false);
                cursor.Index++;
            }
        }, config);
        playerOrigUpdateHook.Apply();
        attached = true;
    }

    internal static void Detach() {
        if (!attached) {
            return;
        }
        On.Celeste.Actor.Update -= OnActorUpdate;
        On.Celeste.Actor.MoveH -= OnActorMoveH;
        On.Celeste.Actor.MoveV -= OnActorMoveV;
        On.Celeste.Player.ClimbHop -= OnPlayerClimbHop;
        On.Celeste.Level.EnforceBounds -= OnLevelEnforceBounds;
        playerOrigUpdateHook?.Dispose();
        playerOrigUpdateHook = null;
        insidePlayerOrigUpdate = false;
        attached = false;
    }

    private static void OnActorUpdate(On.Celeste.Actor.orig_Update orig, Actor self) {
        if (insidePlayerOrigUpdate) {
            OoO_Controller.BreakExternal("PlayerOrigUpdate_BaseUpdate begin");
        }
        orig(self);
        if (insidePlayerOrigUpdate) {
            OoO_Controller.BreakExternal("PlayerOrigUpdate_BaseUpdate end");
        }
    }

    private static bool OnActorMoveH(On.Celeste.Actor.orig_MoveH orig, Actor self, float moveH, Collision onCollide, Solid safe) {
        if (insidePlayerOrigUpdate) {
            OoO_Controller.BreakExternal("PlayerOrigUpdate_MoveH");
        }
        return orig(self, moveH, onCollide, safe);
    }

    private static bool OnActorMoveV(On.Celeste.Actor.orig_MoveV orig, Actor self, float moveV, Collision onCollide, Solid safe) {
        if (insidePlayerOrigUpdate) {
            OoO_Controller.BreakExternal("PlayerOrigUpdate_MoveV");
        }
        return orig(self, moveV, onCollide, safe);
    }

    private static void OnPlayerClimbHop(On.Celeste.Player.orig_ClimbHop orig, Player self) {
        OoO_Controller.BreakExternal("PlayerOrigUpdate_ClimbHop");
        orig(self);
    }

    private static void OnLevelEnforceBounds(On.Celeste.Level.orig_EnforceBounds orig, Level self, Player player) {
        if (insidePlayerOrigUpdate) {
            OoO_Controller.BreakExternal("PlayerOrigUpdate_LevelEnforceBounds");
        }
        orig(self, player);
    }
}
