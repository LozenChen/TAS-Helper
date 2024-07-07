
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class FallingBlockRenderer : AutoWatchTextRenderer{

    public FallingBlock block;

    public Coroutine coroutine;

    public IEnumerator sequence;

    public int state => sequence.GetFieldValue<int>("<>1__state");
    public object current => sequence.GetFieldValue("<>2__current");
    public float timer => sequence.GetFieldValue<float>("<timer>5__4");

    public Vector2 lastPos;

    public Vector2 pos;
    public FallingBlockRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        block = entity as FallingBlock;
        Tuple<Coroutine, IEnumerator> tuple = entity.FindCoroutine("<Sequence>d__21");
        coroutine = tuple.Item1;
        sequence = tuple.Item2;
    }

    public override void UpdateImpl() {
        text.Position = block.Center;
        if (state == 3) {
            text.content = "(s3) "+ coroutine.waitTimer.ToFrame();
            Visible = true;
        }
        else if (state == 4) {
            text.content = "(s4) " + timer.ToFrame() + "~";
            Visible = true;
        }
        else {
            lastPos = pos;
            pos = block.Position + block.movementCounter;
            if (pos != lastPos) {
                text.content = (pos - lastPos).ToSpeed();
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
    public bool TryAddComponent(Entity entity) {
        entity.Add(new FallingBlockRenderer(Mode()));
        return true;
    }
}




