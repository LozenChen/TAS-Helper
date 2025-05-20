using Celeste.Mod.TASHelper.Utils;
using MonoMod.ModInterop;

namespace Celeste.Mod.TASHelper.ModInterop;

internal static class TasSpeedrunToolInterop {

    public static bool Installed = false;

    public const string Slot = "TasHelperPredictor";

    [Initialize(depth: int.MaxValue - 2)]
    public static void InitializeAtFirst() {
        typeof(Imports).ModInterop();
        if (ModUtils.GetModule("SpeedrunTool") is { } srt && srt.Metadata.Version >= new Version(3, 25, 0)) {
            // this interop exists since 3.24.4, for celestetas compatibility
            Installed = Imports.SaveState is not null;
        }
    }

    internal static bool GetInstalledWhenLoading => ModUtils.GetModule("SpeedrunTool") is { } srt && srt.Metadata.Version >= new Version(3, 25, 0);

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
