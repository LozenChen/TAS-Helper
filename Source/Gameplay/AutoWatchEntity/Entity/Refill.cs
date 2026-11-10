using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class RefillRenderer : AutoWatchTextRenderer {

    public Refill refill;
    public RefillRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        refill = entity as Refill;
        if (refill.oneUse) {
            RemoveSelf();
        }
    }

    public override void UpdateImpl() {
        text.Position = refill.Center;
        if (refill.respawnTimer > 0f) {
            text.content = refill.respawnTimer.ToFrame();
            Visible = true;
        }
        else {
            Visible = false;
        }
    }
}

internal sealed class RefillFactory() : RendererFactory<Refill, RefillRenderer>(() => Config.Refill, mode => new RefillRenderer(mode)) {
    public override bool Inherited() => true;
}





