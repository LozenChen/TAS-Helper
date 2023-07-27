using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Gameplay;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.Utils;

namespace Celeste.Mod.TASHelper.Module;

internal static class Loader {

    // order: all mods load -> all mods initialize ~= all mods load content
    public static void EntityLoad() {

        PixelGridHook.Load();

        Messenger.Load();
        SpawnPoint.Load();

    }

    public static void EntityUnload() {

        PixelGridHook.Unload();

        Messenger.Unload();
        SpawnPoint.Unload();

    }

    public static void HelperLoad() {
        PlayerHelper.Load();
        RenderHelper.Load();
        SpinnerHelper.Load();
        HiresLevelRenderer.Load();
        Utils.Logger.Load();
        DebugHelper.Load();
        TH_Hotkeys.Load();


        LoadRangeCountDownCameraTarget.Load();
        SimplifiedSpinner.Load();
        FireBallTrack.Load();

        HookHelper.Load();
    }
    public static void HelperUnload() {
        PlayerHelper.Unload();
        RenderHelper.Unload();
        SpinnerHelper.Unload();
        HiresLevelRenderer.Unload();
        Utils.Logger.Unload();
        DebugHelper.Unload();
        TH_Hotkeys.Unload();
        LoadRangeCountDownCameraTarget.Unload();
        SimplifiedSpinner.Unload();
        FireBallTrack.Unload();
        HookHelper.Unload();
    }

    public static void Initialize() {
        ModUtils.InitializeAtFirst();
        PlayerHelper.Initialize();
        RenderHelper.Initialize();
        SpinnerHelper.Initialize();
        SimplifiedSpinner.Initialize();
        Messenger.Initialize();
        SpawnPoint.Initialize();
        RestoreSettingsExt.Initialize();
        FireBallTrack.Initialize();
    }

    public static void LoadContent() {
    }
}