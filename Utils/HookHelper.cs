using System.Reflection;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.TASHelper.Utils;
internal static class HookHelper {
    // taken from CelesteTAS
    private static readonly List<IDetour> Hooks = new();

    public static void Unload() {
        foreach (IDetour detour in Hooks) {
            detour.Dispose();
        }

        Hooks.Clear();
    }

    // e.g.
    // typeof(Player).GetMethod("orig_Update").IlHook(PlayerPositionBeforeCameraUpdateIL);
    public static void IlHook(this MethodBase from, ILContext.Manipulator manipulator) {
        Hooks.Add(new ILHook(from, manipulator));
    }

    public static void IlHook(this MethodBase from, Action<ILCursor, ILContext> manipulator) {
        from.IlHook(il => {
            ILCursor ilCursor = new(il);
            manipulator(ilCursor, il);
        });
    }
}