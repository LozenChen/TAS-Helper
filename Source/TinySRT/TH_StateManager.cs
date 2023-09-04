
using Celeste.Mod.TASHelper.Utils;
using Force.DeepCloner;
using Force.DeepCloner.Helpers;
using Monocle;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using EventInstance = FMOD.Studio.EventInstance;
using TH = Celeste.Mod.TASHelper.TinySRT.TH_SaveLoadAction;
using TH_Deep = Celeste.Mod.TASHelper.TinySRT.TH_DeepClonerUtils;

namespace Celeste.Mod.TASHelper.TinySRT;

// if you desire extending SRT saveslots in an elegant way, you'd better pack StateManager, SaveLoadAction & DeepCloneUtils into a bigger class
// then you can have a new saveslot by just new an instance of the bigger class
public class TH_StateManager {

    private TH_StateManager() { }

    private static readonly Lazy<PropertyInfo> InGameOverworldHelperIsOpen = new(
        () => ModUtils.GetType("CollabUtils2", "Celeste.Mod.CollabUtils2.UI.InGameOverworldHelper")?.GetPropertyInfo("IsOpen")
    );

    private static readonly Lazy<FieldInfo> CycleGroupCounter = new(
        () => ModUtils.GetType("CelesteTAS", "TAS.EverestInterop.Hitboxes.CycleHitboxColor")?.GetFieldInfo("GroupCounter")
    );


    private static readonly Lazy<TH_StateManager> Lazy = new(() => new TH_StateManager());
    public static TH_StateManager Instance => Lazy.Value;


    // public for tas
    public bool IsSaved => savedLevel != null;
    public State State { get; private set; } = State.None;
    public bool SavedByTas { get; private set; }
    public bool LoadByTas { get; private set; }
    public bool ClearBeforeSave { get; private set; }
    public Level SavedLevel => savedLevel;
    private Level savedLevel;
    private SaveData savedSaveData;
    private Task<DeepCloneState> preCloneTask;
    private FreezeType freezeType;
    private Process celesteProcess;
    private object savedTasCycleGroupCounter;

    private enum FreezeType {
        None,
        Save,
        Load
    }

    private readonly HashSet<EventInstance> playingEventInstances = new();


    // todo 
    /*
     * private void ClearStateWhenSwitchScene(On.Monocle.Scene.orig_Begin orig, Scene self) {
        orig(self);
        if (IsSaved) {
            if (self is Overworld && !SavedByTas && InGameOverworldHelperIsOpen.Value?.GetValue(null) as bool? != true) {
                ClearState();
            }

            // 重启章节 Level 实例变更，所以之前预克隆的实体作废，需要重新克隆
            if (self is Level) {
                State = State.None;
                PreCloneSavedEntities();
            }

            if (self.GetSession() is { } session && session.Area != savedLevel.Session.Area) {
                ClearState();
            }
       }
    */

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
        if (TH_StateManager.InGameOverworldHelperIsOpen.Value?.GetValue(null) as bool? == true) {
            return false;
        }

        if (Instance.IsSaved) {
            Instance.ClearBeforeSave = true;
            Instance.ClearStateInner();
            Instance.ClearBeforeSave = false;
        }

        TH_Deep.PushProcessor();
        TH.InitActions();

        Instance.State = State.Saving;
        Instance.SavedByTas = true;

        TH.OnBeforeSaveState(level);
        level.TH_DeepCloneToShared(Instance.savedLevel = (Level)FormatterServices.GetUninitializedObject(typeof(Level)));
        Instance.savedSaveData = SaveData.Instance.TH_DeepCloneShared();
        Instance.savedTasCycleGroupCounter = TH_StateManager.CycleGroupCounter.Value?.GetValue(null);
        TH.OnSaveState(level);
        TH_Deep.ClearSharedDeepCloneState();
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

        TH.OnBeforeLoadState(level);

        TH_Deep.SetSharedDeepCloneState(Instance.preCloneTask?.Result);

        Instance.UpdateTimeAndDeaths(level);
        Instance.UnloadLevel(level);

        Instance.savedLevel.TH_DeepCloneToShared(level);
        SaveData.Instance = Instance.savedSaveData.TH_DeepCloneShared();

        Instance.RestoreAudio1(level);
        Instance.RestoreCassetteBlockManager1(level);
        TH.OnLoadState(level);
        Instance.PreCloneSavedEntities();
        Instance.GcCollect();
        Instance.LoadStateComplete(level);

        return true;
    }

    public static void ClearState() {
        if (HasCachedCurrent) {
            Instance.ClearStateInner();
            HasCachedCurrent = false;
            TH_Deep.PopProcessor();
        }
    }

    // 32 位应用且使用内存超过 2GB 才回收垃圾
    private void GcCollect() {
        if (Environment.Is64BitProcess) {
            return;
        }

        if (celesteProcess == null) {
            celesteProcess = Process.GetCurrentProcess();
        }
        else {
            celesteProcess.Refresh();
        }

        if (celesteProcess.PrivateMemorySize64 > 1024L * 1024L * 1024L * 2.5) {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    // 释放资源，停止正在播放的声音等
    private void UnloadLevel(Level level) {
        List<Entity> entities = new();

        // Player 必须最早移除，不然在处理 player.triggersInside 字段时会产生空指针异常
        entities.AddRange(level.Tracker.GetEntities<Player>());

        // 移除当前房间的实体，照抄 level.UnloadLevel() 方法，不直接调用是因为 BingUI 在该方法中将其存储的 level 设置为了 null
        AddNonGlobalEntities(level, entities);

        // 恢復主音乐
        if (level.Tracker.GetEntity<CassetteBlockManager>() is { } cassetteBlockManager) {
            entities.Add(cassetteBlockManager);
        }

        foreach (Entity entity in entities.Distinct()) {
            try {
                entity.Removed(level);
            }
            catch (NullReferenceException) {
                // ignore https://discord.com/channels/403698615446536203/954507384183738438/954507384183738438
            }
        }

        // 移除剩下声音组件
        level.Tracker.GetComponentsCopy<SoundSource>().ForEach(component => component.RemoveSelf());
    }

    private void AddNonGlobalEntities(Level level, List<Entity> entities) {
        int global = (int)Tags.Global;
        foreach (Entity entity in level.Entities) {
            if ((entity.tag & global) == 0) {
                entities.Add(entity);
                continue;
            }

            TH_SaveLoadAction.OnUnloadLevel(level, entities, entity);
        }
    }

    private void UpdateTimeAndDeaths(Level level) {
        return;
    }

    private void LoadStateComplete(Level level) {
        RestoreLevelTime(level);
        RestoreAudio2();
        RestoreCassetteBlockManager2(level);
        TH_Deep.ClearSharedDeepCloneState();
        State = State.None;
    }

    private void RestoreLevelTime(Level level) {
        level.TimeActive = savedLevel.TimeActive;
        level.RawTimeActive = savedLevel.RawTimeActive;
        CycleGroupCounter.Value?.SetValue(null, savedTasCycleGroupCounter);
    }

    // 收集需要继续播放的声音
    private void RestoreAudio1(Level level) {
        playingEventInstances.Clear();

        foreach (Component component in level.Entities.SelectMany(entity => entity.Components.ToArray())) {
            if (component is SoundSource { Playing: true, instance: { } eventInstance }) {
                playingEventInstances.Add(eventInstance);
            }
        }
    }

    // 等 ScreenWipe 完毕再开始播放
    private void RestoreAudio2() {
        foreach (EventInstance instance in playingEventInstances) {
            instance.start();
        }

        playingEventInstances.Clear();
    }

    // 分两步的原因是更早的停止音乐，听起来更舒服更好一点
    // 第一步：停止播放主音乐
    private void RestoreCassetteBlockManager1(Level level) {
        if (level.Tracker.GetEntity<CassetteBlockManager>() is { } manager) {
            manager.snapshot?.start();
        }
    }

    // 第二步：播放节奏音乐
    private void RestoreCassetteBlockManager2(Level level) {
        if (level.Tracker.GetEntity<CassetteBlockManager>() is { } manager) {
            if (manager.sfx is { } sfx && !manager.isLevelMusic && manager.leadBeats <= 0) {
                sfx.start();
            }
        }
    }

    // public for tas
    // ReSharper disable once MemberCanBePrivate.Global
    // 为了照顾使用体验，不主动触发内存回收（会卡顿，增加 SaveState 时间）
    public void ClearStateInner() {
        preCloneTask?.Wait();

        // fix: 读档冻结时被TAS清除状态后无法解除冻结
        if (State == State.Waiting && Engine.Scene is Level level) {
            OutOfFreeze(level);
        }

        playingEventInstances.Clear();
        savedLevel = null;
        savedSaveData = null;
        preCloneTask = null;
        celesteProcess?.Dispose();
        celesteProcess = null;
        TH.OnClearState();
        State = State.None;
    }

    private void PreCloneSavedEntities() {
        if (IsSaved) {
            TH.OnPreCloneEntities();
            preCloneTask = Task.Run(() => {
                DeepCloneState deepCloneState = new();
                savedLevel.Entities.DeepClone(deepCloneState);
                savedLevel.RendererList.DeepClone(deepCloneState);
                savedSaveData.DeepClone(deepCloneState);
                return deepCloneState;
            });
        }
    }

    private bool IsAllowSave(Level level, bool tas) {
        // 正常游玩时禁止死亡或者跳过过场时存档，TAS 则无以上限制
        // 跳过过场时的黑屏与读档后加的黑屏冲突，会导致一直卡在跳过过场的过程中
        return State == State.None && !level.Paused;
    }

    private void FreezeGame(FreezeType freeze) {
        freezeType = freeze;
    }

    private void OutOfFreeze(Level level) {
        if (freezeType == FreezeType.Save || savedLevel == null) {
            if (savedLevel != null) {
                RestoreLevelTime(level);
            }

            State = State.None;
        }
        else {
            LoadStateComplete(level);
        }

        freezeType = FreezeType.None;
    }

}

public enum State {
    None,
    Saving,
    Loading,
    Waiting,
}
