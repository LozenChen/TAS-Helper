
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class CloudRenderer : AutoWatchTextRenderer {

    public Cloud cloud;
    public CloudRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        cloud = entity as Cloud;
    }

    public override void UpdateImpl() {
        text.Position = cloud.Center;
        if (cloud.respawnTimer > 0f) {
            text.content = cloud.respawnTimer.ToFrame();
            Visible = true;
        }
        else if (cloud.Collidable && cloud.speed != 0f) {
            text.content = cloud.speed.SpeedToSpeed();
            Visible = true;
        }
        else {
            Visible = false;
        }
    }

    public override void ClearHistoryData() {
    }
}

internal class CloudFactory : IRendererFactory {
    public Type GetTargetType() => typeof(Cloud);

    public bool Inherited() => true;
    public RenderMode Mode() => Config.Cloud;
    public void AddComponent(Entity entity) {
        entity.Add(new CloudRenderer(Mode()));
    }
}




