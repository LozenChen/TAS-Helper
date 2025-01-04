using Celeste.Mod.TASHelper.Gameplay;
using Celeste.Mod.TASHelper.Predictor;
using Celeste.Mod.TASHelper.TinySRT;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.TASHelper.ModInterop;

[ModExportName("TASHelper")]
public static class TASHelperExports {

    // for predictor SL, call this when initialize
    public static void AddSLAction(Action<Dictionary<Type, Dictionary<string, object>>, Level> saveState, Action<Dictionary<Type, Dictionary<string, object>>, Level> loadState, Action clearState, Action<Level> beforeSaveState = null, Action preCloneEntities = null) {
        ExtraSlActions.TH_Actions.Add(new TH_SaveLoadAction(saveState.CreateSlAction(), loadState.CreateSlAction(), clearState, beforeSaveState, preCloneEntities));
    }
    public static object DeepCloneShared(object obj) {
        return obj.TH_DeepCloneShared();
    }

    private static TH_SaveLoadAction.SlAction CreateSlAction(this Action<Dictionary<Type, Dictionary<string, object>>, Level> action) {
        return (dict, level) => action(dict, level);
    }
    public static bool InPrediciton() => PredictorCore.InPredict;

}