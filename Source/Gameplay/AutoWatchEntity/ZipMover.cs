
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class ZipMoverRenderer : AutoWatchTextRenderer{

    public Platform platform;

    public Vector2 lastPos;

    public Vector2 pos;
    public ZipMoverRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        platform = entity as Platform;
    }

    public override void UpdateImpl() {
        text.Position = platform.Center;
        lastPos = pos;
        pos = platform.Position + platform.movementCounter;
        if (pos != lastPos) {
            text.content = (pos - lastPos).DeltaPositionToSpeed();
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
    public bool TryAddComponent(Entity entity) {
        entity.Add(new ZipMoverRenderer(Mode()));
        return true;
    }
}




