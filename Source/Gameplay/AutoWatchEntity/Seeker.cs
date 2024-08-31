
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class SeekerRenderer : AutoWatchTextRenderer {

    public Seeker seeker;

    public StateMachine stateMachine => seeker.State;
    public SeekerRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        seeker = entity as Seeker;
    }

    public override void UpdateImpl() {
        text.Position = seeker.Center;
        text.Clear();

        text.Append(stateMachine.GetCurrentStateName());
        SetVisible();
    }

    private const int StIdle = 0;

    private const int StPatrol = 1;

    private const int StSpotted = 2;

    private const int StAttack = 3;

    private const int StStunned = 4;

    private const int StSkidding = 5;

    private const int StRegenerate = 6;

    private const int StReturned = 7;
}

internal class SeekerFactory : IRendererFactory {
    public Type GetTargetType() => typeof(Seeker);

    public bool Inherited() => false;
    public RenderMode Mode() => Config.Seeker;
    public void AddComponent(Entity entity) {
        entity.Add(new SeekerRenderer(Mode()).SleepWhenUltraFastforward());
    }
}




