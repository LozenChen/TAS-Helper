using Celeste.Mod.TASHelper.Entities;

namespace Celeste.Mod.TASHelper.Utils;

internal static class Loader {

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
        SimplifiedSpinner.Load();
        Logger.Load();
        DebugHelper.Load();
    }
    public static void HelperUnload() {
        PlayerHelper.Unload();
        RenderHelper.Unload();
        SpinnerHelper.Unload();
        SimplifiedSpinner.Unload();
        HookHelper.Unload();
        Logger.Unload();
        DebugHelper.Unload();
    }

    public static void Initialize() {
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