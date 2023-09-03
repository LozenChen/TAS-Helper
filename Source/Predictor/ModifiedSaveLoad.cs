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

    public static StateManager Instance => StateManager.Instance;

    public static string InstanceFrom => Instance == OurInstance ? "TasHelper" : "SpeedrunTool";

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
            SavedBySRT = true;
            CloneSRT();
            Instance.ClearBeforeSave = true;
            Instance.ClearState();
            Instance.ClearBeforeSave = false;
            Push();
        }
        else {
            SavedBySRT = false;
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

    public static bool SavedBySRT = false;
    public static bool LoadState() {
        bool result = LoadStateInner();
        return result;
    }
    private static bool LoadStateInner() {
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
            if (SavedBySRT) {
                Pop();
                RestoreSRT();
            }
        }
    }


    public static void CloneSRT() {
        /* VirtualAssets = SaveLoadAction.VirtualAssets;
         ClonedEventInstancesWhenSave = SaveLoadAction.ClonedEventInstancesWhenSave;
         ClonedEventInstancesWhenPreClone = SaveLoadAction.ClonedEventInstancesWhenPreClone;
         All = typeof(SaveLoadAction).GetFieldValue<List<SaveLoadAction>>("All");
         // nope, these fields are readonly
         typeof(SaveLoadAction).SetFieldValue("VirtualAssets", new List<VirtualAsset>());
         typeof(SaveLoadAction).SetFieldValue("ClonedEventInstancesWhenSave", new List<EventInstance>());
         typeof(SaveLoadAction).SetFieldValue("ClonedEventInstancesWhenPreClone", new List<EventInstance>());
         typeof(SaveLoadAction).SetFieldValue("All", new List<SaveLoadAction>());*/


        sharedDeepCloneState = DeepClonerUtils.sharedDeepCloneState.DeepClone();
        Instance.preCloneTask?.Wait();
        playingEventInstances = new HashSet<EventInstance>(Instance.playingEventInstances);
        Instance.playingEventInstances.Clear();
        savedLevel = Instance.savedLevel.DeepCloneShared();
        savedSaveData = Instance.savedSaveData.DeepCloneShared();
    }

    public static void RestoreSRT() {
        /*     typeof(SaveLoadAction).SetFieldValue("VirtualAssets", VirtualAssets);
             typeof(SaveLoadAction).SetFieldValue("ClonedEventInstancesWhenSave", ClonedEventInstancesWhenSave);
             typeof(SaveLoadAction).SetFieldValue("ClonedEventInstancesWhenPreClone", ClonedEventInstancesWhenPreClone);
             typeof(SaveLoadAction).SetFieldValue("All", All);*/


        DeepClonerUtils.sharedDeepCloneState = sharedDeepCloneState.DeepClone();
        foreach (var item in playingEventInstances) {
            Instance.playingEventInstances.Add(item);
        }
        Instance.savedLevel = savedLevel.DeepCloneShared();
        Instance.savedSaveData = savedSaveData.DeepCloneShared();
        
    }

    private static readonly Lazy<StateManager> Lazy = new(() => new StateManager());
    public static StateManager OurInstance => Lazy.Value;

    private static List<VirtualAsset> VirtualAssets;
    private static List<EventInstance> ClonedEventInstancesWhenSave;
    private static List<EventInstance> ClonedEventInstancesWhenPreClone;
    private static List<SaveLoadAction> All;
    private static HashSet<EventInstance> playingEventInstances;
    private static Level savedLevel;
    private static SaveData savedSaveData;
    private static DeepCloneState sharedDeepCloneState;

    private static ILHook hook;
    private static void Manipulator(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        cursor.EmitDelegate(() => OurInstance);
        cursor.Emit(OpCodes.Ret);
    }
    public static void Push() {
        Celeste.Commands.Log("Push");
        hook = new ILHook(typeof(StateManager).GetGetMethod("Instance"), Manipulator);
    }

    public static void Pop() {
        Celeste.Commands.Log("Pop");
        hook?.Dispose();
    }

}
