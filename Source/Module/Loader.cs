using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Gameplay;
using Celeste.Mod.TASHelper.Gameplay.Spinner;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.Predictor;
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
        ActualPosition.Load();
        SpinnerRenderHelper.Load();
        SpinnerCalculateHelper.Load();
        HiresLevelRenderer.Load();
        Utils.Logger.Load();
        DebugHelper.Load();
        TH_Hotkeys.Load();
        LoadRange_and_CameraTarget.Load();
        SimplifiedSpinner.Load();
        FireBallTrack.Load();
        ModifiedAutoMute.Load();
        PredictorRenderer.Load();
        HookHelper.Load();
    }
    public static void HelperUnload() {
        ActualPosition.Unload();
        SpinnerRenderHelper.Unload();
        SpinnerCalculateHelper.Unload();
        HiresLevelRenderer.Unload();
        Utils.Logger.Unload();
        DebugHelper.Unload();
        TH_Hotkeys.Unload();
        LoadRange_and_CameraTarget.Unload();
        SimplifiedSpinner.Unload();
        FireBallTrack.Unload();
        ModifiedAutoMute.Unload();
        PredictorRenderer.Unload(); 
        HookHelper.Unload();
    }

    public static void Initialize() {
        ModUtils.InitializeAtFirst();
        ActualPosition.Initialize();
        SpinnerRenderHelper.Initialize();
        SpinnerCalculateHelper.Initialize();
        SimplifiedSpinner.Initialize();
        Messenger.Initialize();
        SpawnPoint.Initialize();
        RestoreSettingsExt.Initialize();
        FireBallTrack.Initialize();
        SpinnerColliderHelper.Initialize();
        Countdown_and_LoadRange_Collider.Initialize();

        ModifiedSaveLoad.Initialize();
        InputManager.Initialize();
        Predictor.Core.Initialize();
    }

    public static void LoadContent() {
    }
}