
using Celeste.Mod.TASHelper.Utils;
using Monocle;
using System.Collections;
namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class CrumblePlatformRenderer : AutoWatchTextRenderer {

    public Entity block => this.Entity;

    public Coroutine coroutine;

    public IEnumerator sequence;

    public int state => sequence?.GetFieldValue<int>("<>1__state") ?? -42;

    public object current => sequence?.GetFieldValue("<>2__current");

    public bool onTop => sequence?.GetFieldValue<bool>("<onTop>5__2") ?? false;
    public float timer => sequence?.GetFieldValue<float>("<timer>5__3") ?? -9999;

    public int localVar_i => sequence?.GetFieldValue<int>("<i>5__4") ?? -9999;

    public bool Initialized = false;
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
                text.content = (coroutine.waitTimer + remainingLoop * 0.2f).ToFrameAllowZero();
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
                text.content = coroutine.waitTimer.ToFrameAllowZero();
            }
            else if (state == 6) {
                text.content = "~";
            }
        }
        SetVisible();
    }
}

internal class CrumblePlatformFactory : IRendererFactory {
    public Type GetTargetType() => typeof(CrumblePlatform);

    public bool Inherited() => false;
    public RenderMode Mode() => Config.CrumblePlatform;
    public void AddComponent(Entity entity) {
        entity.Add(new CrumblePlatformRenderer(Mode()).SleepWhileFastForwarding());
    }
}




