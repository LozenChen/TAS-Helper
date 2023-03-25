using Celeste.Mod.Helpers;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Utils;

// completely taken from Celeste TAS
internal static class ModUtils {
    public static readonly Assembly VanillaAssembly = typeof(Player).Assembly;

    public static Type GetType(string modName, string name, bool throwOnError = false, bool ignoreCase = false) {
        return GetAssembly(modName)?.GetType(name, throwOnError, ignoreCase);
    }

    public static Type GetType(string name, bool throwOnError = false, bool ignoreCase = false) {
        return FakeAssembly.GetFakeEntryAssembly().GetType(name, throwOnError, ignoreCase);
    }

    public static Type[] GetTypes() {
        return FakeAssembly.GetFakeEntryAssembly().GetTypes();
    }

    public static EverestModule GetModule(string modName) {
        return Everest.Modules.FirstOrDefault(module => module.Metadata?.Name == modName);
    }

    public static bool IsInstalled(string modName) {
        return GetModule(modName) != null;
    }

    public static Assembly GetAssembly(string modName) {
        return GetModule(modName)?.GetType().Assembly;
    }

    public static bool FrostHelperInstalled = false;

    public static bool VivHelperInstalled = false;

    public static bool PandorasBoxInstalled = false;
    public static void InitializeAtFirst() {
        FrostHelperInstalled = IsInstalled("FrostHelper");
        VivHelperInstalled = IsInstalled("VivHelper");
        PandorasBoxInstalled = IsInstalled("PandorasBox");
    }
}