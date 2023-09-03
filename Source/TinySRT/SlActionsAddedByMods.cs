using SRT = Celeste.Mod.SpeedrunTool.SaveLoad.SaveLoadAction;
using TH = Celeste.Mod.TASHelper.TinySRT.TH_SaveLoadAction;

namespace Celeste.Mod.TASHelper.TinySRT;

public static class SlActionsAddedByMods {

    // if there's mod which add SlAction by itself, instead of by SRT, then we also add it to our SaveLoadActions

    public static readonly List<TH> Actions = new();

    [Load]
    public static void LoadTAS() {
        // Actions.Add(((SRT)TAS.Utils.SpeedrunToolUtils.saveLoadAction).Convert());
        /*
        * no, we don't need to add this to our TH.All
        * everything is ok
        */

        foreach (TH action in Actions) {
            TH.Add(action);
        }
    }

    [Unload]
    public static void UnloadTAS() {
        foreach (TH action in Actions) {
            TH.Remove(action);
        }
        Actions.Clear();
    }

    public static TH.SlAction Convert(this SRT.SlAction action) {
        return (savedValues, level) => { action(savedValues, level); };
    }

    public static TH Convert(this SRT action) {
        return new TH(action.saveState.Convert(), action.loadState.Convert(), action.clearState, action.beforeSaveState, action.beforeLoadState, action.preCloneEntities);
    }
}
