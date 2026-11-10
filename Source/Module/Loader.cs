namespace Celeste.Mod.TASHelper.Module;

internal static class Loader {

    // order: all mods (load settings -> load) -> all mods initialize ~= all mods load content

    public static void Load() {
        ModUtils.ClearCaches();
        Reloading = GFX.Loaded; // Tas Helper load -> GFX load -> Tas Helper unload -> Tas Helper reload. So GFX.Loaded can be used to detect this
        // this is hacky... i guess i should use AssetReloadHelper instead
        AttributeUtils.Invoke<LoadAttribute>();
    }

    public static void Unload() {
        AttributeUtils.Invoke<UnloadAttribute>();
        HookHelper.Unload();
        ModUtils.ClearCaches();
    }

    public static void Initialize() {
        AttributeUtils.Invoke<InitializeAttribute>();
        AttributeUtils.Invoke<EventOnHookAttribute>();
        TasHelperSettings.FirstInstall = false;
        TASHelperModule.Instance.SaveSettings();
#if DEBUG
        CILCodeHelper.InitializeAtLast();
#endif
        if (Reloading) {
            OnReload();
            Reloading = false;
        }
#if DEBUG
        ReportDebugHookFailures();
#endif
    }

#if DEBUG
    [System.Diagnostics.Conditional("DEBUG")]
    private static void ReportDebugHookFailures() {
        if (HookHelper.DebugHookFailures.Count == 0) {
            return;
        }
        List<string> failures = new(HookHelper.DebugHookFailures);
        HookHelper.DebugHookFailures.Clear();
        throw new Exception($"[TASHelper][DEBUG] {failures.Count} IL hook(s) failed to match any instruction (pattern not found); check hook order against other mods:\n  " + string.Join("\n  ", failures));
    }
#endif

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