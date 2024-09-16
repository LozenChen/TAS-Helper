
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class TriggerRenderer : AbstractTriggerRenderer {

    public Trigger trigger;
    public TriggerRenderer(RenderMode mode) : base(mode) { }

    public override string Name() => trigger.GetType().Name;

    public override string GetStaticInfo() => TriggerInfoHelper.GetStaticInfo(trigger);

    public override bool HasDynamicInfo() => TriggerInfoHelper.HasDynamicInfo(trigger);
    public override string GetDynamicInfo() => TriggerInfoHelper.GetDynamicInfo(trigger);

    public override void Added(Entity entity) {
        trigger = entity as Trigger;
        base.Added(entity);
    }
}

internal class TriggerFactory : IRendererFactory {
    public Type GetTargetType() => typeof(Trigger);


    public bool Inherited() => true;
    public RenderMode Mode() => Config.Trigger;
    public void AddComponent(Entity entity) {
        entity.Add(new TriggerRenderer(Mode()).SleepWhenUltraFastforward());
    }
}

#region TriggerLikeEntity
internal class RespawnTargetTriggerRenderer : AbstractTriggerRenderer {

    public RespawnTargetTrigger trigger;
    public RespawnTargetTriggerRenderer(RenderMode mode) : base(mode) { }

    public override string Name() => "RespawnTargetTrigger*";

    public override string GetStaticInfo() => trigger.Target.IntVector2ToString();

    public override bool HasDynamicInfo() => false;
    public override string GetDynamicInfo() => "";

    public override void Added(Entity entity) {
        trigger = entity as RespawnTargetTrigger;
        base.Added(entity);
    }
}

internal class RespawnTargetTriggerFactory : IRendererFactory {
    public Type GetTargetType() => typeof(RespawnTargetTrigger);

    public bool Inherited() => false;
    public RenderMode Mode() => Config.Trigger;
    public void AddComponent(Entity entity) {
        entity.Add(new RespawnTargetTriggerRenderer(Mode()).SleepWhenUltraFastforward());
    }
}

internal class SpawnFacingTriggerRenderer : AbstractTriggerRenderer {

    public SpawnFacingTrigger trigger;
    public SpawnFacingTriggerRenderer(RenderMode mode) : base(mode) { }

    public override string Name() => "SpawnFacingTrigger*";

    public override string GetStaticInfo() => trigger.Facing.ToString();

    public override bool HasDynamicInfo() => false;
    public override string GetDynamicInfo() => "";

    public override void Added(Entity entity) {
        trigger = entity as SpawnFacingTrigger;
        base.Added(entity);
    }
}

internal class SpawnFacingTriggerFactory : IRendererFactory {
    public Type GetTargetType() => typeof(SpawnFacingTrigger);

    public bool Inherited() => false;
    public RenderMode Mode() => Config.Trigger;
    public void AddComponent(Entity entity) {
        entity.Add(new SpawnFacingTriggerRenderer(Mode()).SleepWhenUltraFastforward());
    }
}

#endregion