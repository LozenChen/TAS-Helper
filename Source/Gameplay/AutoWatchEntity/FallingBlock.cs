
using Celeste.Mod.TASHelper.Utils;
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
    public FallingBlockRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        block = entity as FallingBlock;
        if (entity.FindCoroutineComponent("Celeste.FallingBlock+<Sequence>d__21", out Tuple<Coroutine, IEnumerator> tuple)) {
            coroutine = tuple.Item1;
            sequence = tuple.Item2;
        }
        else {
            coroutine = null;
            sequence = null;
        }
    }

    public override void UpdateImpl() {
        text.Position = block.Center;
        if (state == 3) {
            text.content = coroutine.waitTimer.ToFrame();
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
        entity.Add(new FallingBlockRenderer(Mode()));
    }
}




