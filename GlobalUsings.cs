global using static Celeste.Mod.TASHelper.GlobalUsings;

namespace Celeste.Mod.TASHelper;

internal static class GlobalUsings{
    public static TASHelperSettings TasHelperSettings => TASHelperSettings.Instance;

    public static T Apply<T>(this T obj, Action<T> action) {
        action(obj);
        return obj;
    }
}