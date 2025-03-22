using Celeste.Mod.TASHelper.Predictor;
using MonoMod.ModInterop;

namespace Celeste.Mod.TASHelper.ModInterop;

internal static class TASHelperExports {

    [Load]
    public static void Load() {
        typeof(Exports).ModInterop();
    }

    [ModExportName("TASHelper")]
    public static class Exports {
        public static bool InPrediciton() => PredictorCore.InPredict;

    }
}

