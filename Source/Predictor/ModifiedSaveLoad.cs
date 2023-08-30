using Celeste.Mod.SpeedrunTool.SaveLoad;

namespace Celeste.Mod.TASHelper.Predictor;

public static class ModifiedSaveLoad {
    public static bool SaveState() {
        return StateManager.Instance.SaveState();
    }

    public static bool LoadState() {
        return StateManager.Instance.LoadState();
    }
}