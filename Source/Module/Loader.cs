using Celeste.Mod.TASHelper.Utils;

namespace Celeste.Mod.TASHelper.Module;

internal static class Loader {

    // order: all mods load -> all mods initialize ~= all mods load content

    public static void Load() {
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
    }

    public static void LoadContent() {
        AttributeUtils.Invoke<LoadContentAttribute>();
    }
}