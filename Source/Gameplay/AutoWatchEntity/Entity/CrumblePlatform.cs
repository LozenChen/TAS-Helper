using Monocle;
using System.Collections;
namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class CrumblePlatformRenderer : AutoWatchTextRenderer {

    public Entity block => this.Entity;

    public Coroutine coroutine;

    public IEnumerator sequence;

    private GetDelegate<object, int> stateGetter;
    private GetDelegate<object, object> currentGetter;
    private GetDelegate<object, bool> onTopGetter;
    private GetDelegate<object, float> timerGetter;
    private GetDelegate<object, int> localVarIGetter;
    public int state => CoroutineFieldCache.GetInt(sequence, ref stateGetter, "<>1__state", -42);

    public object current => CoroutineFieldCache.GetObject(sequence, ref currentGetter, "<>2__current");

    public bool onTop => CoroutineFieldCache.GetBool(sequence, ref onTopGetter, "<onTop>5__2", false);
    public float timer => CoroutineFieldCache.GetFloat(sequence, ref timerGetter, "<timer>5__3", -9999);

    public int localVar_i => CoroutineFieldCache.GetInt(sequence, ref localVarIGetter, "<i>5__4", -9999);
    public CrumblePlatformRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        if (entity.FindCoroutineComponent("Celeste.CrumblePlatform+<Sequence>d__11", out Tuple<Coroutine, IEnumerator> tuple)) {
            coroutine = tuple.Item1;
            sequence = tuple.Item2;
            text.Position = entity.Center;
        }
        else {
            RemoveSelf();
        }
    }

    public override void UpdateImpl() {
        text.Clear();
        if (block.Collidable) {
            if (state == 1) {
                text.Clear();
            }
            else if (state == 2 && localVar_i >= 0) {
                int remainingLoop = (onTop ? 1 : 3) - 1 - localVar_i;
                text.content = ((coroutine.waitTimer.ToFrameData() + 1) + (0.2f.ToFrameData() + 1) * remainingLoop).ToFrame();
            }
            else if (state == 3) {
                text.content = timer.ToFrame() + "~";
            }
            else if (state == 4) {
                text.content = timer.ToFrame();
            }
        }
        else {
            if (state == 5) {
                text.content = coroutine.waitTimer.ToFramePlusOne();
            }
            else if (state == 6) {
                text.content = "~";
            }
        }
        SetVisible();
    }
}

internal sealed class CrumblePlatformFactory() : RendererFactory<CrumblePlatform, CrumblePlatformRenderer>(() => Config.CrumblePlatform, mode => new CrumblePlatformRenderer(mode)) {
}



internal class MaxHelpingHandCrumblePlatformRenderer : AutoWatchTextRenderer {

    public Entity block => this.Entity;

    public Coroutine coroutine;

    public IEnumerator sequence;

    private GetDelegate<object, int> stateGetter;
    private GetDelegate<object, object> currentGetter;
    private GetDelegate<object, bool> onTopGetter;
    private GetDelegate<object, float> targetTimeGetter;
    private GetDelegate<object, float> timerGetter;
    private GetDelegate<object, float> localVarTimeGetter;
    public int state => CoroutineFieldCache.GetInt(sequence, ref stateGetter, "<>1__state", -42);

    public object current => CoroutineFieldCache.GetObject(sequence, ref currentGetter, "<>2__current");

    public bool onTop => CoroutineFieldCache.GetBool(sequence, ref onTopGetter, "<onTop>5__3", false);
    public float targetTime => CoroutineFieldCache.GetFloat(sequence, ref targetTimeGetter, "<targetTime>5__4", -9999);
    public float timer => CoroutineFieldCache.GetFloat(sequence, ref timerGetter, "<timer>5__5", -9999);

    public float LocalVar_time => CoroutineFieldCache.GetFloat(sequence, ref localVarTimeGetter, "<time>5__6", -9999);
    public MaxHelpingHandCrumblePlatformRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        if (entity.FindCoroutineComponent("Celeste.Mod.MaxHelpingHand.Entities.CustomizableCrumblePlatform+<customSequence>d__40", out Tuple<Coroutine, IEnumerator> tuple)) {
            coroutine = tuple.Item1;
            sequence = tuple.Item2;
            text.Position = entity.Center;
        }
        else {
            RemoveSelf();
        }
    }

    public override void UpdateImpl() {
        text.Clear();
        if (block.Collidable) {
            if (state == 1) {
                text.Clear();
            }
            else if (state == 2 && LocalVar_time >= 0f) {
                float time = LocalVar_time;
                float target = targetTime;
                int frame = coroutine.waitTimer.ToFrameData() + 1;
                time += 0.2f;
                while (time < target) {
                    frame += Math.Min(0.2f, target - time).ToFrameData() + 1;
                    time += 0.2f;
                }
                text.content = frame.ToFrame();
            }
            else if (state == 3) {
                text.content = timer.ToFrame() + "~";
            }
            else if (state == 4) {
                text.content = timer.ToFrame();
            }
        }
        else {
            if (state == 6) {
                text.content = coroutine.waitTimer.ToFramePlusOne();
            }
            else if (state == 7) {
                text.content = "~";
            }
            else if (state == 5) {
                text.Clear();
                // it will remove itself in one second
            }
        }
        SetVisible();
    }
}

internal sealed class MaxHelpingHandCrumblePlatformFactory() : RendererFactory<Entity, MaxHelpingHandCrumblePlatformRenderer>(() => Config.CrumblePlatform, mode => new MaxHelpingHandCrumblePlatformRenderer(mode)) {
    public override Type GetTargetType() => ModUtils.GetType("MaxHelpingHand", "Celeste.Mod.MaxHelpingHand.Entities.CustomizableCrumblePlatform");
}



