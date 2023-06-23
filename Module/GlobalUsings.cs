global using static Celeste.Mod.TASHelper.Module.GlobalVariables;
using Monocle;
using TAS.EverestInterop.Hitboxes;
using TAS.Module;

namespace Celeste.Mod.TASHelper.Module;

internal static class GlobalVariables {
    public static TASHelperSettings TasHelperSettings => TASHelperSettings.Instance;

    public static CelesteTasSettings TasSettings => CelesteTasSettings.Instance;

    public static bool DebugRendered {
        get {
            try { 
                return HitboxToggle.DrawHitboxes || Engine.Commands.Open || GameplayRenderer.RenderDebug; 
            }
            catch {
                // don't know why but several bugs about this have been reported
                return HitboxToggle.DrawHitboxes;
            }
        }

        private set { }
    }
}


internal static class GlobalMethod {
    public static T Apply<T>(this T obj, Action<T> action) {
        action(obj);
        return obj;
    }

}