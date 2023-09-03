using Celeste.Mod.SpeedrunTool.SaveLoad;
using Monocle;
using Celeste.Mod.TASHelper.Utils;
using System.Runtime.Serialization;
using FMOD.Studio;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Force.DeepCloner;
using Force.DeepCloner.Helpers;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.TASHelper.Predictor;

public static class ModifiedSaveLoad {

    private static readonly Lazy<StateManager> Lazy = new(() => new StateManager());
    // public static StateManager Instance => Lazy.Value;
    public static StateManager Instance => StateManager.Instance;

    // thanks to Krafs.Publicizer, we can try to manip it now
    public static bool SaveState() {
        if (HasCachedCurrent) {
            // different Predicts possibly share a common save of current frame
            return true;
        }

        if (Engine.Scene is not Level level) {
            return false;
        }

        if (Instance.State != State.None || level.Paused) {
            return false;
        }

        // 不允许在春游图打开章节面板时存档
        if (StateManager.InGameOverworldHelperIsOpen.Value?.GetValue(null) as bool? == true) {
            return false;
        }

        if (Instance.IsSaved) {
            Instance.ClearBeforeSave = true;
            Instance.ClearState();
            Instance.ClearBeforeSave = false;
        }

        SaveLoadAction.InitActions();

        Instance.State = State.Saving;
        Instance.SavedByTas = true;

        SaveLoadAction.OnBeforeSaveState(level);
        level.DeepCloneToShared(Instance.savedLevel = (Level)FormatterServices.GetUninitializedObject(typeof(Level)));
        Instance.savedSaveData = SaveData.Instance.DeepCloneShared();
        Instance.savedTasCycleGroupCounter = StateManager.CycleGroupCounter.Value?.GetValue(null);
        SaveLoadAction.OnSaveState(level);
        DeepClonerUtils.ClearSharedDeepCloneState();
        Instance.PreCloneSavedEntities();
        Instance.State = State.None;
        HasCachedCurrent = true;
        return true;
    }

    public static bool HasCachedCurrent = false;

    public static bool LoadState() {
        if (Engine.Scene is not Level level) {
            return false;
        }

        if (Instance.State is State.Loading or State.Waiting || !Instance.IsSaved) {
            return false;
        }

        // Instance = StateManger.Instance in any cases

        Instance.LoadByTas = true;
        Instance.State = State.Loading;

        SaveLoadAction.OnBeforeLoadState(level);

        DeepClonerUtils.SetSharedDeepCloneState(Instance.preCloneTask?.Result);

        Instance.UpdateTimeAndDeaths(level);
        Instance.UnloadLevel(level);

        Instance.savedLevel.DeepCloneToShared(level);
        SaveData.Instance = Instance.savedSaveData.DeepCloneShared();

        Instance.RestoreAudio1(level);
        Instance.RestoreCassetteBlockManager1(level);
        SaveLoadAction.OnLoadState(level);
        Instance.PreCloneSavedEntities();
        Instance.GcCollect();
        Instance.LoadStateComplete(level);
        
        return true;
    }

    public static void ClearState() {
        if (HasCachedCurrent) {
            Instance.ClearState();
            HasCachedCurrent = false;
        }
    }
}
