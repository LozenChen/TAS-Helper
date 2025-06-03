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

internal class RefillFactory : IRendererFactory {
    public Type GetTargetType() => typeof(Refill);


    public bool Inherited() => true;
    public RenderMode Mode() => Config.Refill;
    public void AddComponent(Entity entity) {
        entity.Add(new RefillRenderer(Mode()).SleepWhileFastForwarding());
    }
}





