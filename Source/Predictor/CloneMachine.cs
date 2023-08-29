using Celeste.Mod.TASHelper.Gameplay;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.TASHelper.Predictor;

public static class CloneMachine {

    public static void Clone(DummyPlayer dm, Player player) {
        CloneCoroutine(dm, player);

        CloneActorFields(dm, player);

        ClonePlayerFields(dm, player);

        InputManager.FreezeTimer = Engine.FreezeTimer;
    }

    private static void CloneCoroutine(DummyPlayer dm, Player player) {
        dm.StateMachine.ForceState(player.StateMachine.State);
        if (typeof(StateMachine).GetFieldInfo("currentCoroutine") is not { } fieldInfo ||
            fieldInfo.GetValue(player.StateMachine) is not Coroutine source || 
            fieldInfo.GetValue(dm.StateMachine) is not Coroutine target) {
            return;
        }
        
        if (player.StateMachine.State == Player.StDash) {
            // all StDash are entered via XUpdate. By order of operation in StateMachine.Update(), Coroutine.Update() is also called in this frame 
            target.Update();
            if (Core.PlayerStateBeforeUpdate == Player.StDash && Core.PlayerStateBeforeUpdate == Player.StDash){
                if (player.GetFieldValue<float>("dashCooldownTimer") < 0.2f) {
                    target.Update();
                }
                if (!source.Active) {
                    target.Jump();
                    target.Update();
                }
            }
        }
        //todo: support more coroutines


        target.RemoveOnComplete = source.RemoveOnComplete;
        target.UseRawDeltaTime = source.UseRawDeltaTime;
        target.SetFieldValue("waitTimer", source.GetFieldValue<float>("waitTimer"));
        // Celeste.Commands.Log("waitTimer:" + target.GetFieldValue<float>("waitTimer"));
        target.SetFieldValue("ended", source.GetFieldValue<bool>("ended"));
        target.SetFieldValue("Finished", source.Finished);
        target.Active = source.Active;
        target.Visible = source.Visible;


    }

    private static void CloneActorFields(DummyPlayer dm, Player player) {
        dm.Position = player.Position;
        CloneValue<Vector2>(dm, player, "movementCounter");
        dm.TreatNaive = player.TreatNaive;
        dm.IgnoreJumpThrus = player.IgnoreJumpThrus;
        dm.AllowPushing = player.AllowPushing;
        dm.LiftSpeedGraceTime = player.LiftSpeedGraceTime;
        CloneValue<Vector2>(dm, player, "currentLiftSpeed");
        CloneValue<Vector2>(dm, player, "lastLiftSpeed");
        CloneValue<float>(dm, player, "liftSpeedTimer");

        static void CloneValue<T>(Actor dm, Actor player, string field) {
            dm.SetFieldValue(field, player.GetFieldValue<T>(field));
        }
    }

    private static void ClonePlayerFields(DummyPlayer dm, Player player) {
        if (Engine.Scene.Entities.FindFirst<WindController>() is { } windController && typeof(Entity).GetFieldInfo("actualDepth") is { } actualDepthGetter) {
            dm.UpdateBeforeWind = (double)actualDepthGetter.GetValue(player) > (double)actualDepthGetter.GetValue(windController);
        }
        dm.HasStrawberry = false;
        foreach (Follower follower in player.Leader.Followers) {
            if (follower.Entity is StrawberrySeed) {
                dm.HasStrawberry = true;
                break;
            }
        }
        if (SaveData.Instance.Assists.Hiccups && Engine.Scene is Level level) {
            dm.HiccupRandom.SetFieldValue("inext", level.HiccupRandom.GetFieldValue<int>("inext"));
            dm.HiccupRandom.SetFieldValue("inextp", level.HiccupRandom.GetFieldValue<int>("inextp"));
            int[] seedArray = new int[56];
            for (int i = 0; i< 56; i++) {
                seedArray[i] = level.HiccupRandom.GetFieldValue<int[]>("SeedArray")[i];
            }
            dm.HiccupRandom.SetFieldValue("SeedArray", seedArray); 
        }
        dm.BeforePlayer.Clear();
        dm.AfterPlayer.Clear();
        dm.TransitionOrDead = false;

        ClonePlayerSimpleFields(dm, player);
        CloneAdditionalEntityFields(dm, player);
        dm.Collider =  CloneHitbox(player.Collider as Hitbox);
        dm.hurtbox = CloneHitbox(player.GetFieldValue<Hitbox>("hurtbox"));

        Hitbox CloneHitbox(Hitbox hitbox) {
            if (EqualValue(hitbox, dm.normalHitbox)){
                return dm.normalHitbox;
            }else if (EqualValue(hitbox, dm.normalHurtbox)) {
                return dm.normalHurtbox;
            }else if (EqualValue(hitbox, dm.duckHitbox)) {
                return dm.duckHitbox;
            }else if (EqualValue(hitbox, dm.duckHurtbox)) {
                return dm.duckHurtbox;
            }else if (EqualValue(hitbox, dm.starFlyHitbox)) {
                return dm.starFlyHitbox;
            }else if (EqualValue(hitbox, dm.starFlyHurtbox)) {
                return dm.starFlyHurtbox;
            }
            throw new Exception("Unexpected hitbox");
        }

        bool EqualValue(Hitbox h1, Hitbox h2) {
            return h1.Position == h2.Position && h1.Width == h2.Width && h1.Height == h2.Height;
        }

    }

    private static void CloneAdditionalEntityFields(DummyPlayer dm, Player player) {
        // include booster, holding, flingbird, strawberry (which suppress climbhop) ...

    }


    private static void ClonePlayerSimpleFields(DummyPlayer dm, Player player) {
        dm.Speed = player.Speed;
        dm.Facing = player.Facing;
        dm.Dashes = player.Dashes;
        dm.Stamina = player.Stamina;
        dm.StartedDashing = player.StartedDashing;
        dm.PreviousPosition = player.PreviousPosition;
        dm.OverrideDashDirection = player.OverrideDashDirection;
        dm.IntroWalkDirection = player.GetFieldValue<Facings>("IntroWalkDirection");
        dm.JustRespawned = player.JustRespawned;
        dm.EnforceLevelBounds = player.EnforceLevelBounds;
        dm.wasOnGround = player.GetFieldValue<bool>("wasOnGround");
        dm.holdCannotDuck = player.GetFieldValue<bool>("holdCannotDuck");
        dm.windMovedUp = false;
        dm.jumpGraceTimer = player.GetFieldValue<float>("jumpGraceTimer");
        dm.AutoJump = player.AutoJump;
        dm.AutoJumpTimer = player.AutoJumpTimer;
        dm.varJumpSpeed = player.GetFieldValue<float>("varJumpSpeed");
        dm.varJumpTimer = player.GetFieldValue<float>("varJumpTimer");
        dm.moveX = player.GetFieldValue<int>("moveX");
        dm.forceMoveX = player.GetFieldValue<int>("forceMoveX");
        dm.forceMoveXTimer = player.GetFieldValue<float>("forceMoveXTimer");
        dm.hopWaitX = player.GetFieldValue<int>("hopWaitX");
        dm.hopWaitXSpeed = player.GetFieldValue<float>("hopWaitXSpeed");
        dm.dashCooldownTimer = player.GetFieldValue<float>("dashCooldownTimer");
        dm.dashRefillCooldownTimer = player.GetFieldValue<float>("dashRefillCooldownTimer");
        dm.DashDir = player.DashDir;
        dm.wallSlideTimer = player.GetFieldValue<float>("wallSlideTimer");
        dm.wallSlideDir = player.GetFieldValue<int>("wallSlideDir");
        dm.climbNoMoveTimer = player.GetFieldValue<float>("climbNoMoveTimer");
        dm.carryOffset = player.GetFieldValue<Vector2>("carryOffset");
        dm.wallSpeedRetentionTimer = player.GetFieldValue<float>("wallSpeedRetentionTimer");
        dm.wallSpeedRetained = player.GetFieldValue<float>("wallSpeedRetained");
        dm.wallBoostDir = player.GetFieldValue<int>("wallBoostDir");
        dm.wallBoostTimer = player.GetFieldValue<float>("wallBoostTimer");
        dm.maxFall = player.GetFieldValue<float>("maxFall");
        dm.dashAttackTimer = player.GetFieldValue<float>("dashAttackTimer");
        dm.gliderBoostTimer = player.GetFieldValue<float>("gliderBoostTimer");
        dm.dashStartedOnGround = player.GetFieldValue<bool>("dashStartedOnGround");
        dm.lastClimbMove = player.GetFieldValue<int>("lastClimbMovce");
        dm.noWindTimer = player.GetFieldValue<float>("noWindTimer");
        dm.dreamDashCanEndTimer = player.GetFieldValue<float>("dreamDashCanEndTimer");
        dm.climbHopSolidPosition = player.GetFieldValue<Vector2>("climbHopSolidPosition");
        dm.climbHopSolid = player.GetFieldValue<Solid>("climbHopSolid");
        dm.minHoldTimer = player.GetFieldValue<float>("minHoldTimer");
        dm.calledDashEvents = player.GetFieldValue<bool>("calledDashEvents");
        dm.launched = player.GetFieldValue<bool>("launched");
        dm.launchedTimer = player.GetFieldValue<float>("launchedTimer");
        dm.canCurveDash = player.GetFieldValue<bool>("canCurveDash");
        dm.lowFrictionStopTimer = player.GetFieldValue<float>("lowFrictionStopTimer");
        dm.hiccupTimer = player.GetFieldValue<float>("hiccupTimer");
        dm.gliderBoostDir = player.GetFieldValue<Vector2>("gliderBoostDir");
        dm.explodeLaunchBoostTimer = player.GetFieldValue<float>("explodeLaunchBoostTimer");
        dm.explodeLaunchBoostSpeed = player.GetFieldValue<float>("explodeLaunchBoostSpeed");
        dm.demoDashed = player.GetFieldValue<bool>("demoDashed");
        dm.IntroType = player.IntroType;
        dm.wallBoosting = player.GetFieldValue<bool>("wallBoosting");
        dm.beforeDashSpeed = player.GetFieldValue<Vector2>("beforeDashSpeed");
        dm.boostTarget = player.GetFieldValue<Vector2>("boostTarget");
        dm.boostRed = player.GetFieldValue<bool>("boostRed");
        dm.hitSquashNoMoveTimer = player.GetFieldValue<float>("hitSquashNoMoveTimer");
        dm.launchApproachX = player.GetFieldValue<float?>("launchApproachX");
        dm.summitLaunchTargetX = player.GetFieldValue<float>("summitLaunchTargetX");
        dm.dreamBlock = player.GetFieldValue<DreamBlock>("dreamBlock");
        dm.dreamJump = player.GetFieldValue<bool>("dreamJump");
        dm.starFlyTimer = player.GetFieldValue<float>("starFlyTimer");
        dm.starFlyTransforming = player.GetFieldValue<bool>("starFlyTransforming");
        dm.starFlySpeedLerp = player.GetFieldValue<float>("starFlySpeedLerp");
        dm.starFlyLastDir = player.GetFieldValue<Vector2>("starFlyLastDir");
        dm.cassetteFlyCurve = player.GetFieldValue<SimpleCurve>("cassetteFlyCurve");
        dm.cassetteFlyLerp = player.GetFieldValue<float>("cassetteFlyLerp");
        dm.attractTo = player.GetFieldValue<Vector2>("attractTo");
        dm.DummyMoving = player.DummyMoving;
        dm.DummyGravity = player.DummyGravity;
        dm.DummyFriction = player.DummyFriction;
        dm.DummyMaxspeed = player.DummyMaxspeed;
        dm.OverrideIntroType = player.OverrideIntroType;
    }
}
