global using static Celeste.Mod.TASHelper.GlobalUsings;
using TAS.Module;

namespace Celeste.Mod.TASHelper;

internal static class GlobalUsings {
    public static TASHelperSettings TasHelperSettings => TASHelperSettings.Instance;

    public static CelesteTasSettings TasSettings => CelesteTasSettings.Instance;
    public static T Apply<T>(this T obj, Action<T> action) {
        action(obj);
        return obj;
    }
}