using Celeste.Mod.TASHelper.Module;

namespace Celeste.Mod.TASHelper.ModInterop;

public static class RestoreSettingsExt {
    // make TAS Helper settings work as if it's a CelesteTAS setting
    // so it will not be influenced by CelesteTAS's "Restore All Settings when TAS Stop"

    [Initialize]
    public static void Initialize() {
        CelesteTasImports.AddSettingsRestoreHandler(TASHelperModule.Instance, null);
    }
}