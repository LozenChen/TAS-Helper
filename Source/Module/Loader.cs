using Celeste.Mod.TASHelper.Utils;
using MonoMod.ModInterop;

namespace Celeste.Mod.TASHelper.Module;

internal static class Loader {

    // order: all mods (load settings -> load) -> all mods initialize ~= all mods load content

    public static void Load() {
        typeof(ModInterop.TASHelperExports).ModInterop();
        Reloading = GFX.Loaded; // Tas Helper load -> GFX load -> Tas Helper unload -> Tas Helper reload. So GFX.Loaded can be used to detect this
        AttributeUtils.Invoke<LoadAttribute>();
    }

    public static void Unload() {
        AttributeUtils.Invoke<UnloadAttribute>();
        HookHelper.Unload();
    }

    public static void Initialize() {
        typeof(ModInterop.CelesteTasImports).ModInterop();
        HookHelper.InitializeAtFirst();
        ModUtils.InitializeAtFirst();
        AttributeUtils.Invoke<InitializeAttribute>();
        AttributeUtils.Invoke<EventOnHookAttribute>();
        TasHelperSettings.FirstInstall = false;
        TASHelperModule.Instance.SaveSettings();
        CILCodeHelper.InitializeAtLast();
        if (Reloading) {
            OnReload();
            Reloading = false;
        }
    }

    public static void LoadContent() {
        AttributeUtils.Invoke<LoadContentAttribute>();
    }

    public static void OnReload() {
        if (ModUtils.GetType("CelesteTAS-EverestInterop", "TAS.EverestInterop.TargetQuery")?.GetMethodInfo("CollectAllTypes") is { } methodInfo) {
            methodInfo.Invoke(null, parameterless);
            // InfoCustom loses some mod info after hot reload
        }
        AttributeUtils.Invoke<ReloadAttribute>();
    }

    public static bool Reloading;
}