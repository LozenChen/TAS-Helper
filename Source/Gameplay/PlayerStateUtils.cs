using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Gameplay;

public static class PlayerStateUtils {
    public static bool Bounce;
    public static bool SuperBounce;
    public static bool SideBounce;
    public static bool Rebound;
    public static bool ReflectBounce;
    public static bool PointBounce;
    public static bool Ultra;
    public static Vector2 SpeedBeforeUltra;
    public static bool RefillDash;
    public static bool AnyBounce => Bounce || SuperBounce || SideBounce || PointBounce || Rebound;
    public static void Clear() {
        Bounce = SuperBounce = SideBounce = Rebound = ReflectBounce = PointBounce = Ultra = RefillDash = false;
    }

    [Initialize]
    public static void Initialize() {
        typeof(Player).GetMethod("Bounce").HookBefore(() => Bounce = true);
        typeof(Player).GetMethod("SuperBounce").HookBefore(() => SuperBounce = true);
        typeof(Player).GetMethod("Rebound").HookBefore(() => Rebound = true);
        typeof(Player).GetMethod("ReflectBounce").HookBefore(() => ReflectBounce = true);
        typeof(Player).GetMethod("PointBounce").HookBefore(() => PointBounce = true);
        typeof(Player).GetMethod("OnCollideV", BindingFlags.NonPublic | BindingFlags.Instance).IlHook(ILUltra);
        typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget().IlHook(ILUltra);
        if (ModUtils.GetType("ExtendedVariantMode", "ExtendedVariants.Variants.EveryJumpIsUltra") is { } ultraVariantType) {
            ultraVariantType.GetMethod("forceUltra", BindingFlags.NonPublic | BindingFlags.Instance).IlHook((cursor, _) => {
                if (cursor.TryGotoNext(MoveType.After, ins => ins.OpCode == OpCodes.Brfalse_S)) {
                    cursor.Emit(OpCodes.Ldarg_1);
                    cursor.EmitDelegate<Action<Player>>(player => { SpeedBeforeUltra = player.Speed; Ultra = true; });
                }
            });
        }
    }

    [Load]
    public static void Load() {
        On.Monocle.Scene.BeforeUpdate += OnBeforeUpdate;
        On.Celeste.Player.SideBounce += OnSideBounce;
        On.Celeste.Player.RefillDash += OnRefillDash;
    }

    [Unload]

    public static void Unload() {
        On.Monocle.Scene.BeforeUpdate -= OnBeforeUpdate;
        On.Celeste.Player.SideBounce -= OnSideBounce;
        On.Celeste.Player.RefillDash -= OnRefillDash;
    }

    private static void OnBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Monocle.Scene self) {
        orig(self);
        Clear();
    }

    private static bool OnSideBounce(On.Celeste.Player.orig_SideBounce orig, Player self, int dir, float fromX, float fromY) {
        if (orig(self, dir, fromX, fromY)) {
            SideBounce = true;
            return true;
        }
        return false;
    }

    private static bool OnRefillDash(On.Celeste.Player.orig_RefillDash orig, Player self) {
        if (orig(self)) {
            RefillDash = true;
            return true;
        }
        return false;
    }

    private static void ILUltra(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.Before,
            ins => ins.OpCode == OpCodes.Stfld,
            ins => ins.OpCode == OpCodes.Ldarg_0 || ins.OpCode == OpCodes.Ldloc_1,
            ins => ins.OpCode == OpCodes.Ldflda,
            ins => ins.OpCode == OpCodes.Ldflda,
            ins => ins.OpCode == OpCodes.Dup,
            ins => ins.OpCode == OpCodes.Ldind_R4,
            ins => ins.MatchLdcR4(1.2f)
            )) {
            cursor.Emit(cursor.Next.Next.OpCode);
            cursor.EmitDelegate<Action<Player>>(player => { SpeedBeforeUltra = player.Speed; Ultra = true; });
        }
    }
}
