using Celeste.Mod.Helpers;
using ExtendedVariants.Module;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.TASHelper.Utils;

// completely taken from Celeste TAS
internal static class ModUtils {
    public static readonly Assembly VanillaAssembly = typeof(Player).Assembly;

#pragma warning disable CS8603
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
#pragma warning restore CS8603

    public static bool FrostHelperInstalled = false;

    public static bool VivHelperInstalled = false;

    public static bool PandorasBoxInstalled = false;

    public static bool ExtendedVariantInstalled = false;

    public static bool ChronoHelperInstalled = false;

    public static bool BrokemiaHelperInstalled = false;

    public static bool IsaGrabBagInstalled = false;

    private static readonly Lazy<object> upsideDownVariant =
        new(() => Enum.Parse(typeof(ExtendedVariantsModule.Variant), "UpsideDown"));
    private static bool upsideDown {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => (bool)ExtendedVariantsModule.Instance.TriggerManager.GetCurrentVariantValue((ExtendedVariantsModule.Variant)upsideDownVariant.Value);
    }
    public static bool UpsideDown => ExtendedVariantInstalled && upsideDown;
    public static void InitializeAtFirst() {
        FrostHelperInstalled = IsInstalled("FrostHelper");
        VivHelperInstalled = IsInstalled("VivHelper");
        PandorasBoxInstalled = IsInstalled("PandorasBox");
        ExtendedVariantInstalled = IsInstalled("ExtendedVariantMode");
        ChronoHelperInstalled = IsInstalled("ChronoHelper");
        BrokemiaHelperInstalled = IsInstalled("BrokemiaHelper");
        IsaGrabBagInstalled = IsInstalled("IsaGrabBag");
        // we actually also assume they are in enough late version
        // so all entities mentioned in corresponding hooks do exist
    }

}