using SRT = Celeste.Mod.SpeedrunTool.SaveLoad.SaveLoadAction;
using TH = Celeste.Mod.TASHelper.TinySRT.SaveLoadAction;

namespace Celeste.Mod.TASHelper.TinySRT;

public static class SlActionsAddedByMods {

    public static TH TAS_slAction;

    [Load]
    public static void LoadTAS() {
        TH TAS_slAction = ((SRT)TAS.Utils.SpeedrunToolUtils.saveLoadAction).Convert();
        TH.Add(TAS_slAction);
    }

    [Unload]
    public static void UnloadTAS() {
        TH.Remove(TAS_slAction);
    }

    public static TH.SlAction Convert(this SRT.SlAction action) {
        return (savedValues, level) => { action(savedValues, level); };
    }

    public static TH Convert(this SRT action) {
        return new TH(action.saveState.Convert(), action.loadState.Convert(), action.clearState, action.beforeSaveState, action.beforeLoadState, action.preCloneEntities);
    }
}
