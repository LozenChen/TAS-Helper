using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class TheoCrystalRenderer : AutoWatchTextRenderer {

    public TheoCrystal theo;

    public TheoCrystalRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        theo = entity as TheoCrystal;
        text.justify = new Vector2(0.5f, 1f);
    }

    public override void UpdateImpl() {
        text.Position = theo.TopCenter - Vector2.UnitY * 6f;
        text.Clear();
        text.Append(theo.Speed.Speed2ToSpeed2());
        if (theo.Hold.cannotHoldTimer > 0f) {
            text.Append($"cannotHold: {theo.Hold.cannotHoldTimer.ToFrameMinusOne()}"); // Depth = 100
        }
        if (theo.Hold.Holder is { } player && player.minHoldTimer > 0f) {
            int data = player.minHoldTimer.ToFrameData() - 2;
            if (data >= 0) {
                text.Append($"minHoldTimer: {data}");
            }
        }
        SetVisible();
    }
}

internal class TheoCrystalFactory : IRendererFactory {
    public Type GetTargetType() => typeof(TheoCrystal);


    public bool Inherited() => true;
    public RenderMode Mode() => Config.TheoCrystal;
    public void AddComponent(Entity entity) {
        entity.Add(new TheoCrystalRenderer(Mode()));
    }
}





