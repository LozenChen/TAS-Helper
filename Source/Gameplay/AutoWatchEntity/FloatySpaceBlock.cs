
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class FloatySpaceBlockRenderer : AutoWatchTextRenderer {

    public FloatySpaceBlock block;

    public Vector2 lastPos;

    public Vector2 pos;

    public bool useOffsetInsteadOfVelocity = true;
    public FloatySpaceBlockRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        block = entity as FloatySpaceBlock;
        useOffsetInsteadOfVelocity = Config.UseOffsetInsteadOfVelocity;
    }

    public override void UpdateImpl() {
        text.Position = block.Center;
        lastPos = pos;
        pos = block.Position + block.movementCounter;
        if (block.MasterOfGroup || mode == RenderMode.WhenWatched) { // if RenderMode = Always, then we only render the master one
            if (useOffsetInsteadOfVelocity) {
                if (block.MasterOfGroup) {
                    text.content = (pos - block.Moves[block]).OffsetToString();
                }
                else {
                    text.content = (pos - block.master.Moves[block]).OffsetToString();
                }
            }
            else {
                text.content = (pos - lastPos).Positon2ToSignedSpeed();
            }
            Visible = true;
        }
        else {
            Visible = false;
        }
    }

    public override void ClearHistoryData() {
        lastPos = pos = block.Position;
        useOffsetInsteadOfVelocity = Config.UseOffsetInsteadOfVelocity;
    }
}

internal class FloatySpaceBlockFactory : IRendererFactory {
    public Type GetTargetType() => typeof(FloatySpaceBlock);

    public bool Inherited() => false;
    public RenderMode Mode() => Config.FloatySpaceBlock;
    public void AddComponent(Entity entity) {
        entity.Add(new FloatySpaceBlockRenderer(Mode()));
    }
}




