
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class ZipMoverRenderer : AutoWatchTextRenderer {

    public Platform platform;

    public Vector2 lastPos;

    public Vector2 pos;


    public Coroutine coroutine;

    public IEnumerator sequence;

    public int state => sequence?.GetFieldValue<int>("<>1__state") ?? -1;
    public float timer => sequence?.GetFieldValue<float>("<>2__current") ?? -9999;

    public int timerToFrame = 0;
    public ZipMoverRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        platform = entity as Platform;
        if (entity.FindCoroutineComponent("Celeste.ZipMover+<Sequence>d__24", out Tuple<Coroutine, IEnumerator> tuple)) {
            coroutine = tuple.Item1;
            sequence = tuple.Item2;
        }
        else {
            coroutine = null;
            sequence = null;
        }
    }

    public override void UpdateImpl() {
        text.Position = platform.Center;
        lastPos = pos;
        pos = platform.Position + platform.movementCounter;
        if (pos != lastPos) {
            text.content = (pos - lastPos).PositionToAbsoluteSpeed();
            Visible = true;
        }
        else if (timerToFrame > 0) {
            timerToFrame--;
            text.content = timerToFrame.ToFrame();
            Visible = true;
        }
        else if (timer > 0f) {
            timerToFrame = timer.ToFrameData();
            text.content = timerToFrame.ToFrame();
            Visible = true;
        }
        else {
            Visible = false;
        }
    }

    public override void ClearHistoryData() {
        lastPos = pos = platform.Position;
    }
}

internal class ZipMoverFactory : IRendererFactory {
    public Type GetTargetType() => typeof(ZipMover);

    public bool Inherited() => true;
    public RenderMode Mode() => Config.ZipMover;
    public void AddComponent(Entity entity) {
        entity.Add(new ZipMoverRenderer(Mode()));
    }
}




