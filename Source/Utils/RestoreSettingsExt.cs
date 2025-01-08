using Celeste.Mod.TASHelper.Module;

namespace Celeste.Mod.TASHelper.Utils;

public static class RestoreSettingsExt {
    // make TAS Helper settings work as if it's a CelesteTAS setting
    // so it will not be influenced by CelesteTAS's "Restore All Settings when TAS Stop"

    [Initialize]
    public static void Initialize() {
        ModInterop.CelesteTasImports.AddSettingsRestoreHandler(TASHelperModule.Instance, null);
    }
}