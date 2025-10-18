using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class FallingBlockRenderer : AutoWatchTextRenderer {

    public FallingBlock block;

    public Coroutine coroutine;

    public IEnumerator sequence;

    public int state => sequence?.GetFieldValue<int>("<>1__state") ?? -42;
    public float timer => sequence?.GetFieldValue<float>("<timer>5__4") ?? -9999;

    public Vector2 lastPos;

    public Vector2 pos;

    public bool Initialized = false;
    public FallingBlockRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        block = entity as FallingBlock;

    }

    public override void UpdateImpl() {
        text.Position = block.Center;
        if (!Initialized) {
            Initialized = true;
            if (block.FindCoroutineComponent("Celeste.FallingBlock+<Sequence>d__21", out Tuple<Coroutine, IEnumerator> tuple)) {
                coroutine = tuple.Item1;
                sequence = tuple.Item2;
            }
            else if (block.FindCoroutineComponent("Celeste.Mod.HonlyHelper.RisingBlock+<FallingBlock_Sequence>d__5", out Tuple<Coroutine, IEnumerator> tuple2)
                && tuple2.Item2.GetFieldValue("<origEnum>5__8") is IEnumerator enumrator) {
                coroutine = tuple2.Item1;
                sequence = enumrator;
            }
            else {
                coroutine = null;
                sequence = null;
            }
        }
        if (state == 3) {
            text.content = coroutine.waitTimer.ToFrameAllowZero();
            Visible = true;
        }
        else if (state == 4) {
            text.content = timer.ToFrame() + "~";
            Visible = true;
        }
        else {
            lastPos = pos;
            pos = block.Position + block.movementCounter;
            if (pos != lastPos) {
                text.content = (pos - lastPos).PositionToAbsoluteSpeed();
                Visible = true;
            }
            else {
                Visible = false;
            }
        }
    }

    public override void ClearHistoryData() {
        lastPos = pos = block.Position;
    }
}

internal class FallingBlockFactory : IRendererFactory {
    public Type GetTargetType() => typeof(FallingBlock);

    public bool Inherited() => false;
    public RenderMode Mode() => Config.FallingBlock;
    public void AddComponent(Entity entity) {
        entity.Add(new FallingBlockRenderer(Mode()).SleepWhileFastForwarding());
    }
}




