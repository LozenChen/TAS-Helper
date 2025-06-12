using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

// Celeste.ForsakenCitySatellite
// Celeste.ReflectionHeartStatue.Torch
// other mod dash code entity (exclude triggers, as they should be processed under Trigger)


internal class ForsakenCitySatelliteRenderer : AutoWatchTextRenderer {

    public ForsakenCitySatellite satellite;
    public ForsakenCitySatelliteRenderer(RenderMode mode) : base(mode, active: true) { }

    public string dashcode = "";

    public override void Added(Entity entity) {
        base.Added(entity);
        satellite = entity as ForsakenCitySatellite;
        if (!satellite.enabled) {
            RemoveSelf();
            return;
        }
        text.scale = AbstractTriggerRenderer.defaultTextScale;
        text.Position = satellite.birdFlyPosition;
        dashcode = "DashCode: " + string.Join(",", ForsakenCitySatellite.Code.Select(DashCode.ToCode)) + "\n";
    }

    public override void UpdateImpl() {
        Visible = satellite.enabled;
        if (!Visible) {
            RemoveSelf();
            return;
        }
        text.content = dashcode + "Current: " + string.Join(",", satellite.currentInputs.Select(DashCode.ToCode));
    }
}

internal class ForsakenCitySatelliteFactory : IRendererFactory {
    public Type GetTargetType() => typeof(ForsakenCitySatellite);


    public bool Inherited() => false;
    public RenderMode Mode() => Config.DashCodeEntity;
    public void AddComponent(Entity entity) {
        entity.Add(new ForsakenCitySatelliteRenderer(Mode()).SleepWhileFastForwarding());
    }
}


internal class ReflectionHeartStatueRenderer : AutoWatchTextRenderer {

    public ReflectionHeartStatue statue;
    public ReflectionHeartStatueRenderer(RenderMode mode) : base(mode, active: true) { }

    public string dashcode = "";

    public int TorchIndex = -1;

    public override void Added(Entity entity) {
        base.Added(entity);
        statue = entity as ReflectionHeartStatue;
        if (!statue.enabled) {
            RemoveSelf();
            return;
        }
        text.scale = AbstractTriggerRenderer.defaultTextScale;
        text.Position = statue.Position + new Vector2(0f, -52f);
    }

    public override void UpdateImpl() {
        Visible = statue.enabled;
        if (!Visible) {
            RemoveSelf();
            return;
        }
        int i = 1;
        foreach (ReflectionHeartStatue.Torch torch in statue.torches) {
            if (!torch.Activated) {
                if (i > TorchIndex) {
                    TorchIndex = i;
                    dashcode = $"DashCode[{i}]: " + string.Join(",", torch.Code.Select(DashCode.ToCode)) + "\n";
                    text.Position = torch.Position + new Vector2(0, -20f);
                }
                break;
            }
            i++;
        }

        text.content = dashcode + "Current: " + string.Join(",", statue.currentInputs.Select(DashCode.ToCode));
    }
}

internal class ReflectionHeartStatueFactory : IRendererFactory {
    public Type GetTargetType() => typeof(ReflectionHeartStatue);


    public bool Inherited() => false;
    public RenderMode Mode() => Config.DashCodeEntity;
    public void AddComponent(Entity entity) {
        entity.Add(new ReflectionHeartStatueRenderer(Mode()).SleepWhileFastForwarding());
    }
}

