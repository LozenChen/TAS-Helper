using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class CrushBlockRenderer : AutoWatchTextRenderer {

    public CrushBlock crushBlock;

    public Vector2 lastPos;

    public Vector2 pos;
    public CrushBlockRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        crushBlock = entity as CrushBlock;
        lastPos = pos = entity.Position;
    }

    public override void UpdateImpl() {
        text.Position = crushBlock.Center;
        lastPos = pos;
        pos = crushBlock.Position + crushBlock.movementCounter;
        if (pos != lastPos) {
            text.content = (pos - lastPos).PositionToAbsoluteSpeed();
            Visible = true;
        }
        else {
            Visible = false;
        }
    }

    public override void ClearHistoryData() {
        lastPos = pos = crushBlock.Position;
    }
}

internal sealed class CrushBlockFactory() : RendererFactory<CrushBlock, CrushBlockRenderer>(() => Config.CrushBlock, mode => new CrushBlockRenderer(mode)) {
    public override bool Inherited() => true;
}





