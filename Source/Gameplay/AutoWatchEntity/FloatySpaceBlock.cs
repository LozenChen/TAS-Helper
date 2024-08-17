
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class FloatySpaceBlockRenderer : AutoWatchTextRenderer {

    public FloatySpaceBlock block;

    public Vector2 lastPos;

    public Vector2 pos;
    public FloatySpaceBlockRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        block = entity as FloatySpaceBlock;
    }

    public override void UpdateImpl() {
        text.Position = block.Center;
        lastPos = pos;
        pos = block.Position + block.movementCounter;
        if (block.MasterOfGroup || mode == RenderMode.WhenWatched) { // if RenderMode = Always, then we only render the master one
            text.content = (pos - lastPos).Positon2ToSignedSpeed();
            Visible = true;
        }
        else {
            Visible = false;
        }
    }

    public override void ClearHistoryData() {
        lastPos = pos = block.Position;
    }
}

internal class FloatySpaceBlockFactory : IRendererFactory {
    public Type GetTargetType() => typeof(FloatySpaceBlock);

    public bool Inherited() => true;
    public RenderMode Mode() => Config.FloatySpaceBlock;
    public bool TryAddComponent(Entity entity) {
        entity.Add(new FloatySpaceBlockRenderer(Mode()));
        return true;
    }
}




