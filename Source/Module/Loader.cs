using Celeste.Mod.TASHelper.Utils;

namespace Celeste.Mod.TASHelper.Module;

internal static class Loader {

    // order: all mods load -> all mods initialize ~= all mods load content

    public static void Load() {
        Reloading = GFX.Loaded; // Tas Helper load -> GFX load -> Tas Helper unload -> Tas Helper reload. So GFX.Loaded can be used to detect this
        AttributeUtils.Invoke<LoadAttribute>();
    }

    public static void Unload() {
        AttributeUtils.Invoke<UnloadAttribute>();
        HookHelper.Unload();
    }

    public static void Initialize() {
        HookHelper.InitializeAtFirst();
        ModUtils.InitializeAtFirst();
        AttributeUtils.Invoke<InitializeAttribute>();
        typeof(TAS.Manager).GetMethod("DisableRun").HookAfter(() => AttributeUtils.Invoke<TasDisableRunAttribute>());
        TasHelperSettings.FirstInstall = false;
        TASHelperModule.Instance.SaveSettings();
        if (Reloading) {
            OnReload();
            Reloading = false;
        }
    }

    public static void LoadContent() {
        AttributeUtils.Invoke<LoadContentAttribute>();
    }

    public static void OnReload() {
        typeof(TAS.EverestInterop.InfoHUD.InfoCustom).InvokeMethod("CollectAllTypeInfo"); // InfoCustom loses some mod info after hot reload
    }

    public static bool Reloading;
}