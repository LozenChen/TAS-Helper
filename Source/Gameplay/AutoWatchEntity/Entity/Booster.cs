
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class BoosterRenderer : AutoWatchTextRenderer {

    public Booster booster;
    public BoosterRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        booster = entity as Booster;
    }

    public override void UpdateImpl() {
        text.Position = booster.Center;
        if (booster.respawnTimer > 0f) {
            text.content = booster.respawnTimer.ToFrame();
            Visible = true;
        }
        else {
            Visible = false;
        }
    }
}

internal sealed class BoosterFactory() : RendererFactory<Booster, BoosterRenderer>(() => Config.Booster, mode => new BoosterRenderer(mode)) {
    public override bool Inherited() => true;
}