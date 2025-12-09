using Monocle;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Gameplay;
internal static class KoseiDebugRenderer {

    [Initialize]
    private static void Initialize() {
        ModUtils.GetType("KoseiHelper", "Celeste.Mod.KoseiHelper.Entities.DebugRenderer")?.GetMethodInfo("Rendering")?.ILHook(il => {
            ILCursor cursor = new ILCursor(il);
            Instruction target = cursor.Next;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(ShouldSkip);
            cursor.Emit(OpCodes.Brfalse, target);
            cursor.Emit(OpCodes.Ret);
        });
    }

    private static bool ShouldSkip(Entity entity) {
        return entity.GetFieldValue("shape")?.ToString() == "FilledRectangle";
    }
}