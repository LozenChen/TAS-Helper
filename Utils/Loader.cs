using Celeste.Mod.TASHelper.Entities;

namespace Celeste.Mod.TASHelper.Utils;

internal static class Loader {

    // order: all mods load -> all mods initialize ~= all mods load content
    public static void EntityLoad() {
        LoadRangeCountDownCameraTarget.Load();
        PixelGridHook.Load();
        SimplifiedSpinner.Load();
        Messenger.Load();
    }

    public static void EntityUnload() {
        LoadRangeCountDownCameraTarget.Unload();
        PixelGridHook.Unload();
        SimplifiedSpinner.Unload();
        Messenger.Unload();
    }

    public static void HelperLoad() {
        PlayerHelper.Load();
        RenderHelper.Load();
        SpinnerHelper.Load();
        HiresLevelRenderer.Load();
        HookHelper.Load();
        Logger.Load();
        DebugHelper.Load();
        TH_Hotkeys.Load();
    }
    public static void HelperUnload() {
        PlayerHelper.Unload();
        RenderHelper.Unload();
        SpinnerHelper.Unload();
        HiresLevelRenderer.Unload();
        HookHelper.Unload();
        Logger.Unload();
        DebugHelper.Unload();
        TH_Hotkeys.Unload();
    }

    public static void Initialize() {
        TasHelperSettings.InitializeSettings();
        ModUtils.InitializeAtFirst();
        PlayerHelper.Initialize();
        RenderHelper.Initialize();
        SpinnerHelper.Initialize();
        SimplifiedSpinner.Initialize();
        Messenger.Initialize();
    }

    public static void LoadContent() {
    }
}