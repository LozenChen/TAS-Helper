
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class SwapBlockRenderer : AutoWatchTextRenderer {

    public SwapBlock swapblock;

    public Vector2 lastPos;

    public Vector2 pos;
    public SwapBlockRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        swapblock = entity as SwapBlock;
        lastPos = pos = entity.Position;
    }

    public override void UpdateImpl() {
        text.Position = swapblock.Center;
        lastPos = pos;
        pos = swapblock.Position + swapblock.movementCounter;
        text.Clear();
        if (swapblock.returnTimer > 0f) {
            text.Append(swapblock.returnTimer.ToFrame());
        }
        if (pos != lastPos) {
            text.Append((pos - lastPos).PositionToAbsoluteSpeed());
        }
        SetVisible();
    }

    public override void ClearHistoryData() {
        lastPos = pos = swapblock.Position;
    }
}

internal class SwapBlockFactory : IRendererFactory {
    public Type GetTargetType() => typeof(SwapBlock);

    public bool Inherited() => false;
    public RenderMode Mode() => Config.SwapBlock;
    public void AddComponent(Entity entity) {
        entity.Add(new SwapBlockRenderer(Mode()));
    }
}





