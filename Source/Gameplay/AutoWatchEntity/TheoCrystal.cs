using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class TheoCrystalRenderer : AutoWatchTextRenderer {

    public TheoCrystal theo;

    public TheoCrystalRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        theo = entity as TheoCrystal;
    }

    public override void UpdateImpl() {
        text.Position = theo.Center;
        if (theo.Hold.cannotHoldTimer > 0f) {
            text.content = theo.Hold.cannotHoldTimer.ToFrame();
            Visible = true;
        }
        else {
            Visible = false;
        }
    }
}

internal class TheoCrystalFactory : IRendererFactory {
    public Type GetTargetType() => typeof(TheoCrystal);


    public bool Inherited() => true;
    public RenderMode Mode() => Config.TheoCrystal;
    public bool TryAddComponent(Entity entity) {
        entity.Add(new TheoCrystalRenderer(Mode()));
        return true;
    }
}





