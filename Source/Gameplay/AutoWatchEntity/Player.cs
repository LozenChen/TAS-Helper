
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class PlayerRenderer : AutoWatchTextRenderer {

    public Player player;

    public StateMachine stateMachine;

    public int State => stateMachine.State;

    public Coroutine currentCoroutine => stateMachine.currentCoroutine;

    public float waitTimer => stateMachine.currentCoroutine.waitTimer;

    public bool wasWaiting = false;
    public PlayerRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        player = entity as Player;
        stateMachine = player.StateMachine;
    }

    public override void UpdateImpl() {
        text.Position = player.Center;
        text.Clear();
        bool flag = false;
        if (currentCoroutine.Active) {
            if (ExcludeDashState && State == StDash) {
                // do nothing
            }
            else if (waitTimer > 0f) {
                text.Append(currentCoroutine.waitTimer.ToFrame());
                flag = true;
            }
            else {
                if (wasWaiting) {
                    text.Append("0");
                }
            }
        }
        wasWaiting = flag;
        SetVisible();
    }

    public static bool ExcludeDashState = true;

    private const int StNormal = 0;

    private const int StClimb = 1;

    private const int StDash = 2;

    private const int StSwim = 3;

    private const int StBoost = 4;

    private const int StRedDash = 5;

    private const int StHitSquash = 6;

    private const int StLaunch = 7;

    private const int StPickup = 8;

    private const int StDreamDash = 9;

    private const int StSummitLaunch = 10;

    private const int StDummy = 11;

    private const int StIntroWalk = 12;

    private const int StIntroJump = 13;

    private const int StIntroRespawn = 14;

    private const int StIntroWakeUp = 15;

    private const int StBirdDashTutorial = 16;

    private const int StFrozen = 17;

    private const int StReflectionFall = 18;

    private const int StStarFly = 19;

    private const int StTempleFall = 20;

    private const int StCassetteFly = 21;

    private const int StAttract = 22;

    private const int StIntroMoonJump = 23;

    private const int StFlingBird = 24;

    private const int StIntroThinkForABit = 25;
}

internal class PlayerFactory : IRendererFactory {
    public Type GetTargetType() => typeof(Player);

    public bool Inherited() => false;
    public RenderMode Mode() => Config.Player;
    public bool TryAddComponent(Entity entity) {
        entity.Add(new PlayerRenderer(Mode()));
        return true;
    }
}





