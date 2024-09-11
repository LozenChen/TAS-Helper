
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class TriggerRenderer : AutoWatchTextRenderer {

    public Trigger trigger;

    public bool orig_Visible;
    public TriggerRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        trigger = entity as Trigger;

        text.Position = trigger.Center;
        text.content = trigger.GetType().Name;
        orig_Visible = Visible;
    }

    public override void UpdateImpl() {
        Visible = orig_Visible && !SimplifiedTrigger.IsUnimportantTrigger(trigger);
    }

    public override void ClearHistoryData() {
        orig_Visible = Visible;
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





