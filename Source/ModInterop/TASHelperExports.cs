using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.TASHelper.ModInterop;

internal static class TASHelperExports {

    [Load]
    public static void Load() {
        typeof(Exports).ModInterop();
    }

    [ModExportName("TASHelper")]
    public static class Exports {
        public static bool InPrediciton() => Predictor.PredictorCore.InPredict;

        public static bool IsSpinner(Entity entity) => Gameplay.Spinner.Info.HazardTypeHelper.IsSpinner(entity);
        public static bool IsLightning(Entity entity) => Gameplay.Spinner.Info.HazardTypeHelper.IsLightning(entity);
        public static bool IsDust(Entity entity) => Gameplay.Spinner.Info.HazardTypeHelper.IsDust(entity);
        public static bool IsHazard(Entity entity) => Gameplay.Spinner.Info.HazardTypeHelper.IsHazard(entity);

    }
}

