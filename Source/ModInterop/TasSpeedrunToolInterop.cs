using MonoMod.ModInterop;

namespace Celeste.Mod.TASHelper.ModInterop;

internal static class TasSpeedrunToolInterop {

    public static bool Installed = false;

    public const string Slot = "TasHelperPredictor";
    public static void InitializeAtFirst() {
        typeof(Imports).ModInterop();
        Installed = Imports.SaveState is not null;
    }

    public static bool wasSaved = false;

    public static bool SaveState() {
        wasSaved = true;
        return Imports.SaveState(Slot);
    }
    public static bool LoadState() => Imports.LoadState(Slot);
    public static void ClearState() {
        if (wasSaved) {
            Imports.ClearState(Slot);
            wasSaved = false;
        }
    }
    public static bool TasIsSaved() => Imports.TasIsSaved(Slot);


    [ModImportName("SpeedrunTool.TasAction")]
    internal static class Imports {
        public static Func<string, bool> SaveState;
        public static Func<string, bool> LoadState;
        public static Action<string> ClearState;
        public static Func<string, bool> TasIsSaved;
    }
}
