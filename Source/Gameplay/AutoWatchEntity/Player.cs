
using Celeste.Mod.TASHelper.Utils;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class PlayerRenderer : AutoWatchTextRenderer {

    public static bool ExcludeDashState = false;

    public static bool ShowWallBoostTimer = true;

    public static bool ShowDreamDashCanEndTimer = true;


    public Player player;

    public StateMachine stateMachine;

    public int State;

    public Coroutine currentCoroutine => stateMachine.currentCoroutine;

    public float waitTimer => stateMachine.currentCoroutine.waitTimer;

    public bool wasWaiting = false;
    public PlayerRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        player = entity as Player;
        stateMachine = player.StateMachine;
        State = stateMachine.State;
    }

    public override void UpdateImpl() {
        State = stateMachine.State;
        text.Position = player.Center;
        text.Clear();
        bool flag = false;

        // hope in the future i can understand what these codes are
        if (currentCoroutine.Active) {
            if (State == StDash) {
                if (ExcludeDashState) {
                    // do nothing
                }
                else if (!player.StartedDashing){
                    text.Append(currentCoroutine.waitTimer.ToFrameAllowZero());
                }
            }
            else if (waitTimer > 0f) {
                text.Append(currentCoroutine.waitTimer.ToFrame());
                flag = true;
            }
            else if (State == StPickup && currentCoroutine.Current.GetType().FullName == "Monocle.Tween+<Wait>d__45" && currentCoroutine.Current.GetFieldValue("<>4__this") is Tween tween) {
                text.Append((tween.TimeLeft.ToFrameData() + 1).ToString());
                flag = true;
            }
            else if (!wasWaiting && ((State == StStarFly && player.starFlyTransforming) || State == StIntroWalk || State == StIntroJump || State == StIntroMoonJump || State == StIntroThinkForABit)) {
                text.Append("~");
            }
            else if (State == StIntroWakeUp && currentCoroutine.Current.GetType().FullName == "Monocle.Sprite+<PlayUtil>d__40" && currentCoroutine.Current.GetFieldValue("<>4__this") is Sprite sprite) {
                text.Append($"{sprite.CurrentAnimationTotalFrames - sprite.CurrentAnimationFrame}|{(sprite.currentAnimation.Delay - sprite.animationTimer).ToFrameMinusOne()}");
                flag = true;
            }
        }
        else if (State == StIntroRespawn && player.respawnTween is not null) {
            text.Append(player.respawnTween.TimeLeft.ToFrame());
            flag = true;
        }
        else if (State == StNormal && ShowWallBoostTimer && player.wallBoostTimer > 0f) {
            // 约定, 计时以 0 结尾, 0 的下一帧是状态变化, 包括不能 wallboost, 可以 dreamDashEnd
            text.Append($"wb:{player.wallBoostTimer.ToFrameMinusOne()}");
        }
        else if (State == StDreamDash && ShowDreamDashCanEndTimer && player.dreamDashCanEndTimer > 0f) {
            text.Append(player.dreamDashCanEndTimer.ToFrameMinusOne());
        }

        if (!flag && State == StStarFly && !player.starFlyTransforming) { // here the coroutine can by active, also can be inactive, that's why we don't use a "else if"
            text.Append(player.starFlyTimer.ToFrame());
        }

        if (!flag && wasWaiting) {
            text.Append("0");
        }
        wasWaiting = flag;

        // TODO: dashAttackTimer, gliderBoostTimer

        SetVisible();
    }

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





