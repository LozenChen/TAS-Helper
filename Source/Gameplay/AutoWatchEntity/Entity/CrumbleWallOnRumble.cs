using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class CrumbleWallOnRumbleRenderer : AutoWatchTextRenderer {

    public CrumbleWallOnRumble crumble;

    public bool Initalized = false;

    public Dictionary<RumbleTrigger, int> rumbleTriggers = new Dictionary<RumbleTrigger, int>();

    public RumbleTrigger currentActiveTrigger = null;

    public int listCurrentIndex;

    public int listTargetIndex;

    public float localTimer;
    public CrumbleWallOnRumbleRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        crumble = entity as CrumbleWallOnRumble;
    }

    private const string expectedIEnumeratorName = "<RumbleRoutine>d__13";

    public void DelayedInitialize() {
        // if a (persistent) rumble trigger remove the CrumbleBlock, then we need to do nothing 
        if (!crumble.Collidable || crumble.Scene is null) {
            SleepForever(); // we don't remove it, so when users click on the crumble wall, nothing happens
            return;
        }

        text.Position = crumble.Center;
        rumbleTriggers.Clear();
        foreach (RumbleTrigger trigger in crumble.Scene.Tracker.SafeGetEntities<RumbleTrigger>()) {
            int index = trigger.crumbles.IndexOf(crumble);
            if (index != -1) {
                rumbleTriggers.Add(trigger, index);
            }
        }
        if (rumbleTriggers.IsEmpty()) {
            SleepForever();
            return;
        }

        Initalized = true;
    }

    public void SleepForever() {
        Visible = PostActive = hasUpdate = false;
        if (text is not null) {
            HiresLevelRenderer.Remove(text);
        }
    }

    public override void UpdateImpl() {
        if (!Initalized) {
            DelayedInitialize();
            if (!Initalized) {
                return;
            }
        }
        if (!crumble.Collidable || crumble.Scene is null) {
            SleepForever();
            return;
        }
        int fastestCurrentIndex = -1;
        float fastestWaitTimer = 9999f;
        bool triggerChanged = false;
        if (rumbleTriggers.Count(x => x.Key.started) == 1) {
            if (currentActiveTrigger is null || !currentActiveTrigger.started) { // how can it be not started
                currentActiveTrigger = rumbleTriggers.First(x => x.Key.started).Key;
                foreach (Component c in currentActiveTrigger.Components) {
                    if (c is not Coroutine cor) {
                        continue;
                    }
                    if (cor.Current?.GetType()?.Name?.Equals(expectedIEnumeratorName) ?? false) {
                        // when communal helper exists (due to SwapImmediately), this won't be shown in the first frame
                        // when vanilla, for some reason we also don't render its first frame
                        int state = cor.Current.GetFieldValue<int>("<>1__state");
                        switch (state) {
                            case 1: {
                                    fastestCurrentIndex = -1;
                                    break;
                                }
                            case 2: {
                                    List<CrumbleWallOnRumble>.Enumerator enumerator = cor.Current.GetFieldValue<List<CrumbleWallOnRumble>.Enumerator>("<>7__wrap1");
                                    fastestCurrentIndex = currentActiveTrigger.crumbles.IndexOf(enumerator.Current);
                                    break;
                                }
                            default: {
                                    continue;
                                }
                        }
                        fastestWaitTimer = cor.waitTimer;
                        triggerChanged = true;
                    }
                }
                if (!triggerChanged) {
                    currentActiveTrigger = null;
                }
            }
        }
        else {
            List<RumbleTrigger> slowerTriggers = new List<RumbleTrigger>();
            RumbleTrigger lastFastestTrigger = null;
            int fastestRecord = int.MaxValue;
            foreach (KeyValuePair<RumbleTrigger, int> pair in rumbleTriggers) {
                if (pair.Key is RumbleTrigger trigger && trigger.started) {
                    foreach (Component c in trigger.Components) {
                        if (c is not Coroutine cor) {
                            continue;
                        }
                        if (cor.Current?.GetType()?.Name?.Equals(expectedIEnumeratorName) ?? false) {
                            // when communal helper exists (due to SwapImmediately), this won't be shown in the first frame
                            // when vanilla, for some reason we also don't render its first frame
                            int state = cor.Current.GetFieldValue<int>("<>1__state");
                            int currentBreakingBlock = -1;
                            switch (state) {
                                case 1: {
                                        currentBreakingBlock = -1;
                                        break;
                                    }
                                case 2: {
                                        List<CrumbleWallOnRumble>.Enumerator enumerator = cor.Current.GetFieldValue<List<CrumbleWallOnRumble>.Enumerator>("<>7__wrap1");
                                        currentBreakingBlock = trigger.crumbles.IndexOf(enumerator.Current);
                                        break;
                                    }
                                default: {
                                        continue;
                                    }
                            }
                            int predictTime = PredictTime(currentBreakingBlock, pair.Value, cor.waitTimer);
                            if (lastFastestTrigger is null) {
                                lastFastestTrigger = trigger;
                                fastestRecord = predictTime;
                                fastestCurrentIndex = currentBreakingBlock;
                                fastestWaitTimer = cor.waitTimer;
                            }
                            else if (predictTime >= fastestRecord) {
                                slowerTriggers.Add(trigger);
                            }
                            else {
                                slowerTriggers.Add(lastFastestTrigger);
                                lastFastestTrigger = trigger;
                                fastestRecord = predictTime;
                                fastestCurrentIndex = currentBreakingBlock;
                                fastestWaitTimer = cor.waitTimer;
                            }
                            break;
                        }
                    }
                }
            }
            if (currentActiveTrigger != lastFastestTrigger && lastFastestTrigger is not null) {
                currentActiveTrigger = lastFastestTrigger;
                triggerChanged = true;
            }
            foreach (RumbleTrigger trigger in slowerTriggers) {
                rumbleTriggers.Remove(trigger);
            }
        }
        if (currentActiveTrigger is null) {
            Visible = false;
            return;
        }
        if (triggerChanged) {
            localTimer = fastestWaitTimer;
            listCurrentIndex = fastestCurrentIndex;
            listTargetIndex = rumbleTriggers[currentActiveTrigger];
        }
        else {
            if (localTimer > 0f) {
                localTimer -= Engine.DeltaTime;
            }
            else {
                listCurrentIndex++;
                localTimer = 0.05f;
            }
        }
        text.content = PredictTime(listCurrentIndex, listTargetIndex, localTimer).ToFrame();
        Visible = true;
    }

    public static int PredictTime(int currentIndex, int targetIndex, float waitTimer) {
        // if there are multiple different manual triggered RumbleTrigger with different delay, this can be a bit wrong when TimeRate changes
        return (targetIndex - currentIndex - 1) * ((0.05f).ToFrameData() + 1) + waitTimer.ToFrameData();
    }
}

internal class CrumbleWallOnRumbleFactory : IRendererFactory {
    public Type GetTargetType() => typeof(CrumbleWallOnRumble);

    public bool Inherited() => true;
    public RenderMode Mode() => Config.CrumbleWallOnRumble;
    public void AddComponent(Entity entity) {
        entity.Add(new CrumbleWallOnRumbleRenderer(Mode()).SleepWhileFastForwarding());
    }
}





