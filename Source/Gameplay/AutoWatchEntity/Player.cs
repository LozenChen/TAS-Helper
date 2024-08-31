
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class PlayerRenderer : AutoWatchTextRenderer {

    public HiresText textBelow;

    public Player player;

    public StateMachine stateMachine;

    public int State;

    public Coroutine currentCoroutine => stateMachine.currentCoroutine;

    public float waitTimer => stateMachine.currentCoroutine.waitTimer;

    public bool wasWaiting = false;

    public bool flag = false;

    public static Vector2 offset = Vector2.UnitY * 6f; // make sure this is different to that of cutscene

    public int dashAttack = 0;
    public PlayerRenderer(RenderMode mode) : base(mode, active: true, preActive: true) { }

    public override void PreUpdateImpl() {
        text.Clear();
        textBelow.Clear();
        flag = false;
        dashAttack = 0;

        State = stateMachine.State; // get the pre-update state (in case we'll need it someday), later we need to change it to post-update state

        if (player.dashAttackTimer > 0f && Config.ShowDashAttackTimer) {
            dashAttack = player.dashAttackTimer.ToFrameData();
        }
    }

    public override void UpdateImpl() {
        // hope in the future i can understand what these codes are

        State = stateMachine.State; // re-update this field, coz it may have already changed

        if (dashAttack > 0 && State != StRedDash && State != StDreamDash && State != StAttract) {
            if (player.DashAttacking && dashAttack > 0) {
                // if player dash attacks this frame, then we don't show the timer
                textBelow.Append($"dashAttack: {dashAttack - 1}");
            }
            else if (dashAttack == 1 && player.dashAttackTimer < 0f) {
                textBelow.Append("dashAttack: 0");
                // techinically we should show "0" if it's last frame of dash attack, and doesn't actually collide
                // but it's a bit hard to tell if the timer expires naturally, or by colliding
                // we use a hacky way here
                // if the timer expires naturally, then by Engine.DeltaTime = 0.0166667f instead of exactly 1/6, the timer will be a bit smaller than 0
            }
        }


        if (currentCoroutine.Active) {
            if (State == StDash) {
                // technically there's no a DashTimer... though there is a const float DashTime
                if (Config.ShowDashTimer && !player.StartedDashing) {
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
                // if it's a player.DummyWalkToExact, then we show it on the cutscene / NPC / lookout ... instead
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
        else if (State == StNormal && Config.ShowWallBoostTimer && player.wallBoostTimer > 0f) {
            // 约定, 计时以 0 结尾, 0 的下一帧是状态变化, 包括不能 wallboost, 可以 dreamDashEnd
            textBelow.Append($"wallBoost: {player.wallBoostTimer.ToFrameMinusOne()}");
        }
        else if (State == StLaunch && Config.ShowStLaunchSpeed) {
            if (CanDash(player)) {
                textBelow.Append($"StLaunch: {player.Speed.Length():F0}~ > 220");
            }
            else {
                textBelow.Append($"StLaunch: {player.Speed.Length():F0} > 220");
            }
        }
        else if (State == StDreamDash && Config.ShowDreamDashCanEndTimer && player.dreamDashCanEndTimer > 0f) {
            textBelow.Append($"dreamDashCanEnd: {player.dreamDashCanEndTimer.ToFrameMinusOne()}");
        }

        if (!flag && State == StStarFly && !player.starFlyTransforming) { // here the coroutine can by active, also can be inactive, that's why we don't use a "else if"
            text.Append(player.starFlyTimer.ToFrame());
        }

        if (!flag && wasWaiting) {
            text.Append("0");
        }
        wasWaiting = flag;

        text.Position = player.Center;
        textBelow.Position = player.BottomCenter + offset;
        SetVisible();
    }

    private static bool CanDash(Player player) {
        // without button check
        if (player.dashCooldownTimer <= 0f && player.Dashes > 0 && (TalkComponent.PlayerOver == null || !Input.Talk.Pressed)) {
            if (player.LastBooster != null && player.LastBooster.Ch9HubTransition) {
                return !player.LastBooster.BoostingPlayer;
            }
            return true;
        }
        return false;
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

    public override void Added(Entity entity) {
        base.Added(entity);
        HiresLevelRenderer.Add(text = new HiresText("", entity.Position, this));
        HiresLevelRenderer.Add(textBelow = new HiresText("", entity.Position, this));
        player = entity as Player;
        stateMachine = player.StateMachine;
        State = stateMachine.State;
        textBelow.justify = new Microsoft.Xna.Framework.Vector2(0.5f, 0f);
    }

    public override void EntityAdded(Scene scene) {
        base.EntityAdded(scene);
        if (text is not null) {
            HiresLevelRenderer.AddIfNotPresent(text); // without this, PlayerRender may get lost after EventTrigger "ch9_goto_the_future" (first two sides of Farewell)
        }
        if (textBelow is not null) {
            HiresLevelRenderer.AddIfNotPresent(textBelow);
        }
    }

    public override void Removed(Entity entity) {
        base.Removed(entity);
        if (text is not null) {
            HiresLevelRenderer.Remove(text);
        }
        if (textBelow is not null) {
            HiresLevelRenderer.Remove(textBelow);
        }
    }
    public override void EntityRemoved(Scene scene) {
        base.EntityRemoved(scene);
        if (text is not null) {
            HiresLevelRenderer.Remove(text);
        }
        if (textBelow is not null) {
            HiresLevelRenderer.Remove(textBelow);
        }
    }

    public override void OnClone() {
        base.OnClone();
        if (textBelow is not null) {
            HiresLevelRenderer.AddIfNotPresent(textBelow);
        }
    }

    public new void SetVisible() {
        Visible = (text.content != "" || textBelow.content != "");
    }
}

internal class PlayerFactory : IRendererFactory {
    public Type GetTargetType() => typeof(Player);

    public bool Inherited() => false;
    public RenderMode Mode() => Config.Player;
    public void AddComponent(Entity entity) {
        entity.Add(new PlayerRenderer(Mode()).SleepWhenUltraFastforward());
    }
}





