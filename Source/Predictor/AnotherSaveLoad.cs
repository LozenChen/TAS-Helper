using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Celeste.Mod.SpeedrunTool.Extensions;
using Celeste.Mod.SpeedrunTool.Message;
using Celeste.Mod.SpeedrunTool.Other;
using Celeste.Mod.SpeedrunTool.Utils;
using Force.DeepCloner;
using Force.DeepCloner.Helpers;
using EventInstance = FMOD.Studio.EventInstance;
using Celeste.Mod.SpeedrunTool.SaveLoad;
using Monocle;

namespace Celeste.Mod.TASHelper.Predictor;

public sealed class P_StateManager {
    private static SpeedrunTool.SpeedrunToolSettings ModSettings => SpeedrunTool.SpeedrunToolSettings.Instance;

    private static readonly Lazy<P_StateManager> Lazy = new(() => new P_StateManager());
    public static P_StateManager Instance => Lazy.Value;
    private P_StateManager() { }

    private static readonly Lazy<PropertyInfo> InGameOverworldHelperIsOpen = new(
        () => ModUtils.GetType("CollabUtils2", "Celeste.Mod.CollabUtils2.UI.InGameOverworldHelper")?.GetPropertyInfo("IsOpen")
    );

    private static readonly Lazy<FieldInfo> CycleGroupCounter = new(
        () => ModUtils.GetType("CelesteTAS", "TAS.EverestInterop.Hitboxes.CycleHitboxColor")?.GetFieldInfo("GroupCounter")
    );

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
    private Process celesteProcess;
    private object savedTasCycleGroupCounter;


    private readonly HashSet<EventInstance> playingEventInstances = new();

    internal bool HasCached = false;
    public bool SaveState() {
        if (HasCached) {
            return true;
        }

        if (Engine.Scene is not Level level) {
            return false;
        }

        if (!IsAllowSave(level)) {
            return false;
        }

        // 不允许在春游图打开章节面板时存档
        if (InGameOverworldHelperIsOpen.Value?.GetValue(null) as bool? == true) {
            return false;
        }

        SaveLoadAction.InitActions();

        State = State.Saving;
        SavedByTas = true;

        SaveLoadAction.OnBeforeSaveState(level);
        level.DeepCloneToShared(savedLevel = (Level)FormatterServices.GetUninitializedObject(typeof(Level)));
        savedSaveData = SaveData.Instance.DeepCloneShared();
        savedTasCycleGroupCounter = CycleGroupCounter.Value?.GetValue(null);
        SaveLoadAction.OnSaveState(level);
        DeepClonerUtils.ClearSharedDeepCloneState();
        PreCloneSavedEntities();
        State = State.None;

        //HasCached = true;
        return true;
    }

    public bool LoadState() {
        if (Engine.Scene is not Level level) {
            return false;
        }

        if (!SavedByTas) {
            return false;
        }

        LoadByTas = true;
        State = State.Loading;

        SaveLoadAction.OnBeforeLoadState(level);

        DeepClonerUtils.SetSharedDeepCloneState(preCloneTask?.Result);

        UnloadLevel(level);

        savedLevel.DeepCloneToShared(level);
        SaveData.Instance = savedSaveData.DeepCloneShared();

        RestoreAudio1(level);
        RestoreCassetteBlockManager1(level);
        SaveLoadAction.OnLoadState(level);
        PreCloneSavedEntities();
        GcCollect();


        LoadStateComplete(level);


        return true;
    }

    // 32 位应用且使用内存超过 2GB 才回收垃圾
    private void GcCollect() {
        if (ModSettings.NoGcAfterLoadState || Environment.Is64BitProcess) {
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

            SaveLoadAction.OnUnloadLevel(level, entities, entity);
        }
    }


    private void LoadStateComplete(Level level) {
        RestoreLevelTime(level);
        RestoreAudio2();
        RestoreCassetteBlockManager2(level);
        DeepClonerUtils.ClearSharedDeepCloneState();
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

    // 为了照顾使用体验，不主动触发内存回收（会卡顿，增加 SaveState 时间）
    public void ClearState() {
        preCloneTask?.Wait();
        playingEventInstances.Clear();
        savedLevel = null;
        savedSaveData = null;
        preCloneTask = null;
        celesteProcess?.Dispose();
        celesteProcess = null;
        SaveLoadAction.OnClearState();
        State = State.None;
    }


    private void PreCloneSavedEntities() {
        if (IsSaved) {
            SaveLoadAction.OnPreCloneEntities();
            preCloneTask = Task.Run(() => {
                DeepCloneState deepCloneState = new();
                savedLevel.Entities.DeepClone(deepCloneState);
                savedLevel.RendererList.DeepClone(deepCloneState);
                savedSaveData.DeepClone(deepCloneState);
                return deepCloneState;
            });
        }
    }

    private bool IsAllowSave(Level level) {
        // 正常游玩时禁止死亡或者跳过过场时存档，TAS 则无以上限制
        // 跳过过场时的黑屏与读档后加的黑屏冲突，会导致一直卡在跳过过场的过程中
        return State == State.None && !level.Paused;
    }

}

public enum State {
    None,
    Saving,
    Loading,
    Waiting,
}