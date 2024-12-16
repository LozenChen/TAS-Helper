using Celeste.Mod.TASHelper.Utils;
using MonoMod.ModInterop;
using TAS;

namespace Celeste.Mod.TASHelper.Module;

internal static class Loader {

    // order: all mods (load settings -> load) -> all mods initialize ~= all mods load content

    public static void Load() {
        typeof(ModExports.TASHelperExports).ModInterop();
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
        AttributeUtils.Invoke<ReloadAttribute>();
    }

    public static bool Reloading;
}