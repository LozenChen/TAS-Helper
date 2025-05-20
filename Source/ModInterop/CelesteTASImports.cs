using MonoMod.ModInterop;

namespace Celeste.Mod.TASHelper.ModInterop;

internal static class CelesteTasImporter {

    [Initialize(depth: int.MaxValue - 1)]
    public static void InitializeAtFirst() {
        typeof(CelesteTasImports).ModInterop();
    }
}

[ModImportName("CelesteTAS")]
internal static class CelesteTasImports {
    public delegate void AddSettingsRestoreHandlerDelegate(EverestModule module, (Func<object> Backup, Action<object> Restore)? handler);
    public delegate void RemoveSettingsRestoreHandlerDelegate(EverestModule module);

    /// Registers custom delegates for backing up and restoring mod setting before / after running a TAS
    /// A `null` handler causes the settings to not be backed up and later restored
    public static AddSettingsRestoreHandlerDelegate AddSettingsRestoreHandler = null!;

    /// De-registers a previously registered handler for the module
    public static RemoveSettingsRestoreHandlerDelegate RemoveSettingsRestoreHandler = null!;
}