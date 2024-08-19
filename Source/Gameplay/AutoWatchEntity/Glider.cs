using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class GliderRenderer : AutoWatchTextRenderer {

    public Glider glider;

    public GliderRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        glider = entity as Glider;
        text.justify = new Vector2(0.5f, 1f);
    }

    public override void UpdateImpl() {
        text.Position = glider.TopCenter - Vector2.UnitY * 6f;
        text.Clear();
        text.Append(glider.Speed.Speed2ToSpeed2());
        if (glider.Hold.cannotHoldTimer > 0f) {
            text.Append($"cannotHold: {glider.Hold.cannotHoldTimer.ToFrame()}");
        }
        SetVisible();
    }
}

internal class GliderFactory : IRendererFactory {
    public Type GetTargetType() => typeof(Glider);


    public bool Inherited() => true;
    public RenderMode Mode() => Config.Glider;
    public bool TryAddComponent(Entity entity) {
        entity.Add(new GliderRenderer(Mode()));
        return true;
    }
}





