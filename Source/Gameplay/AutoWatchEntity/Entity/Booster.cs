
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

internal class BoosterFactory : IRendererFactory {
    public Type GetTargetType() => typeof(Booster);

    public bool Inherited() => true;
    public RenderMode Mode() => Config.Booster;
    public void AddComponent(Entity entity) {
        entity.Add(new BoosterRenderer(Mode()).SleepWhileFastForwarding());
    }
}