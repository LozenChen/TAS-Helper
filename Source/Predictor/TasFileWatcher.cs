using Monocle;
using TAS.Input;

namespace Celeste.Mod.TASHelper.Predictor;

public static class TasFileWatcher {

    [TasFileChanged]
    private static void OnTasFileChanged() {
        if (TasHelperSettings.PredictFutureEnabled && FrameStep && Engine.Scene is Level) {
            if (TasHelperSettings.PredictOnFileChange) {
                PredictorCore.PredictLater(true);
                PredictorCore.delayedClearState = true;
            }
            else if (TasHelperSettings.DropPredictionWhenTasFileChange) {
                PredictorCore.delayedClearFutures = true; // clear it directly may interrupt PredictorRenderer.DebugRender
                PredictorCore.HasCachedFutures = false;
                PredictorCore.delayedClearState = true;
            }
        }
    }
}