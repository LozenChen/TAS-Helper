using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.TASHelper.Gameplay;
internal static class BetterSpeedrunTimerDisplay {

    [Load]

    private static void Load() {
        using (new DetourContext { Before = new List<string> { "*" }, ID = "TAS Helper BetterSpeedrunTimerDisplay" }) {
            IL.Celeste.SpeedrunTimerDisplay.Render += ILSpeedrunTimerDisplayRender;
        }
    }

    private static void Unload() {
        IL.Celeste.SpeedrunTimerDisplay.Render -= ILSpeedrunTimerDisplayRender;
    }

    private static void ILSpeedrunTimerDisplayRender(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After, ins => ins.OpCode == OpCodes.Ret) || cursor.Next is null) {
            return;
        }
        VariableDefinition alpha = new VariableDefinition(il.Import(typeof(float)));
        il.Body.Variables.Add(alpha);

        cursor.MoveAfterLabels();
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc, alpha);
        cursor.EmitDelegate(UpdateAlpha);
        cursor.Emit(OpCodes.Stloc, alpha);

        Instruction head = cursor.Next;

        while (cursor.TryGotoNext(MoveType.After, ins => ins.MatchCall(typeof(Color), "get_Black"))) {
            ColorMulWithAlpha();
        }

        cursor.Goto(head);
        while (cursor.TryGotoNext(MoveType.Before,
            ins => ins.MatchCallvirt(
                typeof(MTexture).GetMethod("Draw", new Type[] { typeof(Vector2) })))
            ) {
            cursor.Remove();
            cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("get_Zero"));
            cursor.Emit(OpCodes.Call, typeof(Color).GetMethod("get_White"));
            cursor.Emit(OpCodes.Callvirt, typeof(MTexture).GetMethod("Draw", new Type[] { typeof(Vector2), typeof(Vector2), typeof(Color) }));
            cursor.Index++;
        }

        cursor.Goto(head);
        while (cursor.TryGotoNext(MoveType.After, ins => ins.MatchCall(typeof(Color), "get_White"))) {
            ColorMulWithAlpha();
        }

        cursor.Goto(head);
        while (cursor.TryGotoNext(MoveType.Before, ins => ins.MatchCall(typeof(SpeedrunTimerDisplay), nameof(SpeedrunTimerDisplay.DrawTime)))) {
            FloatMulWithAlpha();
            cursor.Index++;
        }

        void FloatMulWithAlpha() {
            cursor.Emit(OpCodes.Ldloc, alpha);
            cursor.Emit(OpCodes.Mul);
        }

        void ColorMulWithAlpha() {
            cursor.Emit(OpCodes.Ldloc, alpha);
            cursor.Emit(OpCodes.Call, typeof(Color).GetMethod(nameof(Color.Multiply)));
        }
    }

    private static float UpdateAlpha(SpeedrunTimerDisplay display, float alpha) {
        if (Settings.Instance.SpeedrunClock == SpeedrunType.Off || !FrameStep) {
            return 1f;
        }
        return TasHelperSettings.SpeedrunTimerDisplayOpacityToFloat;
    }

}