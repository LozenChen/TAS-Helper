using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class FlagTouchSwitchRenderer : AutoWatchTextRenderer {

    public Entity flagTouchSwitch;

    public Session session;

    public string flag;

    public string contentWhenActivated;

    public string contentWhenUnactivated;

    public bool setTo;

    public int id;

    public bool persistent;

    internal static float defaultTextScale = 0.5f;

    internal static int breaklineLimit = 4;
    public FlagTouchSwitchRenderer(RenderMode mode) : base(mode, active: true) { }

    /*

    public override void Added(Entity entity) {
        base.Added(entity);
        flagTouchSwitch = entity;
        session = entity.SceneAs<Level>().Session;
        flag = entity.GetFieldValue<string>("flag");
        setTo = !entity.GetFieldValue<bool>("inverted");
        persistent = entity.GetFieldValue<bool>("persistent");
        text.Position = flagTouchSwitch.Center;
        text.scale = defaultTextScale;
        string separator = flag.Length > breaklineLimit ? ":\n" : ": ";
        if (session.GetFlag(flag) == setTo) {
            text.content = setTo ? $"Added{separator}{flag}" : $"Removed{separator}{flag}";
            PostActive = hasUpdate = false;
            return;
        }
        id = entity.GetFieldValue<int>("id");
        if (session.GetFlag($"{flag}_switch{id}")) {
            text.content = $"Added:\n{flag}_switch{id}";
            PostActive = hasUpdate = false;
            return;
        }
        contentWhenActivated = setTo ? $"Added{separator}{flag}" : $"Removed{separator}{flag}";
        contentWhenUnactivated = setTo ? $"Add{separator}{flag}" : $"Remove{separator}{flag}";
    }

    public override void UpdateImpl() {
        text.content = session.GetFlag(flag) == setTo ? contentWhenActivated : contentWhenUnactivated;
    }
    */

    // need rewrite. i think it's better to hook at the end of TurnOn function to tell me to update
    // should be like:
    // if permenant, then just "Add(ed): flag_switch_id"
    // if not, then "Add: flag\n [07/50]" if not completed (07/50 = 07 finished, 50 in total)
    // (as we are hooking TurnOn func, we can check all the detailed conditions),
    // and "Added: flag" if completed 
    // if flag is too long, then we write it in another line
}

internal class FlagTouchSwitchFactory : IRendererFactory {

    public Type GetTargetType() => null;

    // public Type GetTargetType() => ModUtils.GetType("MaxHelpingHand", "Celeste.Mod.MaxHelpingHand.Entities.FlagTouchSwitch");

    public bool Inherited() => true;
    public RenderMode Mode() => Config.FlagTouchSwitch;
    public void AddComponent(Entity entity) {
        entity.Add(new FlagTouchSwitchRenderer(Mode()).SleepWhenUltraFastforward());
    }
}





