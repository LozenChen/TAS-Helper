
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class TriggerRenderer : AbstractTriggerRenderer {

    public Trigger trigger;
    public TriggerRenderer(RenderMode mode) : base(mode) { }

    public override string Name() => ShowModName ? GetNameWithModName(trigger.GetType()) : trigger.GetType().Name;

    public static bool ShowModName = true;
    public static string GetNameWithModName(Type type) {
        if (type.Assembly == typeof(Celeste).Assembly) {
            return type.Name;
        }
        else {
            return type.Name + "@" + type.Assembly.GetName().Name;
        }
    }

    public override string GetStaticInfo() => TriggerInfoHelper.GetStaticInfo(trigger);

    public override bool HasDynamicInfo() => TriggerInfoHelper.HasDynamicInfo(trigger);
    public override string GetDynamicInfo() => TriggerInfoHelper.GetDynamicInfo(trigger);

    public override void Added(Entity entity) {
        trigger = entity as Trigger;
        base.Added(entity);
    }
}

internal sealed class TriggerFactory() : RendererFactory<Trigger, TriggerRenderer>(() => Config.Trigger, mode => new TriggerRenderer(mode)) {
    public override bool Inherited() => true;
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

internal sealed class RespawnTargetTriggerFactory() : RendererFactory<RespawnTargetTrigger, RespawnTargetTriggerRenderer>(() => Config.Trigger, mode => new RespawnTargetTriggerRenderer(mode)) {
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

internal sealed class SpawnFacingTriggerFactory() : RendererFactory<SpawnFacingTrigger, SpawnFacingTriggerRenderer>(() => Config.Trigger, mode => new SpawnFacingTriggerRenderer(mode)) {
}

#endregion