using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class SeekerRenderer : AutoWatchText2Renderer {

    public Seeker seeker;

    public StateMachine stateMachine;

    public Coroutine currentCoroutine => stateMachine.currentCoroutine;

    public float waitTimer => stateMachine.currentCoroutine.waitTimer;

    public bool wasWaiting = false;

    public bool flag = false;

    public int State;

    public static Vector2 default_offset = Vector2.UnitY * 6f;
    public SeekerRenderer(RenderMode mode) : base(mode, active: true, preActive: false) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        seeker = entity as Seeker;
        offset = default_offset;
        stateMachine = seeker.State;
        // depth = -200
    }

    public override void UpdateImpl() {
        text.Clear();
        textBelow.Clear();
        bool stateChanged = stateMachine.ChangedStates;
        State = stateMachine.State;
        flag = false;

        if (currentCoroutine.Active) {
            if (waitTimer > 0f) {
                text.Append(currentCoroutine.waitTimer.ToFrame());
                // when seeker attacks, SeekerEffectsController makes timerate decrease to 0.5f, so the number shown here may change dramatically
                flag = true;
            }
            else if (stateChanged && State == StReturned) {
                // first frame of StReturned
                // StReturned coroutine comes from an end of a coroutine, so by OoO there is 1 frame where it's StReturned but the coroutine does not update
                // ============================================
                // StIdle: no switched to by coroutine
                // StPatrol: no coroutine
                // StSpotted: by IdleUpdate / PatrolUpdate / SkiddingUpdate, so no issue
                // StAttack: it's switched to in the end of SpottedCoroutine
                // StStunned: it's switched to by theo's hitSeeker, theo is Depth 100, so StStunned coroutine does not have this issue
                // StSkidding: it's switched to when attack update
                // StRegenerate: it's switched to by GotBouncedOn <- OnBouncePlayer <- PlayerCollider, depth 0, so it's ok
                text.Append((0.3f.ToFrameData() + 1).ToString()); 
                flag = true;
            }
            else if (!wasWaiting) {
                text.Append("~");
            }
        }
        if (!flag && wasWaiting) {
            text.Append("0");
        }
        wasWaiting = flag;


        textBelow.Append(stateMachine.GetCurrentStateName());

        SetVisible();

        text.Position = seeker.Center;
        textBelow.Position = seeker.BottomCenter + offset;
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




