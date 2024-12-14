
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class FloatySpaceBlockRenderer : AutoWatchTextRenderer {

    public FloatySpaceBlock block;

    public Vector2 lastPos;

    public Vector2 pos;

    public bool useOffsetInsteadOfVelocity = true;

    private const bool allowZero = false;

    private const bool breakline = true;
    public FloatySpaceBlockRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        block = entity as FloatySpaceBlock;
        useOffsetInsteadOfVelocity = Config.FloatySpaceBlock_UseOffsetInsteadOfVelocity;
    }

    public override void UpdateImpl() {
        text.Position = block.Center;
        lastPos = pos;
        pos = block.Position + block.movementCounter;
        if (block.MasterOfGroup || mode == RenderMode.WhenWatched) { // if RenderMode = Always, then we only render the master one
            if (useOffsetInsteadOfVelocity) {
                if (block.MasterOfGroup) {
                    text.content = (pos - block.Moves[block]).OffsetToString(allowZero, breakline);
                }
                else {
                    text.content = (pos - block.master.Moves[block]).OffsetToString(allowZero, breakline);
                }
            }
            else {
                text.content = (pos - lastPos).Positon2ToSignedSpeed(allowZero, breakline);
            }
            Visible = true;
        }
        else {
            Visible = false;
        }
    }

    public override void ClearHistoryData() {
        lastPos = pos = block.Position;
        useOffsetInsteadOfVelocity = Config.FloatySpaceBlock_UseOffsetInsteadOfVelocity;
    }
}

internal class FloatySpaceBlockFactory : IRendererFactory {
    public Type GetTargetType() => typeof(FloatySpaceBlock);

    public bool Inherited() => false;
    public RenderMode Mode() => Config.FloatySpaceBlock;
    public void AddComponent(Entity entity) {
        entity.Add(new FloatySpaceBlockRenderer(Mode()).SleepWhileFastForwarding());
    }
}




