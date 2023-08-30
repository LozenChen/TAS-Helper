using Celeste.Mod.SpeedrunTool.SaveLoad;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Monocle;
using static Celeste.Mod.SpeedrunTool.SaveLoad.StateManager;
using Force.DeepCloner;
using Force.DeepCloner.Helpers;
using Celeste.Mod.TASHelper.Utils;
using EventInstance = FMOD.Studio.EventInstance;

namespace Celeste.Mod.TASHelper.Predictor;

public static class ModifiedSaveLoad {

    // thanks to Krafs.Publicizer, we can try to manip it now
    public static bool SaveState() {
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

        alreadySaved = Instance.IsSaved;

        if (alreadySaved) {
            StoreBackup();
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
        return true;
    }

    public static bool LoadState() {
        if (Engine.Scene is not Level level) {
            return false;
        }

        if (Instance.State is State.Loading or State.Waiting || !Instance.IsSaved) {
            return false;
        }

        if (!Instance.SavedByTas) {
            return false;
        }

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
        if (alreadySaved) {
            RestoreBackup();
        }
        return true;
    }

    public static bool alreadySaved = false;

    public static bool hasCache = false;
    public static void StoreBackup() {
        if (hasCache) {
            // todo

            // different Predicts possibly share a common backup
            return;
        }
        // check StateManager.ClearState() to see what's important
        // things in SaveLoadAction seems not need to store, since they do not change after first SL(initialized = true), which is true when StoreBackUp() is called

        State = Instance.State;
        SavedByTas= Instance.SavedByTas;
        LoadByTas = Instance.LoadByTas;
        ClearBeforeSave = Instance.ClearBeforeSave;
        freezeType = Instance.freezeType;

        SaveLoadAction.OnBeforeSaveState(Instance.savedLevel);
        Instance.savedLevel.DeepCloneToShared(P_savedLevel = (Level)FormatterServices.GetUninitializedObject(typeof(Level)));
        P_savedSaveData = Instance.savedSaveData.DeepCloneShared();
        savedTasCycleGroupCounter = Instance.savedTasCycleGroupCounter is null ? null : (int)Instance.savedTasCycleGroupCounter;
        SaveLoadAction.OnSaveState(Instance.savedLevel);
        DeepClonerUtils.ClearSharedDeepCloneState();
        SaveLoadAction.OnPreCloneEntities();
        P_preCloneTask = Task.Run(() => {
            DeepCloneState deepCloneState = new();
            P_savedLevel.Entities.DeepClone(deepCloneState);
            P_savedLevel.RendererList.DeepClone(deepCloneState);
            P_savedSaveData.DeepClone(deepCloneState);
            return deepCloneState;
        });
        hasCache = true;

    }

    private static State State;
    private static bool SavedByTas;
    private static bool LoadByTas;
    private static bool ClearBeforeSave;
    private static Level P_savedLevel;
    private static SaveData P_savedSaveData;
    private static Task<DeepCloneState> P_preCloneTask;
    private static FreezeType freezeType;
    private static object savedTasCycleGroupCounter;

    public static void RestoreBackup() {

        SaveLoadAction.OnBeforeLoadState(Instance.savedLevel);

        DeepClonerUtils.SetSharedDeepCloneState(P_preCloneTask?.Result);
        Instance.UnloadLevel(Instance.savedLevel);

        P_savedLevel.DeepCloneToShared(Instance.savedLevel);
        Instance.savedSaveData = P_savedSaveData.DeepCloneShared();

      
        SaveLoadAction.OnLoadState(Instance.savedLevel);
        SaveLoadAction.OnPreCloneEntities();
        Instance.preCloneTask = Task.Run(() => {
            DeepCloneState deepCloneState = new();
            P_savedLevel.Entities.DeepClone(deepCloneState);
            P_savedLevel.RendererList.DeepClone(deepCloneState);
            P_savedSaveData.DeepClone(deepCloneState);
            return deepCloneState;
        });
        Instance.GcCollect();


        DeepClonerUtils.ClearSharedDeepCloneState();





        Instance.savedTasCycleGroupCounter = savedTasCycleGroupCounter;

        Instance.State = State;
        Instance.SavedByTas = SavedByTas;
        Instance.LoadByTas = LoadByTas;
        Instance.ClearBeforeSave = ClearBeforeSave;
        Instance.freezeType = freezeType;
        
    }

    public static void Initialize() {
        typeof(StateManager).GetMethod("SaveState", BindingFlags.Instance | BindingFlags.NonPublic).HookAfter(() => hasCache = false);
    }
}