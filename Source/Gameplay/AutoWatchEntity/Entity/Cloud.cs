
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class CloudRenderer : AutoWatchTextRenderer {

    public Cloud cloud;
    public CloudRenderer(RenderMode mode) : base(mode, active: true) { }

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

internal sealed class CloudFactory() : RendererFactory<Cloud, CloudRenderer>(() => Config.Cloud, mode => new CloudRenderer(mode)) {
    public override bool Inherited() => true;
}




