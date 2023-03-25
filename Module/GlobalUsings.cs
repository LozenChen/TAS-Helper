global using static Celeste.Mod.TASHelper.Module.GlobalVariables;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using TAS.Module;

namespace Celeste.Mod.TASHelper.Module;

internal static class GlobalVariables {
    public static TASHelperSettings TasHelperSettings => TASHelperSettings.Instance;

    public static CelesteTasSettings TasSettings => CelesteTasSettings.Instance;
}


internal static class GlobalMethod {
    public static T Apply<T>(this T obj, Action<T> action) {
        action(obj);
        return obj;
    }

}