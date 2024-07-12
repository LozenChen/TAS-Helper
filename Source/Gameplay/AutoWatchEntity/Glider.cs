
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class GliderRenderer : AutoWatchTextRenderer{

    public Glider glider;
    public GliderRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        glider = entity as Glider;
    }

    public override void UpdateImpl() {
        text.Position = glider.Center;
        if (glider.Hold.cannotHoldTimer > 0f) {
            text.content = glider.Hold.cannotHoldTimer.ToFrame();
            Visible = true;
        }
        else {
            Visible = false;
        }
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





