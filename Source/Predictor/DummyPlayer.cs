using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using static Celeste.Level;
using IntroTypes = Celeste.Player.IntroTypes;

namespace Celeste.Mod.TASHelper.Predictor;

public class DummyPlayer : Actor {

    public static DummyPlayer Instance;

    public bool UpdateBeforeWind;

    public bool TransitionOrDead;

    public bool HasStrawberry;

    public Random HiccupRandom = new Random();

    public const float eps = 0.001f;
    public SortedList<float, Coroutine> BeforePlayer = new SortedList<float, Coroutine>();
    public SortedList<float, Coroutine> AfterPlayer = new SortedList<float, Coroutine>();

    public void AddCoroutine(Coroutine routine, float depth) {
        float reverted = -depth;
        if (reverted < 0) {
            while (BeforePlayer.Keys.Contains(reverted)) {
                reverted += eps;
            }
            BeforePlayer.Add(reverted, routine);
        }
        else {
            while (AfterPlayer.Keys.Contains(reverted)) {
                reverted += eps;
            }
            AfterPlayer.Add(reverted, routine);
        }
    }

    public static readonly Vector2 CarryOffsetTarget = new Vector2(0f, -12f);

    public Vector2 Speed;

    public Facings Facing;

    public StateMachine StateMachine;

    public int Dashes;

    public float Stamina = 110f;

    public Vector2 PreviousPosition;

    public Vector2? OverrideDashDirection;

    public Facings IntroWalkDirection;

    public bool JustRespawned;

    public bool EnforceLevelBounds = true;

    public Level level;

    public Collision onCollideH;

    public Collision onCollideV;

    public bool onGround;

    public bool wasOnGround;

    public int moveX;

    public int climbTriggerDir;

    public bool holdCannotDuck;

    public bool windMovedUp;

    public int StrawberryCollectIndex;

    public float StrawberryCollectResetTimer;

    public Hitbox hurtbox;

    public float jumpGraceTimer;

    public bool AutoJump;

    public float AutoJumpTimer;

    public float varJumpSpeed;

    public float varJumpTimer;

    public int forceMoveX;

    public float forceMoveXTimer;

    public int hopWaitX;

    public float hopWaitXSpeed;

    public Vector2 lastAim;

    public float dashCooldownTimer;

    public float dashRefillCooldownTimer;

    public Vector2 DashDir;

    public float wallSlideTimer = 1.2f;

    public int wallSlideDir;

    public float climbNoMoveTimer;

    public Vector2 carryOffset;

    public float wallSpeedRetentionTimer;

    public float wallSpeedRetained;

    public int wallBoostDir;

    public float wallBoostTimer;

    public float maxFall;

    public float dashAttackTimer;

    public float gliderBoostTimer;

    public float highestAirY;

    public bool dashStartedOnGround;

    public int lastClimbMove;

    public float noWindTimer;

    public float dreamDashCanEndTimer;

    public Solid climbHopSolid;

    public Vector2 climbHopSolidPosition;

    public float minHoldTimer;

    public Booster CurrentBooster;

    public Booster LastBooster;

    public bool calledDashEvents;

    public bool launched;

    public float launchedTimer;

    public bool canCurveDash;

    public float lowFrictionStopTimer;

    public float hiccupTimer;

    public Vector2 gliderBoostDir;

    public float explodeLaunchBoostTimer;

    public float explodeLaunchBoostSpeed;

    public bool demoDashed;

    public readonly Hitbox normalHitbox = new Hitbox(8f, 11f, -4f, -11f);

    public readonly Hitbox duckHitbox = new Hitbox(8f, 6f, -4f, -6f);

    public readonly Hitbox normalHurtbox = new Hitbox(8f, 9f, -4f, -11f);

    public readonly Hitbox duckHurtbox = new Hitbox(8f, 4f, -4f, -6f);

    public readonly Hitbox starFlyHitbox = new Hitbox(8f, 8f, -4f, -10f);

    public readonly Hitbox starFlyHurtbox = new Hitbox(6f, 6f, -3f, -9f);

    public IntroTypes IntroType;

    public bool wallBoosting;

    public Vector2 beforeDashSpeed;

    public Vector2 boostTarget;

    public bool boostRed;

    public float hitSquashNoMoveTimer;

    public float? launchApproachX;

    public float summitLaunchTargetX;

    public DreamBlock dreamBlock;

    public bool dreamJump;

    public float starFlyTimer;

    public bool starFlyTransforming;

    public float starFlySpeedLerp;

    public Vector2 starFlyLastDir;

    public FlingBird flingBird;

    public SimpleCurve cassetteFlyCurve;

    public float cassetteFlyLerp;

    public Vector2 attractTo;

    public bool DummyMoving;

    public bool DummyGravity = true;

    public bool DummyFriction = true;

    public bool DummyMaxspeed = true;

    public IntroTypes? OverrideIntroType;

    public bool Dead { get; set; }

    public bool TimePaused {
        get {
            if (Dead) {
                return true;
            }
            int state = StateMachine.State;
            if (state != 10 && (uint)(state - 12) > 3u && state != 25) {
                return false;
            }
            return true;
        }
    }

    public bool InControl {
        get {
            switch (StateMachine.State) {
                default:
                    return true;
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 17:
                case 23:
                case 25:
                    return false;
            }
        }
    }

    public PlayerInventory Inventory {
        get {
            if (level != null && level.Session != null) {
                return level.Session.Inventory;
            }
            return PlayerInventory.Default;
        }
    }

    public bool OnSafeGround { get; set; }

    public bool LoseShards => onGround;

    public int MaxDashes {
        get {
            if (SaveData.Instance.Assists.DashMode != 0) {
                Level obj = level;
                if (obj != null && !obj.InCutscene) {
                    return 2;
                }
            }
            return Inventory.Dashes;
        }
    }

    public Vector2 LiftBoost {
        get {
            Vector2 liftSpeed = base.LiftSpeed;
            if (Math.Abs(liftSpeed.X) > 250f) {
                liftSpeed.X = 250f * (float)Math.Sign(liftSpeed.X);
            }
            if (liftSpeed.Y > 0f) {
                liftSpeed.Y = 0f;
            }
            else if (liftSpeed.Y < -130f) {
                liftSpeed.Y = -130f;
            }
            return liftSpeed;
        }
    }

    public bool Ducking {
        get {
            if (base.Collider != duckHitbox) {
                return base.Collider == duckHurtbox;
            }
            return true;
        }
        set {
            if (value) {
                base.Collider = duckHitbox;
                hurtbox = duckHurtbox;
            }
            else {
                base.Collider = normalHitbox;
                hurtbox = normalHurtbox;
            }
        }
    }

    public bool CanUnDuck {
        get {
            if (!Ducking) {
                return true;
            }
            Collider collider = base.Collider;
            base.Collider = normalHitbox;
            bool result = !CollideCheck<Solid>();
            base.Collider = collider;
            return result;
        }
    }

    public Holdable Holding { get; set; }

    public bool IsTired => CheckStamina < 20f;

    public float CheckStamina {
        get {
            if (wallBoostTimer > 0f) {
                return Stamina + 27.5f;
            }
            return Stamina;
        }
    }

    public bool DashAttacking {
        get {
            if (!(dashAttackTimer > 0f)) {
                return StateMachine.State == 5;
            }
            return true;
        }
    }

    public bool CanDash {
        get {
            if ((Input.CrouchDashPressed || Input.DashPressed) && dashCooldownTimer <= 0f && Dashes > 0 && (TalkComponent.PlayerOver == null || !Input.Talk.Pressed)) {
                if (LastBooster != null && LastBooster.Ch9HubTransition) {
                    return !LastBooster.BoostingPlayer;
                }
                return true;
            }
            return false;
        }
    }

    public bool StartedDashing { get; set; }

    public bool SuperWallJumpAngleCheck {
        get {
            if (Math.Abs(DashDir.X) <= 0.2f) {
                return DashDir.Y <= -0.75f;
            }
            return false;
        }
    }

    public bool AtAttractTarget {
        get {
            if (StateMachine.State == 22) {
                return base.ExactPosition == attractTo;
            }
            return false;
        }
    }

    public bool IsIntroState {
        get {
            switch (StateMachine.State) {
                case 12:
                case 13:
                case 14:
                case 15:
                case 23:
                case 25:
                    return true;
                default:
                    return false;
            }
        }
    }

    public DummyPlayer()
        : base(Vector2.Zero) {
        base.Depth = 0;
        base.Tag = Tags.Persistent;

        base.Collider = normalHitbox;
        hurtbox = normalHurtbox;
        onCollideH = OnCollideH;
        onCollideV = OnCollideV;
        StateMachine = new StateMachine(26);
        StateMachine.SetCallbacks(0, NormalUpdate, null, NormalBegin, NormalEnd);
        StateMachine.SetCallbacks(1, ClimbUpdate, null, ClimbBegin, ClimbEnd);
        StateMachine.SetCallbacks(2, DashUpdate, DashCoroutine, DashBegin, DashEnd);
        StateMachine.SetCallbacks(3, SwimUpdate, null, SwimBegin);
        StateMachine.SetCallbacks(4, BoostUpdate, BoostCoroutine, BoostBegin, BoostEnd);
        StateMachine.SetCallbacks(5, RedDashUpdate, RedDashCoroutine, RedDashBegin, RedDashEnd);
        StateMachine.SetCallbacks(6, HitSquashUpdate, null, HitSquashBegin);
        StateMachine.SetCallbacks(7, LaunchUpdate, null, LaunchBegin);
        StateMachine.SetCallbacks(8, null, PickupCoroutine);
        StateMachine.SetCallbacks(9, DreamDashUpdate, null, DreamDashBegin, DreamDashEnd);
        StateMachine.SetCallbacks(10, SummitLaunchUpdate, null, SummitLaunchBegin);
        StateMachine.SetCallbacks(11, DummyUpdate, null, DummyBegin);
        StateMachine.SetCallbacks(12, NullUpdate, null, null);
        StateMachine.SetCallbacks(13, NullUpdate, null, null);
        StateMachine.SetCallbacks(14, NullUpdate, null, null);
        StateMachine.SetCallbacks(15, NullUpdate, null, null);
        StateMachine.SetCallbacks(16, NullUpdate, null, null);
        StateMachine.SetCallbacks(17, FrozenUpdate);
        StateMachine.SetCallbacks(18, NullUpdate, null, null);
        StateMachine.SetCallbacks(19, StarFlyUpdate, StarFlyCoroutine, StarFlyBegin, StarFlyEnd);
        StateMachine.SetCallbacks(20, NullUpdate, null, null);
        StateMachine.SetCallbacks(21, NullUpdate, null, null);
        StateMachine.SetCallbacks(22, AttractUpdate, null, AttractBegin, AttractEnd);
        StateMachine.SetCallbacks(23, NullUpdate, null, null);
        StateMachine.SetCallbacks(24, FlingBirdUpdate, FlingBirdCoroutine, FlingBirdBegin, FlingBirdEnd);
        StateMachine.SetCallbacks(25, NullUpdate, null, null);
        Add(StateMachine);
        lastAim = Vector2.UnitX;
        Facing = Facings.Right;
        IntroType = IntroTypes.None;
        StateMachine.State = 0;

        Instance = this;
    }

    public override void DebugRender(Camera camera) {
        base.DebugRender(camera);
        Collider collider = base.Collider;
        base.Collider = hurtbox;
        Draw.HollowRect(base.Collider, Color.Lime);
        base.Collider = collider;
    }

    public bool LaunchedBoostCheck() {
        if (LiftBoost.LengthSquared() >= 10000f && Speed.LengthSquared() >= 48400f) {
            launched = true;
            return true;
        }
        launched = false;
        return false;
    }

    public void HiccupJump() {
        switch (StateMachine.State) {
            default:
                StateMachine.State = 0;
                Speed.X = Calc.Approach(Speed.X, 0f, 40f);
                if (Speed.Y > -60f) {
                    varJumpSpeed = (Speed.Y = -60f);
                    varJumpTimer = 0.15f;
                    AutoJump = true;
                    AutoJumpTimer = 0f;
                    if (jumpGraceTimer > 0f) {
                        jumpGraceTimer = 0.6f;
                    }
                }
                break;
            case 1:
                StateMachine.State = 0;
                varJumpSpeed = (Speed.Y = -60f);
                varJumpTimer = 0.15f;
                Speed.X = 130f * (float)(0 - Facing);
                AutoJump = true;
                AutoJumpTimer = 0f;
                break;
            case 19:
                if (Speed.X > 0f) {
                    Speed = Speed.Rotate(0.6981317f);
                }
                else {
                    Speed = Speed.Rotate(-0.6981317f);
                }
                break;
            case 5:
            case 9:
                if (Speed.X < 0f || (Speed.X == 0f && Speed.Y < 0f)) {
                    Speed = Speed.Rotate(0.17453292f);
                }
                else {
                    Speed = Speed.Rotate(-0.17453292f);
                }
                break;
            case 4:
            case 7:
            case 22:
                break;
            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
            case 15:
            case 16:
            case 17:
            case 18:
            case 21:
            case 24:
                return;
        }
    }

    public void Jump(bool particles = true, bool playSfx = true) {
        Input.Jump.ConsumeBuffer();
        jumpGraceTimer = 0f;
        varJumpTimer = 0.2f;
        AutoJump = false;
        dashAttackTimer = 0f;
        gliderBoostTimer = 0f;
        wallSlideTimer = 1.2f;
        wallBoostTimer = 0f;
        Speed.X += 40f * (float)moveX;
        Speed.Y = -105f;
        Speed += LiftBoost;
        varJumpSpeed = Speed.Y;
        LaunchedBoostCheck();
    }

    public void SuperJump() {
        Input.Jump.ConsumeBuffer();
        jumpGraceTimer = 0f;
        varJumpTimer = 0.2f;
        AutoJump = false;
        dashAttackTimer = 0f;
        gliderBoostTimer = 0f;
        wallSlideTimer = 1.2f;
        wallBoostTimer = 0f;
        Speed.X = 260f * (float)Facing;
        Speed.Y = -105f;
        Speed += LiftBoost;
        gliderBoostTimer = 0.55f;

        if (Ducking) {
            Ducking = false;
            Speed.X *= 1.25f;
            Speed.Y *= 0.5f;

            gliderBoostDir = Calc.AngleToVector((float)Math.PI * -3f / 16f, 1f);
        }
        else {
            gliderBoostDir = Calc.AngleToVector(-(float)Math.PI / 4f, 1f);

        }
        varJumpSpeed = Speed.Y;
        launched = true;

    }

    public bool WallJumpCheck(int dir) {
        int num = 3;
        bool flag = DashAttacking && DashDir.X == 0f && DashDir.Y == -1f;
        if (flag) {
            Spikes.Directions directions = ((dir <= 0) ? Spikes.Directions.Right : Spikes.Directions.Left);
            foreach (Spikes entity in level.Tracker.GetEntities<Spikes>()) {
                if (entity.Direction == directions && CollideCheck(entity, Position + Vector2.UnitX * dir * 5f)) {
                    flag = false;
                    break;
                }
            }
        }
        if (flag) {
            num = 5;
        }
        if (ClimbBoundsCheck(dir) && !ClimbBlocker.EdgeCheck(level, this, dir * num)) {
            return CollideCheck<Solid>(Position + Vector2.UnitX * dir * num);
        }
        return false;
    }

    public void WallJump(int dir) {
        orig_WallJump(dir);
    }

    public void SuperWallJump(int dir) {
        Ducking = false;
        Input.Jump.ConsumeBuffer();
        jumpGraceTimer = 0f;
        varJumpTimer = 0.25f;
        AutoJump = false;
        dashAttackTimer = 0f;
        gliderBoostTimer = 0.55f;
        gliderBoostDir = -Vector2.UnitY;
        wallSlideTimer = 1.2f;
        wallBoostTimer = 0f;
        Speed.X = 170f * (float)dir;
        Speed.Y = -160f;
        Speed += LiftBoost;
        varJumpSpeed = Speed.Y;
        launched = true;

    }

    public void ClimbJump() {
        if (!onGround) {
            Stamina -= 27.5f;
        }
        dreamJump = false;
        Jump(particles: false, playSfx: false);
        if (moveX == 0) {
            wallBoostDir = 0 - Facing;
            wallBoostTimer = 0.2f;
        }
    }

    public void Bounce(float fromY) {
        if (StateMachine.State == 4 && CurrentBooster != null) {
            CurrentBooster.PlayerReleased();
            CurrentBooster = null;
        }
        Collider collider = base.Collider;
        base.Collider = normalHitbox;
        MoveVExact((int)(fromY - base.Bottom));
        if (!Inventory.NoRefills) {
            RefillDash();
        }
        RefillStamina();
        StateMachine.State = 0;
        jumpGraceTimer = 0f;
        varJumpTimer = 0.2f;
        AutoJump = true;
        AutoJumpTimer = 0.1f;
        dashAttackTimer = 0f;
        gliderBoostTimer = 0f;
        wallSlideTimer = 1.2f;
        wallBoostTimer = 0f;
        varJumpSpeed = (Speed.Y = -140f);
        launched = false;
        base.Collider = collider;
    }

    public void SuperBounce(float fromY) {
        if (StateMachine.State == 4 && CurrentBooster != null) {
            CurrentBooster.PlayerReleased();
            CurrentBooster = null;
        }
        Collider collider = base.Collider;
        base.Collider = normalHitbox;
        MoveV(fromY - base.Bottom);
        if (!Inventory.NoRefills) {
            RefillDash();
        }
        RefillStamina();
        StateMachine.State = 0;
        jumpGraceTimer = 0f;
        varJumpTimer = 0.2f;
        AutoJump = true;
        AutoJumpTimer = 0f;
        dashAttackTimer = 0f;
        gliderBoostTimer = 0f;
        wallSlideTimer = 1.2f;
        wallBoostTimer = 0f;
        Speed.X = 0f;
        varJumpSpeed = (Speed.Y = -185f);
        launched = false;
        base.Collider = collider;
    }

    public bool SideBounce(int dir, float fromX, float fromY) {
        if (Math.Abs(Speed.X) > 240f && Math.Sign(Speed.X) == dir) {
            return false;
        }
        Collider collider = base.Collider;
        base.Collider = normalHitbox;
        MoveV(Calc.Clamp(fromY - base.Bottom, -4f, 4f));
        if (dir > 0) {
            MoveH(fromX - base.Left);
        }
        else if (dir < 0) {
            MoveH(fromX - base.Right);
        }
        if (!Inventory.NoRefills) {
            RefillDash();
        }
        RefillStamina();
        StateMachine.State = 0;
        jumpGraceTimer = 0f;
        varJumpTimer = 0.2f;
        AutoJump = true;
        AutoJumpTimer = 0f;
        dashAttackTimer = 0f;
        gliderBoostTimer = 0f;
        wallSlideTimer = 1.2f;
        forceMoveX = dir;
        forceMoveXTimer = 0.3f;
        wallBoostTimer = 0f;
        launched = false;
        Speed.X = 240f * (float)dir;
        varJumpSpeed = (Speed.Y = -140f);
        base.Collider = collider;
        return true;
    }

    public void Rebound(int direction = 0) {
        Speed.X = (float)direction * 120f;
        Speed.Y = -120f;
        varJumpSpeed = Speed.Y;
        varJumpTimer = 0.15f;
        AutoJump = true;
        AutoJumpTimer = 0f;
        dashAttackTimer = 0f;
        gliderBoostTimer = 0f;
        wallSlideTimer = 1.2f;
        wallBoostTimer = 0f;
        launched = false;
        lowFrictionStopTimer = 0.15f;
        forceMoveXTimer = 0f;
        StateMachine.State = 0;
    }

    public void ReflectBounce(Vector2 direction) {
        if (direction.X != 0f) {
            Speed.X = direction.X * 220f;
        }
        if (direction.Y != 0f) {
            Speed.Y = direction.Y * 220f;
        }
        AutoJumpTimer = 0f;
        dashAttackTimer = 0f;
        gliderBoostTimer = 0f;
        wallSlideTimer = 1.2f;
        wallBoostTimer = 0f;
        launched = false;
        dashAttackTimer = 0f;
        gliderBoostTimer = 0f;
        forceMoveXTimer = 0f;
        StateMachine.State = 0;
    }

    public bool RefillDash() {
        if (Dashes < MaxDashes) {
            Dashes = MaxDashes;
            return true;
        }
        return false;
    }

    public bool UseRefill(bool twoDashes) {
        int num = MaxDashes;
        if (twoDashes) {
            num = 2;
        }
        if (Dashes < num || Stamina < 20f) {
            Dashes = num;
            RefillStamina();
            return true;
        }
        return false;
    }

    public void RefillStamina() {
        Stamina = 110f;
    }

    public PlayerDeadBody Die(Vector2 direction, bool evenIfInvincible = false, bool registerDeathInStats = true) {
        PlayerDeadBody playerDeadBody = orig_Die(direction, evenIfInvincible, registerDeathInStats);
        TransitionOrDead = true;
        return playerDeadBody;
    }

    public bool CanUnDuckAt(Vector2 at) {
        Vector2 position = Position;
        Position = at;
        bool canUnDuck = CanUnDuck;
        Position = position;
        return canUnDuck;
    }

    public bool DuckFreeAt(Vector2 at) {
        Vector2 position = Position;
        Collider collider = base.Collider;
        Position = at;
        base.Collider = duckHitbox;
        bool result = !CollideCheck<Solid>();
        Position = position;
        base.Collider = collider;
        return result;
    }

    public void Duck() {
        base.Collider = duckHitbox;
    }

    public void UnDuck() {
        base.Collider = normalHitbox;
    }

    public void UpdateCarry() {
        if (Holding != null) {
            if (Holding.Scene == null) {
                Holding = null;
            }
            else {
                // todo
                // Holding.Carry(Position + carryOffset + Vector2.UnitY * Sprite.CarryYOffset);
                Holding.Carry(Position + carryOffset + Vector2.UnitY * (-1));
            }
        }
    }

    public void Swat(int dir) {
        if (Holding != null) {
            Holding.Release(new Vector2(0.8f * (float)dir, -0.25f));
            Holding = null;
        }
    }

    public bool Pickup(Holdable pickup) {
        if (Dead) {
            return false;
        }
        return orig_Pickup(pickup);
    }

    public void Throw() {
        if (Holding != null) {
            if (Input.MoveY.Value == 1) {
                Drop();
            }
            else {
                Holding.Release(Vector2.UnitX * (float)Facing);
                Speed.X += 80f * (float)(0 - Facing);
            }
            Holding = null;
        }
    }

    public void Drop() {
        if (Holding != null) {
            Holding.Release(Vector2.Zero);
            Holding = null;
        }
    }

    public void StartJumpGraceTime() {
        jumpGraceTimer = 0.1f;
    }

    public override bool IsRiding(Solid solid) {
        if (StateMachine.State == 23) {
            return false;
        }
        if (StateMachine.State == 9) {
            return CollideCheck(solid);
        }
        if (StateMachine.State == 1 || StateMachine.State == 6) {
            return CollideCheck(solid, Position + Vector2.UnitX * (float)Facing);
        }
        if (climbTriggerDir != 0) {
            return CollideCheck(solid, Position + Vector2.UnitX * climbTriggerDir);
        }
        return base.IsRiding(solid);
    }

    public override bool IsRiding(JumpThru jumpThru) {
        if (StateMachine.State == 9) {
            return false;
        }
        if (StateMachine.State != 1 && Speed.Y >= 0f) {
            return base.IsRiding(jumpThru);
        }
        return false;
    }

    public bool BounceCheck(float y) {
        return base.Bottom <= y + 3f;
    }

    public void PointBounce(Vector2 from) {
        if (StateMachine.State == 2) {
            StateMachine.State = 0;
        }
        if (StateMachine.State == 4 && CurrentBooster != null) {
            CurrentBooster.PlayerReleased();
        }
        RefillDash();
        RefillStamina();
        Vector2 vector = (base.Center - from).SafeNormalize();
        if (vector.Y > -0.2f && vector.Y <= 0.4f) {
            vector.Y = -0.2f;
        }
        Speed = vector * 220f;
        Speed.X *= 1.5f;
        if (Math.Abs(Speed.X) < 100f) {
            if (Speed.X == 0f) {
                Speed.X = (float)(0 - Facing) * 100f;
            }
            else {
                Speed.X = (float)Math.Sign(Speed.X) * 100f;
            }
        }
    }

    public void WindMove(Vector2 move) {
        if (StateMachine.State != 22) {
            orig_WindMove(move);
        }
    }

    public void OnCollideH(CollisionData data) {
        canCurveDash = false;
        if (StateMachine.State == 19) {
            if (starFlyTimer < 0.2f) {
                Speed.X = 0f;
                return;
            }
            Speed.X *= -0.5f;
        }
        else {
            if (StateMachine.State == 9) {
                return;
            }
            /*
            if (DashAttacking && data.Hit != null && data.Hit.OnDashCollide != null && data.Direction.X == (float)Math.Sign(DashDir.X)) {
                DashCollisionResults dashCollisionResults = data.Hit.OnDashCollide(this, data.Direction);
                if (dashCollisionResults == DashCollisionResults.NormalOverride) {
                    dashCollisionResults = DashCollisionResults.NormalCollision;
                }
                else if (StateMachine.State == 5) {
                    dashCollisionResults = DashCollisionResults.Ignore;
                }
                switch (dashCollisionResults) {
                    case DashCollisionResults.Rebound:
                        Rebound(-Math.Sign(Speed.X));
                        return;
                    case DashCollisionResults.Bounce:
                        ReflectBounce(new Vector2(-Math.Sign(Speed.X), 0f));
                        return;
                    case DashCollisionResults.Ignore:
                        return;
                }
            }*/
            if (StateMachine.State == 2 || StateMachine.State == 5) {
                if (onGround && DuckFreeAt(Position + Vector2.UnitX * Math.Sign(Speed.X))) {
                    Ducking = true;
                    return;
                }
                if (Speed.Y == 0f && Speed.X != 0f) {
                    for (int i = 1; i <= 4; i++) {
                        for (int num = 1; num >= -1; num -= 2) {
                            Vector2 vector = new Vector2(Math.Sign(Speed.X), i * num);
                            Vector2 vector2 = Position + vector;
                            if (!CollideCheck<Solid>(vector2) && CollideCheck<Solid>(vector2 - Vector2.UnitY * num) && !DashCorrectCheck(vector)) {
                                MoveVExact(i * num);
                                MoveHExact(Math.Sign(Speed.X));
                                return;
                            }
                        }
                    }
                }
            }
            if (DreamDashCheck(Vector2.UnitX * Math.Sign(Speed.X))) {
                StateMachine.State = 9;
                dashAttackTimer = 0f;
                gliderBoostTimer = 0f;
                return;
            }
            if (wallSpeedRetentionTimer <= 0f) {
                wallSpeedRetained = Speed.X;
                wallSpeedRetentionTimer = 0.06f;
            }
            if (data.Hit != null && data.Hit.OnCollide != null) {
                data.Hit.OnCollide(data.Direction);
            }
            Speed.X = 0f;
            dashAttackTimer = 0f;
            gliderBoostTimer = 0f;
            if (StateMachine.State == 5) {
                StateMachine.State = 6;
            }
        }
    }

    public void OnCollideV(CollisionData data) {
        canCurveDash = false;
        if (StateMachine.State == 19) {
            if (starFlyTimer < 0.2f) {
                Speed.Y = 0f;
                return;
            }
            Speed.Y *= -0.5f;
        }
        else if (StateMachine.State == 3) {
            Speed.Y = 0f;
        }
        else {
            if (StateMachine.State == 9) {
                return;
            }
            /*
            if (data.Hit != null && data.Hit.OnDashCollide != null) {
                if (DashAttacking && data.Direction.Y == (float)Math.Sign(DashDir.Y)) {
                    DashCollisionResults dashCollisionResults = data.Hit.OnDashCollide(this, data.Direction);
                    if (StateMachine.State == 5) {
                        dashCollisionResults = DashCollisionResults.Ignore;
                    }
                    switch (dashCollisionResults) {
                        case DashCollisionResults.Rebound:
                            Rebound();
                            return;
                        case DashCollisionResults.Bounce:
                            ReflectBounce(new Vector2(0f, -Math.Sign(Speed.Y)));
                            return;
                        case DashCollisionResults.Ignore:
                            return;
                    }
                }
                else if (StateMachine.State == 10) {
                    data.Hit.OnDashCollide(this, data.Direction);
                    return;
                }
            }
            */
            if (Speed.Y > 0f) {
                if ((StateMachine.State == 2 || StateMachine.State == 5) && !dashStartedOnGround) {
                    if (Speed.X <= 0.01f) {
                        for (int num = -1; num >= -4; num--) {
                            if (!OnGround(Position + new Vector2(num, 0f))) {
                                MoveHExact(num);
                                MoveVExact(1);
                                return;
                            }
                        }
                    }
                    if (Speed.X >= -0.01f) {
                        for (int i = 1; i <= 4; i++) {
                            if (!OnGround(Position + new Vector2(i, 0f))) {
                                MoveHExact(i);
                                MoveVExact(1);
                                return;
                            }
                        }
                    }
                }
                if (DreamDashCheck(Vector2.UnitY * Math.Sign(Speed.Y))) {
                    StateMachine.State = 9;
                    dashAttackTimer = 0f;
                    gliderBoostTimer = 0f;
                    return;
                }
                if (DashDir.X != 0f && DashDir.Y > 0f && Speed.Y > 0f) {
                    DashDir.X = Math.Sign(DashDir.X);
                    DashDir.Y = 0f;
                    Speed.Y = 0f;
                    Speed.X *= 1.2f;
                    Ducking = true;
                }
            }
            else {
                if (Speed.Y < 0f) {
                    int num3 = 4;
                    if (DashAttacking && Math.Abs(Speed.X) < 0.01f) {
                        num3 = 5;
                    }
                    if (Speed.X <= 0.01f) {
                        for (int j = 1; j <= num3; j++) {
                            if (!CollideCheck<Solid>(Position + new Vector2(-j, -1f))) {
                                Position += new Vector2(-j, -1f);
                                return;
                            }
                        }
                    }
                    if (Speed.X >= -0.01f) {
                        for (int k = 1; k <= num3; k++) {
                            if (!CollideCheck<Solid>(Position + new Vector2(k, -1f))) {
                                Position += new Vector2(k, -1f);
                                return;
                            }
                        }
                    }
                    if (varJumpTimer < 0.15f) {
                        varJumpTimer = 0f;
                    }
                }
                if (DreamDashCheck(Vector2.UnitY * Math.Sign(Speed.Y))) {
                    StateMachine.State = 9;
                    dashAttackTimer = 0f;
                    gliderBoostTimer = 0f;
                    return;
                }
            }
            if (data.Hit != null && data.Hit.OnCollide != null) {
                data.Hit.OnCollide(data.Direction);
            }
            dashAttackTimer = 0f;
            gliderBoostTimer = 0f;
            Speed.Y = 0f;
            if (StateMachine.State == 5) {
                StateMachine.State = 6;
            }
        }
    }

    public bool DreamDashCheck(Vector2 dir) {
        if (Inventory.DreamDash && DashAttacking && (dir.X == (float)Math.Sign(DashDir.X) || dir.Y == (float)Math.Sign(DashDir.Y))) {
            DreamBlock dreamBlock = CollideFirst<DreamBlock>(Position + dir);
            if (dreamBlock != null) {
                if (CollideCheck<Solid, DreamBlock>(Position + dir)) {
                    Vector2 vector = new Vector2(Math.Abs(dir.Y), Math.Abs(dir.X));
                    bool flag;
                    bool flag2;
                    if (dir.X != 0f) {
                        flag = Speed.Y <= 0f;
                        flag2 = Speed.Y >= 0f;
                    }
                    else {
                        flag = Speed.X <= 0f;
                        flag2 = Speed.X >= 0f;
                    }
                    if (flag) {
                        for (int num = -1; num >= -4; num--) {
                            Vector2 at = Position + dir + vector * num;
                            if (!CollideCheck<Solid, DreamBlock>(at)) {
                                Position += vector * num;
                                this.dreamBlock = dreamBlock;
                                return true;
                            }
                        }
                    }
                    if (flag2) {
                        for (int i = 1; i <= 4; i++) {
                            Vector2 at2 = Position + dir + vector * i;
                            if (!CollideCheck<Solid, DreamBlock>(at2)) {
                                Position += vector * i;
                                this.dreamBlock = dreamBlock;
                                return true;
                            }
                        }
                    }
                    return false;
                }
                this.dreamBlock = dreamBlock;
                return true;
            }
        }
        return false;
    }

    public void OnBoundsH() {
        Speed.X = 0f;
        if (StateMachine.State == 5) {
            StateMachine.State = 0;
        }
    }

    public void OnBoundsV() {
        Speed.Y = 0f;
        if (StateMachine.State == 5) {
            StateMachine.State = 0;
        }
    }

    protected override void OnSquish(CollisionData data) {
        bool flag = false;
        if (!Ducking && StateMachine.State != 1) {
            flag = true;
            Ducking = true;
            data.Pusher.Collidable = true;
            if (!CollideCheck<Solid>()) {
                data.Pusher.Collidable = false;
                return;
            }
            Vector2 position = Position;
            Position = data.TargetPosition;
            if (!CollideCheck<Solid>()) {
                data.Pusher.Collidable = false;
                return;
            }
            Position = position;
            data.Pusher.Collidable = false;
        }
        if (!TrySquishWiggle(data, 3, 5)) {
            bool evenIfInvincible = false;
            if (data.Pusher != null && data.Pusher.SquishEvenInAssistMode) {
                evenIfInvincible = true;
            }
            Die(Vector2.Zero, evenIfInvincible);
        }
        else if (flag && CanUnDuck) {
            Ducking = false;
        }
    }

    public void NormalBegin() {
        maxFall = 160f;
    }

    public void NormalEnd() {
        wallBoostTimer = 0f;
        wallSpeedRetentionTimer = 0f;
        hopWaitX = 0;
    }

    public bool ClimbBoundsCheck(int dir) {
        if (base.Left + (float)(dir * 2) >= (float)level.Bounds.Left) {
            return base.Right + (float)(dir * 2) < (float)level.Bounds.Right;
        }
        return false;
    }

    public void ClimbTrigger(int dir) {
        climbTriggerDir = dir;
    }

    public bool ClimbCheck(int dir, int yAdd = 0) {
        if (ClimbBoundsCheck(dir) && !ClimbBlocker.Check(base.Scene, this, Position + Vector2.UnitY * yAdd + Vector2.UnitX * 2f * (float)Facing)) {
            return CollideCheck<Solid>(Position + new Vector2(dir * 2, yAdd));
        }
        return false;
    }

    public int NullUpdate() {
        return 0;
    }
    public int NormalUpdate() {
        if (LiftBoost.Y < 0f && wasOnGround && !onGround && Speed.Y >= 0f) {
            Speed.Y = LiftBoost.Y;
        }
        if (Holding == null) {
            if (Input.GrabCheck && !IsTired && !Ducking) {
                foreach (Holdable component in base.Scene.Tracker.GetComponents<Holdable>()) {
                    if (component.Check(this) && Pickup(component)) {
                        return 8;
                    }
                }
                if (Speed.Y >= 0f && Math.Sign(Speed.X) != 0 - Facing) {
                    if (ClimbCheck((int)Facing)) {
                        Ducking = false;
                        if (!SaveData.Instance.Assists.NoGrabbing) {
                            return 1;
                        }
                        ClimbTrigger((int)Facing);
                    }
                    if (!SaveData.Instance.Assists.NoGrabbing && (float)Input.MoveY < 1f && level.Wind.Y <= 0f) {
                        for (int i = 1; i <= 2; i++) {
                            if (!CollideCheck<Solid>(Position + Vector2.UnitY * -i) && ClimbCheck((int)Facing, -i)) {
                                MoveVExact(-i);
                                Ducking = false;
                                return 1;
                            }
                        }
                    }
                }
            }
            if (CanDash) {
                Speed += LiftBoost;
                return StartDash();
            }
            if (Ducking) {
                if (onGround && (float)Input.MoveY != 1f) {
                    if (CanUnDuck) {
                        Ducking = false;
                    }
                    else if (Speed.X == 0f) {
                        for (int num = 4; num > 0; num--) {
                            if (CanUnDuckAt(Position + Vector2.UnitX * num)) {
                                MoveH(50f * Engine.DeltaTime);
                                break;
                            }
                            if (CanUnDuckAt(Position - Vector2.UnitX * num)) {
                                MoveH(-50f * Engine.DeltaTime);
                                break;
                            }
                        }
                    }
                }
            }
            else if (onGround && (float)Input.MoveY == 1f && Speed.Y >= 0f) {
                Ducking = true;
            }
        }
        else {
            if (!Input.GrabCheck && minHoldTimer <= 0f) {
                Throw();
            }
            if (!Ducking && onGround && (float)Input.MoveY == 1f && Speed.Y >= 0f && !holdCannotDuck) {
                Drop();
                Ducking = true;
            }
            else if (onGround && Ducking && Speed.Y >= 0f) {
                if (CanUnDuck) {
                    Ducking = false;
                }
                else {
                    Drop();
                }
            }
            else if (onGround && (float)Input.MoveY != 1f && holdCannotDuck) {
                holdCannotDuck = false;
            }
        }
        if (Ducking && onGround) {
            Speed.X = Calc.Approach(Speed.X, 0f, 500f * Engine.DeltaTime);
        }
        else {
            float num2 = (onGround ? 1f : 0.65f);
            if (onGround && level.CoreMode == Session.CoreModes.Cold) {
                num2 *= 0.3f;
            }
            if (SaveData.Instance.Assists.LowFriction && lowFrictionStopTimer <= 0f) {
                num2 *= (onGround ? 0.35f : 0.5f);
            }
            float num3;
            if (Holding != null && Holding.SlowRun) {
                num3 = 70f;
            }
            else if (Holding != null && Holding.SlowFall && !onGround) {
                num3 = 108.00001f;
                num2 *= 0.5f;
            }
            else {
                num3 = 90f;
            }
            if (level.InSpace) {
                num3 *= 0.6f;
            }
            if (Math.Abs(Speed.X) > num3 && Math.Sign(Speed.X) == moveX) {
                Speed.X = Calc.Approach(Speed.X, num3 * (float)moveX, 400f * num2 * Engine.DeltaTime);
            }
            else {
                Speed.X = Calc.Approach(Speed.X, num3 * (float)moveX, 1000f * num2 * Engine.DeltaTime);
            }
        }
        float num4 = 160f;
        float num5 = 240f;
        if (level.InSpace) {
            num4 *= 0.6f;
            num5 *= 0.6f;
        }
        if (Holding != null && Holding.SlowFall && forceMoveXTimer <= 0f) {
            maxFall = Calc.Approach(target: ((float)Input.GliderMoveY == 1f) ? 120f : ((windMovedUp && (float)Input.GliderMoveY == -1f) ? (-32f) : (((float)Input.GliderMoveY == -1f) ? 24f : ((!windMovedUp) ? 40f : 0f))), val: maxFall, maxMove: 300f * Engine.DeltaTime);
        }
        else if ((float)Input.MoveY == 1f && Speed.Y >= num4) {
            maxFall = Calc.Approach(maxFall, num5, 300f * Engine.DeltaTime);
        }
        else {
            maxFall = Calc.Approach(maxFall, num4, 300f * Engine.DeltaTime);
        }
        if (!onGround) {
            float target2 = maxFall;
            if (Holding != null && Holding.SlowFall) {
                holdCannotDuck = (float)Input.MoveY == 1f;
            }
            if ((moveX == (int)Facing || (moveX == 0 && Input.GrabCheck)) && Input.MoveY.Value != 1) {
                if (Speed.Y >= 0f && wallSlideTimer > 0f && Holding == null && ClimbBoundsCheck((int)Facing) && CollideCheck<Solid>(Position + Vector2.UnitX * (float)Facing) && !ClimbBlocker.EdgeCheck(level, this, (int)Facing) && CanUnDuck) {
                    Ducking = false;
                    wallSlideDir = (int)Facing;
                }
                if (wallSlideDir != 0) {
                    if (Input.GrabCheck) {
                        ClimbTrigger(wallSlideDir);
                    }
                    if (wallSlideTimer > 0.6f && ClimbBlocker.Check(level, this, Position + Vector2.UnitX * wallSlideDir)) {
                        wallSlideTimer = 0.6f;
                    }
                    target2 = MathHelper.Lerp(160f, 20f, wallSlideTimer / 1.2f);
                }
            }
            float num7 = ((Math.Abs(Speed.Y) < 40f && (Input.Jump.Check || AutoJump)) ? 0.5f : 1f);
            if (Holding != null && Holding.SlowFall && forceMoveXTimer <= 0f) {
                num7 *= 0.5f;
            }
            if (level.InSpace) {
                num7 *= 0.6f;
            }
            Speed.Y = Calc.Approach(Speed.Y, target2, 900f * num7 * Engine.DeltaTime);
        }
        if (varJumpTimer > 0f) {
            if (AutoJump || Input.Jump.Check) {
                Speed.Y = Math.Min(Speed.Y, varJumpSpeed);
            }
            else {
                varJumpTimer = 0f;
            }
        }
        if (Input.Jump.Pressed && (TalkComponent.PlayerOver == null || !Input.Talk.Pressed)) {
            Water water = null;
            if (jumpGraceTimer > 0f) {
                Jump();
            }
            else if (CanUnDuck) {
                bool canUnDuck = CanUnDuck;
                if (canUnDuck && WallJumpCheck(1)) {
                    if (Facing == Facings.Right && Input.GrabCheck && !SaveData.Instance.Assists.NoGrabbing && Stamina > 0f && Holding == null && !ClimbBlocker.Check(base.Scene, this, Position + Vector2.UnitX * 3f)) {
                        ClimbJump();
                    }
                    else if (DashAttacking && SuperWallJumpAngleCheck) {
                        SuperWallJump(-1);
                    }
                    else {
                        WallJump(-1);
                    }
                }
                else if (canUnDuck && WallJumpCheck(-1)) {
                    if (Facing == Facings.Left && Input.GrabCheck && !SaveData.Instance.Assists.NoGrabbing && Stamina > 0f && Holding == null && !ClimbBlocker.Check(base.Scene, this, Position + Vector2.UnitX * -3f)) {
                        ClimbJump();
                    }
                    else if (DashAttacking && SuperWallJumpAngleCheck) {
                        SuperWallJump(1);
                    }
                    else {
                        WallJump(1);
                    }
                }
                else if ((water = CollideFirst<Water>(Position + Vector2.UnitY * 2f)) != null) {
                    Jump();
                    water.TopSurface.DoRipple(Position, 1f);
                }
            }
        }
        return 0;
    }

    public void ClimbBegin() {
        AutoJump = false;
        Speed.X = 0f;
        Speed.Y *= 0.2f;
        wallSlideTimer = 1.2f;
        climbNoMoveTimer = 0.1f;
        wallBoostTimer = 0f;
        lastClimbMove = 0;
        for (int i = 0; i < 2; i++) {
            if (CollideCheck<Solid>(Position + Vector2.UnitX * (float)Facing)) {
                break;
            }
            Position += Vector2.UnitX * (float)Facing;
        }
    }

    public void ClimbEnd() {
        wallSpeedRetentionTimer = 0f;
    }

    public int ClimbUpdate() {
        climbNoMoveTimer -= Engine.DeltaTime;
        if (onGround) {
            Stamina = 110f;
        }
        if (Input.Jump.Pressed && (!Ducking || CanUnDuck)) {
            if (moveX == 0 - Facing) {
                WallJump(0 - Facing);
            }
            else {
                ClimbJump();
            }
            return 0;
        }
        if (CanDash) {
            Speed += LiftBoost;
            return StartDash();
        }
        if (!Input.GrabCheck) {
            Speed += LiftBoost;
            return 0;
        }
        if (!CollideCheck<Solid>(Position + Vector2.UnitX * (float)Facing)) {
            if (Speed.Y < 0f) {
                if (wallBoosting) {
                    Speed += LiftBoost;
                }
                else {
                    ClimbHop();
                }
            }
            return 0;
        }
        WallBooster wallBooster = WallBoosterCheck();
        if (climbNoMoveTimer <= 0f && wallBooster != null) {
            wallBoosting = true;

            Speed.Y = Calc.Approach(Speed.Y, -160f, 600f * Engine.DeltaTime);
            base.LiftSpeed = Vector2.UnitY * Math.Max(Speed.Y, -80f);
        }
        else {
            wallBoosting = false;

            float num = 0f;
            bool flag = false;
            if (climbNoMoveTimer <= 0f) {
                if (ClimbBlocker.Check(base.Scene, this, Position + Vector2.UnitX * (float)Facing)) {
                    flag = true;
                }
                else if (Input.MoveY.Value == -1) {
                    num = -45f;
                    if (CollideCheck<Solid>(Position - Vector2.UnitY) || (ClimbHopBlockedCheck() && SlipCheck(-1f))) {
                        if (Speed.Y < 0f) {
                            Speed.Y = 0f;
                        }
                        num = 0f;
                        flag = true;
                    }
                    else if (SlipCheck()) {
                        ClimbHop();
                        return 0;
                    }
                }
                else if (Input.MoveY.Value == 1) {
                    num = 80f;
                    if (onGround) {
                        if (Speed.Y > 0f) {
                            Speed.Y = 0f;
                        }
                        num = 0f;
                    }
                }
                else {
                    flag = true;
                }
            }
            else {
                flag = true;
            }
            lastClimbMove = Math.Sign(num);
            if (flag && SlipCheck()) {
                num = 30f;
            }
            Speed.Y = Calc.Approach(Speed.Y, num, 900f * Engine.DeltaTime);
        }
        if (Input.MoveY.Value != 1 && Speed.Y > 0f && !CollideCheck<Solid>(Position + new Vector2((float)Facing, 1f))) {
            Speed.Y = 0f;
        }
        if (climbNoMoveTimer <= 0f) {
            if (lastClimbMove == -1) {
                Stamina -= 45.454544f * Engine.DeltaTime;
            }
            else {
                if (lastClimbMove == 0) {
                    Stamina -= 10f * Engine.DeltaTime;
                }
            }
        }
        if (Stamina <= 0f) {
            Speed += LiftBoost;
            return 0;
        }
        return 1;
    }

    public WallBooster WallBoosterCheck() {
        if (ClimbBlocker.Check(base.Scene, this, Position + Vector2.UnitX * (float)Facing)) {
            return null;
        }
        foreach (WallBooster entity in base.Scene.Tracker.GetEntities<WallBooster>()) {
            if (entity.Facing == Facing && CollideCheck(entity)) {
                return entity;
            }
        }
        return null;
    }

    public void ClimbHop() {
        climbHopSolid = CollideFirst<Solid>(Position + Vector2.UnitX * (float)Facing);
        if (climbHopSolid != null) {
            climbHopSolidPosition = climbHopSolid.Position;
            hopWaitX = (int)Facing;
            hopWaitXSpeed = (float)Facing * 100f;
        }
        else {
            hopWaitX = 0;
            Speed.X = (float)Facing * 100f;
        }
        lowFrictionStopTimer = 0.15f;
        Speed.Y = Math.Min(Speed.Y, -120f);
        forceMoveX = 0;
        forceMoveXTimer = 0.2f;
        noWindTimer = 0.3f;
    }

    public bool SlipCheck(float addY = 0f) {
        Vector2 vector = ((Facing != Facings.Right) ? (base.TopLeft - Vector2.UnitX + Vector2.UnitY * (4f + addY)) : (base.TopRight + Vector2.UnitY * (4f + addY)));
        if (!base.Scene.CollideCheck<Solid>(vector)) {
            return !base.Scene.CollideCheck<Solid>(vector + Vector2.UnitY * (-4f + addY));
        }
        return false;
    }

    public bool ClimbHopBlockedCheck() {
        /*
        foreach (Follower follower in Leader.Followers) {
            if (follower.Entity is StrawberrySeed) {
                return true;
            }
        }
        */
        if (HasStrawberry) {
            return true;
        }
        foreach (LedgeBlocker component in base.Scene.Tracker.GetComponents<LedgeBlocker>()) {
            if (component.HopBlockCheck(this)) {
                return true;
            }
        }
        if (CollideCheck<Solid>(Position - Vector2.UnitY * 6f)) {
            return true;
        }
        return false;
    }

    public bool JumpThruBoostBlockedCheck() {
        /*
        foreach (LedgeBlocker component in base.Scene.Tracker.GetComponents<LedgeBlocker>()) {
            if (component.JumpThruBoostCheck(this)) {
                return true;
            }
        }
        */
        return false;
    }

    public bool DashCorrectCheck(Vector2 add) {
        Vector2 position = Position;
        Collider collider = base.Collider;
        Position += add;
        base.Collider = hurtbox;
        foreach (LedgeBlocker component in base.Scene.Tracker.GetComponents<LedgeBlocker>()) {
            if (component.DashCorrectCheck(this)) {
                Position = position;
                base.Collider = collider;
                return true;
            }
        }
        Position = position;
        base.Collider = collider;
        return false;
    }

    public int StartDash() {
        Dashes = Math.Max(0, Dashes - 1);
        demoDashed = Input.CrouchDashPressed;
        Input.Dash.ConsumeBuffer();
        Input.CrouchDash.ConsumeBuffer();
        return 2;
    }

    public void CallDashEvents() {
        if (calledDashEvents) {
            return;
        }
        calledDashEvents = true;
        // CurrentBooster.PlayerBoosted(this, DashDir);
        CurrentBooster = null;
    }

    public void DashBegin() {
        calledDashEvents = false;
        dashStartedOnGround = onGround;
        launched = false;
        canCurveDash = true;
        if (Engine.TimeRate > 0.25f) {
            InputManager.Freeze(0.05f);
        }
        dashCooldownTimer = 0.2f;
        dashRefillCooldownTimer = 0.1f;
        StartedDashing = true;
        wallSlideTimer = 1.2f;

        dashAttackTimer = 0.3f;
        gliderBoostTimer = 0.55f;
        if (SaveData.Instance.Assists.SuperDashing) {
            dashAttackTimer += 0.15f;
        }
        beforeDashSpeed = Speed;
        Speed = Vector2.Zero;
        DashDir = Vector2.Zero;
        if (!onGround && Ducking && CanUnDuck) {
            Ducking = false;
        }
        else if (!Ducking && (demoDashed || Input.MoveY.Value == 1)) {
            Ducking = true;
        }
    }


    public void DashEnd() {
        CallDashEvents();
        demoDashed = false;
    }

    public int DashUpdate() {
        StartedDashing = false;

        if (SaveData.Instance.Assists.SuperDashing && canCurveDash && Input.Aim.Value != Vector2.Zero && Speed != Vector2.Zero) {
            Vector2 aimVector = Input.GetAimVector();
            aimVector = CorrectDashPrecision(aimVector);
            float num = Vector2.Dot(aimVector, Speed.SafeNormalize());
            if (num >= -0.1f && num < 0.99f) {
                Speed = Speed.RotateTowards(aimVector.Angle(), 4.1887903f * Engine.DeltaTime);
                DashDir = Speed.SafeNormalize();
                DashDir = CorrectDashPrecision(DashDir);
            }
        }
        if (SaveData.Instance.Assists.SuperDashing && CanDash) {
            StartDash();
            StateMachine.ForceState(2);
            return 2;
        }
        if (Holding == null && DashDir != Vector2.Zero && Input.GrabCheck && !IsTired && CanUnDuck) {
            foreach (Holdable component in base.Scene.Tracker.GetComponents<Holdable>()) {
                if (component.Check(this) && Pickup(component)) {
                    return 8;
                }
            }
        }
        if (Math.Abs(DashDir.Y) < 0.1f) {
            foreach (JumpThru entity in base.Scene.Tracker.GetEntities<JumpThru>()) {
                if (CollideCheck(entity) && base.Bottom - entity.Top <= 6f && !DashCorrectCheck(Vector2.UnitY * (entity.Top - base.Bottom))) {
                    MoveVExact((int)(entity.Top - base.Bottom));
                }
            }
            if (CanUnDuck && Input.Jump.Pressed && jumpGraceTimer > 0f) {
                SuperJump();
                return 0;
            }
        }
        if (SuperWallJumpAngleCheck) {
            if (Input.Jump.Pressed && CanUnDuck) {
                if (WallJumpCheck(1)) {
                    SuperWallJump(-1);
                    return 0;
                }
                if (WallJumpCheck(-1)) {
                    SuperWallJump(1);
                    return 0;
                }
            }
        }
        else if (Input.Jump.Pressed && CanUnDuck) {
            if (WallJumpCheck(1)) {
                if (Facing == Facings.Right && Input.GrabCheck && Stamina > 0f && Holding == null && !ClimbBlocker.Check(base.Scene, this, Position + Vector2.UnitX * 3f)) {
                    ClimbJump();
                }
                else {
                    WallJump(-1);
                }
                return 0;
            }
            if (WallJumpCheck(-1)) {
                if (Facing == Facings.Left && Input.GrabCheck && Stamina > 0f && Holding == null && !ClimbBlocker.Check(base.Scene, this, Position + Vector2.UnitX * -3f)) {
                    ClimbJump();
                }
                else {
                    WallJump(1);
                }
                return 0;
            }
        }
        return 2;
    }

    public Vector2 CorrectDashPrecision(Vector2 dir) {
        if (dir.X != 0f && Math.Abs(dir.X) < 0.001f) {
            dir.X = 0f;
            dir.Y = Math.Sign(dir.Y);
        }
        else if (dir.Y != 0f && Math.Abs(dir.Y) < 0.001f) {
            dir.Y = 0f;
            dir.X = Math.Sign(dir.X);
        }
        return dir;
    }

    public IEnumerator DashCoroutine() {
        Celeste.Commands.Log("1");
        yield return null;
        Celeste.Commands.Log("2");
        Vector2 value = lastAim;
        if (OverrideDashDirection.HasValue) {
            value = OverrideDashDirection.Value;
        }
        Celeste.Commands.Log("3");
        value = CorrectDashPrecision(value);
        Vector2 speed = value * 240f;
        if (Math.Sign(beforeDashSpeed.X) == Math.Sign(speed.X) && Math.Abs(beforeDashSpeed.X) > Math.Abs(speed.X)) {
            speed.X = beforeDashSpeed.X;
        }
        Speed = speed;
        if (CollideCheck<Water>()) {
            Speed *= 0.75f;
        }
        Celeste.Commands.Log(DashDir);
        gliderBoostDir = (DashDir = value);
        Celeste.Commands.Log("4");
        Celeste.Commands.Log(DashDir);
        if (DashDir.X != 0f) {
            Facing = (Facings)Math.Sign(DashDir.X);
        }
        CallDashEvents();

        if (onGround && DashDir.X != 0f && DashDir.Y > 0f && Speed.Y > 0f && (!Inventory.DreamDash || !CollideCheck<DreamBlock>(Position + Vector2.UnitY))) {
            DashDir.X = Math.Sign(DashDir.X);
            DashDir.Y = 0f;
            Speed.Y = 0f;
            Speed.X *= 1.2f;
            Ducking = true;
        }

        if (DashDir.X != 0f && Input.GrabCheck) {
            SwapBlock swapBlock = CollideFirst<SwapBlock>(Position + Vector2.UnitX * Math.Sign(DashDir.X));
            if (swapBlock != null && swapBlock.Direction.X == (float)Math.Sign(DashDir.X)) {
                StateMachine.State = 1;
                Speed = Vector2.Zero;
                yield break;
            }
        }
        Vector2 swapCancel = Vector2.One;
        foreach (SwapBlock entity in Scene.Tracker.GetEntities<SwapBlock>()) {
            if (CollideCheck(entity, Position + Vector2.UnitY) && entity != null && entity.Swapping) {
                if (DashDir.X != 0f && entity.Direction.X == (float)Math.Sign(DashDir.X)) {
                    Speed.X = (swapCancel.X = 0f);
                }
                if (DashDir.Y != 0f && entity.Direction.Y == (float)Math.Sign(DashDir.Y)) {
                    Speed.Y = (swapCancel.Y = 0f);
                }
            }
        }
        if (SaveData.Instance.Assists.SuperDashing) {
            yield return 0.3f;
        }
        else {
            yield return 0.15f;
        }

        AutoJump = true;
        AutoJumpTimer = 0f;
        if (DashDir.Y <= 0f) {
            Speed = DashDir * 160f;
            Speed.X *= swapCancel.X;
            Speed.Y *= swapCancel.Y;
        }
        if (Speed.Y < 0f) {
            Speed.Y *= 0.75f;
        }
        StateMachine.State = 0;
    }

    public bool SwimCheck() {
        if (CollideCheck<Water>(Position + Vector2.UnitY * -8f)) {
            return CollideCheck<Water>(Position);
        }
        return false;
    }

    public bool SwimUnderwaterCheck() {
        return CollideCheck<Water>(Position + Vector2.UnitY * -9f);
    }

    public bool SwimJumpCheck() {
        return !CollideCheck<Water>(Position + Vector2.UnitY * -14f);
    }

    public bool SwimRiseCheck() {
        return !CollideCheck<Water>(Position + Vector2.UnitY * -18f);
    }

    public bool UnderwaterMusicCheck() {
        if (CollideCheck<Water>(Position)) {
            return CollideCheck<Water>(Position + Vector2.UnitY * -12f);
        }
        return false;
    }

    public void SwimBegin() {
        if (Speed.Y > 0f) {
            Speed.Y *= 0.5f;
        }
        Stamina = 110f;
    }

    public int SwimUpdate() {
        if (!SwimCheck()) {
            return 0;
        }
        if (CanUnDuck) {
            Ducking = false;
        }
        if (CanDash) {
            demoDashed = Input.CrouchDashPressed;
            Input.Dash.ConsumeBuffer();
            Input.CrouchDash.ConsumeBuffer();
            return 2;
        }
        bool flag = SwimUnderwaterCheck();
        if (!flag && Speed.Y >= 0f && Input.GrabCheck && !IsTired && CanUnDuck && Math.Sign(Speed.X) != 0 - Facing && ClimbCheck((int)Facing)) {
            if (SaveData.Instance.Assists.NoGrabbing) {
                ClimbTrigger((int)Facing);
            }
            else if (!MoveVExact(-1)) {
                Ducking = false;
                return 1;
            }
        }
        Vector2 value = Input.Feather.Value;
        value = value.SafeNormalize();
        float num = (flag ? 60f : 80f);
        float num2 = 80f;
        if (Math.Abs(Speed.X) > 80f && Math.Sign(Speed.X) == Math.Sign(value.X)) {
            Speed.X = Calc.Approach(Speed.X, num * value.X, 400f * Engine.DeltaTime);
        }
        else {
            Speed.X = Calc.Approach(Speed.X, num * value.X, 600f * Engine.DeltaTime);
        }
        if (value.Y == 0f && SwimRiseCheck()) {
            Speed.Y = Calc.Approach(Speed.Y, -60f, 600f * Engine.DeltaTime);
        }
        else if (value.Y >= 0f || SwimUnderwaterCheck()) {
            if (Math.Abs(Speed.Y) > 80f && Math.Sign(Speed.Y) == Math.Sign(value.Y)) {
                Speed.Y = Calc.Approach(Speed.Y, num2 * value.Y, 400f * Engine.DeltaTime);
            }
            else {
                Speed.Y = Calc.Approach(Speed.Y, num2 * value.Y, 600f * Engine.DeltaTime);
            }
        }
        if (!flag && moveX != 0 && CollideCheck<Solid>(Position + Vector2.UnitX * moveX) && !CollideCheck<Solid>(Position + new Vector2(moveX, -3f))) {
            ClimbHop();
        }
        if (Input.Jump.Pressed && SwimJumpCheck()) {
            Jump();
            return 0;
        }
        return 3;
    }

    public void Boost(Booster booster) {
        StateMachine.State = 4;
        Speed = Vector2.Zero;
        boostTarget = booster.Center;
        boostRed = false;
        LastBooster = (CurrentBooster = booster);
    }

    public void RedBoost(Booster booster) {
        StateMachine.State = 4;
        Speed = Vector2.Zero;
        boostTarget = booster.Center;
        boostRed = true;
        LastBooster = (CurrentBooster = booster);
    }

    public void BoostBegin() {
        if ((SceneAs<Level>()?.Session.MapData.GetMeta()?.TheoInBubble).GetValueOrDefault()) {
            RefillDash();
            RefillStamina();
        }
        else {
            orig_BoostBegin();
        }
    }

    public void BoostEnd() {
        Vector2 vector = (boostTarget - base.Collider.Center).Floor();
        MoveToX(vector.X);
        MoveToY(vector.Y);
    }

    public int BoostUpdate() {
        Vector2 vector = Input.Aim.Value * 3f;
        Vector2 vector2 = Calc.Approach(base.ExactPosition, boostTarget - base.Collider.Center + vector, 80f * Engine.DeltaTime);
        MoveToX(vector2.X);
        MoveToY(vector2.Y);
        if (Input.DashPressed || Input.CrouchDashPressed) {
            demoDashed = Input.CrouchDashPressed;
            Input.Dash.ConsumePress();
            Input.CrouchDash.ConsumeBuffer();
            if (boostRed) {
                return 5;
            }
            return 2;
        }
        return 4;
    }

    public IEnumerator BoostCoroutine() {
        yield return 0.25f;
        if (boostRed) {
            StateMachine.State = 5;
        }
        else {
            StateMachine.State = 2;
        }
    }

    public void RedDashBegin() {
        calledDashEvents = false;
        dashStartedOnGround = false;
        InputManager.Freeze(0.05f);
        dashCooldownTimer = 0.2f;
        dashRefillCooldownTimer = 0.1f;
        StartedDashing = true;
        dashAttackTimer = 0.3f;
        gliderBoostTimer = 0.55f;
        DashDir = (Speed = Vector2.Zero);
        if (!onGround && CanUnDuck) {
            Ducking = false;
        }
    }

    public void RedDashEnd() {
        CallDashEvents();
    }

    public int RedDashUpdate() {
        StartedDashing = false;
        bool flag = LastBooster != null && LastBooster.Ch9HubTransition;
        gliderBoostTimer = 0.05f;
        if (CanDash) {
            return StartDash();
        }
        if (DashDir.Y == 0f) {
            foreach (JumpThru entity in base.Scene.Tracker.GetEntities<JumpThru>()) {
                if (CollideCheck(entity) && base.Bottom - entity.Top <= 6f) {
                    MoveVExact((int)(entity.Top - base.Bottom));
                }
            }
            if (CanUnDuck && Input.Jump.Pressed && jumpGraceTimer > 0f && !flag) {
                SuperJump();
                return 0;
            }
        }
        if (!flag) {
            if (SuperWallJumpAngleCheck) {
                if (Input.Jump.Pressed && CanUnDuck) {
                    if (WallJumpCheck(1)) {
                        SuperWallJump(-1);
                        return 0;
                    }
                    if (WallJumpCheck(-1)) {
                        SuperWallJump(1);
                        return 0;
                    }
                }
            }
            else if (Input.Jump.Pressed && CanUnDuck) {
                if (WallJumpCheck(1)) {
                    if (Facing == Facings.Right && Input.GrabCheck && Stamina > 0f && Holding == null && !ClimbBlocker.Check(base.Scene, this, Position + Vector2.UnitX * 3f)) {
                        ClimbJump();
                    }
                    else {
                        WallJump(-1);
                    }
                    return 0;
                }
                if (WallJumpCheck(-1)) {
                    if (Facing == Facings.Left && Input.GrabCheck && Stamina > 0f && Holding == null && !ClimbBlocker.Check(base.Scene, this, Position + Vector2.UnitX * -3f)) {
                        ClimbJump();
                    }
                    else {
                        WallJump(1);
                    }
                    return 0;
                }
            }
        }
        return 5;
    }

    public IEnumerator RedDashCoroutine() {
        yield return null;
        Speed = CorrectDashPrecision(lastAim) * 240f;
        gliderBoostDir = (DashDir = lastAim);
        if (DashDir.X != 0f) {
            Facing = (Facings)Math.Sign(DashDir.X);
        }
        CallDashEvents();
    }

    public void HitSquashBegin() {
        hitSquashNoMoveTimer = 0.1f;
    }

    public int HitSquashUpdate() {
        Speed.X = Calc.Approach(Speed.X, 0f, 800f * Engine.DeltaTime);
        Speed.Y = Calc.Approach(Speed.Y, 0f, 800f * Engine.DeltaTime);
        if (Input.Jump.Pressed) {
            if (onGround) {
                Jump();
            }
            else if (WallJumpCheck(1)) {
                if (Facing == Facings.Right && Input.GrabCheck && Stamina > 0f && Holding == null && !ClimbBlocker.Check(base.Scene, this, Position + Vector2.UnitX * 3f)) {
                    ClimbJump();
                }
                else {
                    WallJump(-1);
                }
            }
            else if (WallJumpCheck(-1)) {
                if (Facing == Facings.Left && Input.GrabCheck && Stamina > 0f && Holding == null && !ClimbBlocker.Check(base.Scene, this, Position + Vector2.UnitX * -3f)) {
                    ClimbJump();
                }
                else {
                    WallJump(1);
                }
            }
            else {
                Input.Jump.ConsumeBuffer();
            }
            return 0;
        }
        if (CanDash) {
            return StartDash();
        }
        if (Input.GrabCheck && ClimbCheck((int)Facing)) {
            return 1;
        }
        if (hitSquashNoMoveTimer > 0f) {
            hitSquashNoMoveTimer -= Engine.DeltaTime;
            return 6;
        }
        return 0;
    }

    public Vector2 ExplodeLaunch(Vector2 from, bool snapUp = true, bool sidesOnly = false) {
        InputManager.Freeze(0.1f);
        launchApproachX = null;
        Vector2 vector = (base.Center - from).SafeNormalize(-Vector2.UnitY);
        float num = Vector2.Dot(vector, Vector2.UnitY);
        if (snapUp && num <= -0.7f) {
            vector.X = 0f;
            vector.Y = -1f;
        }
        else if (num <= 0.65f && num >= -0.55f) {
            vector.Y = 0f;
            vector.X = Math.Sign(vector.X);
        }
        if (sidesOnly && vector.X != 0f) {
            vector.Y = 0f;
            vector.X = Math.Sign(vector.X);
        }
        Speed = 280f * vector;
        if (Speed.Y <= 50f) {
            Speed.Y = Math.Min(-150f, Speed.Y);
            AutoJump = true;
        }
        if (Speed.X != 0f) {
            if (Input.MoveX.Value == Math.Sign(Speed.X)) {
                explodeLaunchBoostTimer = 0f;
                Speed.X *= 1.2f;
            }
            else {
                explodeLaunchBoostTimer = 0.01f;
                explodeLaunchBoostSpeed = Speed.X * 1.2f;
            }
        }
        SlashFx.Burst(base.Center, Speed.Angle());
        if (!Inventory.NoRefills) {
            RefillDash();
        }
        RefillStamina();
        dashCooldownTimer = 0.2f;
        StateMachine.State = 7;
        return vector;
    }

    public void FinalBossPushLaunch(int dir) {
        launchApproachX = null;
        Speed.X = 0.9f * (float)dir * 280f;
        Speed.Y = -150f;
        AutoJump = true;
        RefillDash();
        RefillStamina();
        dashCooldownTimer = 0.28f;
        StateMachine.State = 7;
    }

    public void BadelineBoostLaunch(float atX) {
        launchApproachX = atX;
        Speed.X = 0f;
        Speed.Y = -330f;
        AutoJump = true;
        if (Holding != null) {
            Drop();
        }
        SlashFx.Burst(base.Center, Speed.Angle());
        RefillDash();
        RefillStamina();
        dashCooldownTimer = 0.2f;
        StateMachine.State = 7;
    }

    public void LaunchBegin() {
        launched = true;
    }

    public int LaunchUpdate() {
        if (launchApproachX.HasValue) {
            MoveTowardsX(launchApproachX.Value, 60f * Engine.DeltaTime);
        }
        if (CanDash) {
            return StartDash();
        }
        if (Input.GrabCheck && !IsTired && !Ducking) {
            foreach (Holdable component in base.Scene.Tracker.GetComponents<Holdable>()) {
                if (component.Check(this) && Pickup(component)) {
                    return 8;
                }
            }
        }
        if (Speed.Y < 0f) {
            Speed.Y = Calc.Approach(Speed.Y, 160f, 450f * Engine.DeltaTime);
        }
        else {
            Speed.Y = Calc.Approach(Speed.Y, 160f, 225f * Engine.DeltaTime);
        }
        Speed.X = Calc.Approach(Speed.X, 0f, 200f * Engine.DeltaTime);
        if (Speed.Length() < 220f) {
            return 0;
        }
        return 7;
    }

    public void SummitLaunch(float targetX) {
        summitLaunchTargetX = targetX;
        StateMachine.State = 10;
    }

    public void SummitLaunchBegin() {
        wallBoostTimer = 0f;
        Speed = -Vector2.UnitY * 240f;
    }

    public int SummitLaunchUpdate() {
        Facing = Facings.Right;
        MoveTowardsX(summitLaunchTargetX, 20f * Engine.DeltaTime);
        Speed = -Vector2.UnitY * 240f;
        return 10;
    }

    public void StopSummitLaunch() {
        StateMachine.State = 0;
        Speed.Y = -140f;
        AutoJump = true;
        varJumpSpeed = Speed.Y;
    }

    public IEnumerator PickupCoroutine() {
        Vector2 oldSpeed = Speed;
        float varJump = varJumpTimer;
        Speed = Vector2.Zero;
        Vector2 begin = Holding.Entity.Position - Position;
        Vector2 carryOffsetTarget = CarryOffsetTarget;
        SimpleCurve curve = new SimpleCurve(control: new Vector2(begin.X + (float)(Math.Sign(begin.X) * 2), CarryOffsetTarget.Y - 2f), begin: begin, end: carryOffsetTarget);
        carryOffset = begin;
        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, 0.16f, start: true);
        tween.OnUpdate = delegate (Tween t) {
            carryOffset = curve.GetPoint(t.Eased);
        };
        Add(tween);
        yield return tween.Wait();
        Speed = oldSpeed;
        Speed.Y = Math.Min(Speed.Y, 0f);
        varJumpTimer = varJump;
        StateMachine.State = 0;
        if (Holding != null && Holding.SlowFall) {
            if (gliderBoostTimer > 0f && gliderBoostDir.Y < 0f) {
                gliderBoostTimer = 0f;
                Speed.Y = Math.Min(Speed.Y, -240f * Math.Abs(gliderBoostDir.Y));
            }
            else if (Speed.Y < 0f) {
                Speed.Y = Math.Min(Speed.Y, -105f);
            }
            if (onGround && (float)Input.MoveY == 1f) {
                holdCannotDuck = true;
            }
        }
    }

    public void DreamDashBegin() {
        Speed = DashDir * 240f;
        TreatNaive = true;
        base.Depth = -12000;
        dreamDashCanEndTimer = 0.1f;
        Stamina = 110f;
        dreamJump = false;
    }

    public void DreamDashEnd() {
        base.Depth = 0;
        if (!dreamJump) {
            AutoJump = true;
            AutoJumpTimer = 0f;
        }
        if (!Inventory.NoRefills) {
            RefillDash();
        }
        RefillStamina();
        TreatNaive = false;
        if (dreamBlock != null) {
            if (DashDir.X != 0f) {
                jumpGraceTimer = 0.1f;
                dreamJump = true;
            }
            else {
                jumpGraceTimer = 0f;
            }
            // dreamBlock.OnPlayerExit(this);
            dreamBlock = null;
        }
    }

    public int DreamDashUpdate() {
        Vector2 position = Position;
        NaiveMove(Speed * Engine.DeltaTime);
        if (dreamDashCanEndTimer > 0f) {
            dreamDashCanEndTimer -= Engine.DeltaTime;
        }
        DreamBlock dreamBlock = CollideFirst<DreamBlock>();
        if (dreamBlock == null) {
            if (DreamDashedIntoSolid()) {
                if (SaveData.Instance.Assists.Invincible) {
                    Position = position;
                    Speed *= -1f;
                }
                else {
                    Die(Vector2.Zero);
                }
            }
            else if (dreamDashCanEndTimer <= 0f) {
                InputManager.Freeze(0.05f);
                if (Input.Jump.Pressed && DashDir.X != 0f) {
                    dreamJump = true;
                    Jump();
                }
                else if (DashDir.Y >= 0f || DashDir.X != 0f) {
                    if (DashDir.X > 0f && CollideCheck<Solid>(Position - Vector2.UnitX * 5f)) {
                        MoveHExact(-5);
                    }
                    else if (DashDir.X < 0f && CollideCheck<Solid>(Position + Vector2.UnitX * 5f)) {
                        MoveHExact(5);
                    }
                    bool flag = ClimbCheck(-1);
                    bool flag2 = ClimbCheck(1);
                    if (Input.GrabCheck && ((moveX == 1 && flag2) || (moveX == -1 && flag))) {
                        Facing = (Facings)moveX;
                        if (!SaveData.Instance.Assists.NoGrabbing) {
                            return 1;
                        }
                        ClimbTrigger(moveX);
                        Speed.X = 0f;
                    }
                }
                return 0;
            }
        }
        else {
            this.dreamBlock = dreamBlock;

        }
        return 9;
    }

    public bool DreamDashedIntoSolid() {
        if (CollideCheck<Solid>()) {
            for (int i = 1; i <= 5; i++) {
                for (int j = -1; j <= 1; j += 2) {
                    for (int k = 1; k <= 5; k++) {
                        for (int l = -1; l <= 1; l += 2) {
                            Vector2 vector = new Vector2(i * j, k * l);
                            if (!CollideCheck<Solid>(Position + vector)) {
                                Position += vector;
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }

    public bool StartStarFly() {
        RefillStamina();
        if (StateMachine.State == 18) {
            return false;
        }
        if (StateMachine.State == 19) {
            starFlyTimer = 2f;

        }
        else {
            StateMachine.State = 19;
        }
        return true;
    }

    public void StarFlyBegin() {
        starFlyTransforming = true;
        starFlyTimer = 2f;
        starFlySpeedLerp = 0f;
        jumpGraceTimer = 0f;
        base.Collider = starFlyHitbox;
        hurtbox = starFlyHurtbox;
    }

    public void StarFlyEnd() {
        StarFlyReturnToNormalHitbox();
    }

    public void StarFlyReturnToNormalHitbox() {
        base.Collider = normalHitbox;
        hurtbox = normalHurtbox;
        if (!CollideCheck<Solid>()) {
            return;
        }
        Vector2 position = Position;
        base.Y -= normalHitbox.Bottom - starFlyHitbox.Bottom;
        if (CollideCheck<Solid>()) {
            Position = position;
            Ducking = true;
            base.Y -= duckHitbox.Bottom - starFlyHitbox.Bottom;
            if (CollideCheck<Solid>()) {
                Position = position;
                Die(Vector2.Zero);
            }
        }
    }

    public IEnumerator StarFlyCoroutine() {
        // TODO: need modify
        /*
        while (Sprite.CurrentAnimationID == "startStarFly") {
            yield return null;
        }
        */
        while (Speed != Vector2.Zero) {
            yield return null;
        }
        yield return 0.1f;

        starFlyTransforming = false;
        starFlyTimer = 2f;
        RefillDash();
        RefillStamina();
        Vector2 vector = Input.Feather.Value;
        if (vector == Vector2.Zero) {
            vector = Vector2.UnitX * (float)Facing;
        }
        Speed = vector * 250f;
        starFlyLastDir = vector;

        while (starFlyTimer > 0.5f) {
            yield return null;
        }

    }

    public int StarFlyUpdate() {

        if (starFlyTransforming) {
            Speed = Calc.Approach(Speed, Vector2.Zero, 1000f * Engine.DeltaTime);
        }
        else {
            Vector2 value = Input.Feather.Value;
            bool flag = false;
            if (value == Vector2.Zero) {
                flag = true;
                value = starFlyLastDir;
            }
            Vector2 vector = Speed.SafeNormalize(Vector2.Zero);
            vector = (starFlyLastDir = ((!(vector == Vector2.Zero)) ? vector.RotateTowards(value.Angle(), 5.5850534f * Engine.DeltaTime) : value));
            float target;
            if (flag) {
                starFlySpeedLerp = 0f;
                target = 91f;
            }
            else if (vector != Vector2.Zero && Vector2.Dot(vector, value) >= 0.45f) {
                starFlySpeedLerp = Calc.Approach(starFlySpeedLerp, 1f, Engine.DeltaTime / 1f);
                target = MathHelper.Lerp(140f, 190f, starFlySpeedLerp);
            }
            else {
                starFlySpeedLerp = 0f;
                target = 140f;
            }

            float val = Speed.Length();
            val = Calc.Approach(val, target, 1000f * Engine.DeltaTime);
            Speed = vector * val;
            if (Input.Jump.Pressed) {
                if (OnGround(3)) {
                    Jump();
                    return 0;
                }
                if (WallJumpCheck(-1)) {
                    WallJump(1);
                    return 0;
                }
                if (WallJumpCheck(1)) {
                    WallJump(-1);
                    return 0;
                }
            }
            if (Input.GrabCheck) {
                bool flag2 = false;
                int dir = 0;
                if (Input.MoveX.Value != -1 && ClimbCheck(1)) {
                    Facing = Facings.Right;
                    dir = 1;
                    flag2 = true;
                }
                else if (Input.MoveX.Value != 1 && ClimbCheck(-1)) {
                    Facing = Facings.Left;
                    dir = -1;
                    flag2 = true;
                }
                if (flag2) {
                    if (SaveData.Instance.Assists.NoGrabbing) {
                        Speed = Vector2.Zero;
                        ClimbTrigger(dir);
                        return 0;
                    }
                    return 1;
                }
            }
            if (CanDash) {
                return StartDash();
            }
            starFlyTimer -= Engine.DeltaTime;
            if (starFlyTimer <= 0f) {
                if (Input.MoveY.Value == -1) {
                    Speed.Y = -100f;
                }
                if (Input.MoveY.Value < 1) {
                    varJumpSpeed = Speed.Y;
                    AutoJump = true;
                    AutoJumpTimer = 0f;
                    varJumpTimer = 0.2f;
                }
                if (Speed.Y > 0f) {
                    Speed.Y = 0f;
                }
                if (Math.Abs(Speed.X) > 140f) {
                    Speed.X = 140f * (float)Math.Sign(Speed.X);
                }
                return 0;
            }
        }
        return 19;
    }

    public bool DoFlingBird(FlingBird bird) {
        if (!Dead && StateMachine.State != 24) {
            flingBird = bird;
            StateMachine.State = 24;
            if (Holding != null) {
                Drop();
            }
            return true;
        }
        return false;
    }

    public void FinishFlingBird() {
        StateMachine.State = 0;
        AutoJump = true;
        forceMoveX = 1;
        forceMoveXTimer = 0.2f;
        Speed = FlingBird.FlingSpeed;
        varJumpTimer = 0.2f;
        varJumpSpeed = Speed.Y;
        launched = true;
    }

    public void FlingBirdBegin() {
        RefillDash();
        RefillStamina();
    }

    public void FlingBirdEnd() {
    }

    public int FlingBirdUpdate() {
        MoveTowardsX(flingBird.X, 250f * Engine.DeltaTime);
        MoveTowardsY(flingBird.Y + 8f + base.Collider.Height, 250f * Engine.DeltaTime);
        return 24;
    }

    public IEnumerator FlingBirdCoroutine() {
        yield break;
    }

    public void StartCassetteFly(Vector2 targetPosition, Vector2 control) {
        StateMachine.State = 21;
        cassetteFlyCurve = new SimpleCurve(Position, targetPosition, control);
        cassetteFlyLerp = 0f;
        Speed = Vector2.Zero;
        if (Holding != null) {
            Drop();
        }
    }

    public void CassetteFlyBegin() {

    }

    public void CassetteFlyEnd() {
    }

    public int CassetteFlyUpdate() {
        return 21;
    }

    public IEnumerator CassetteFlyCoroutine() {

        Depth = -2000000;
        yield return 0.4f;
        while (cassetteFlyLerp < 1f) {

            cassetteFlyLerp = Calc.Approach(cassetteFlyLerp, 1f, 1.6f * Engine.DeltaTime);
            Position = cassetteFlyCurve.GetPoint(Ease.SineInOut(cassetteFlyLerp));
            yield return null;
        }
        Position = cassetteFlyCurve.End;

        yield return 0.2f;

        StateMachine.State = 0;
        Depth = 0;
    }

    public void StartAttract(Vector2 attractTo) {
        this.attractTo = attractTo.Round();
        StateMachine.State = 22;
    }

    public void AttractBegin() {
        Speed = Vector2.Zero;
    }

    public void AttractEnd() {
    }

    public int AttractUpdate() {
        if (Vector2.Distance(attractTo, base.ExactPosition) <= 1.5f) {
            Position = attractTo;
            ZeroRemainderX();
            ZeroRemainderY();
        }
        else {
            Vector2 vector = Calc.Approach(base.ExactPosition, attractTo, 200f * Engine.DeltaTime);
            MoveToX(vector.X);
            MoveToY(vector.Y);
        }
        return 22;
    }

    public void DummyBegin() {
        DummyMoving = false;
        DummyGravity = true;
    }

    public int DummyUpdate() {
        if (CanUnDuck) {
            Ducking = false;
        }
        if (!onGround && DummyGravity) {
            float num = ((Math.Abs(Speed.Y) < 40f && (Input.Jump.Check || AutoJump)) ? 0.5f : 1f);
            if (level.InSpace) {
                num *= 0.6f;
            }
            Speed.Y = Calc.Approach(Speed.Y, 160f, 900f * num * Engine.DeltaTime);
        }
        if (varJumpTimer > 0f) {
            if (AutoJump || Input.Jump.Check) {
                Speed.Y = Math.Min(Speed.Y, varJumpSpeed);
            }
            else {
                varJumpTimer = 0f;
            }
        }
        if (!DummyMoving) {
            if (Math.Abs(Speed.X) > 90f && DummyMaxspeed) {
                Speed.X = Calc.Approach(Speed.X, 90f * (float)Math.Sign(Speed.X), 2500f * Engine.DeltaTime);
            }
            if (DummyFriction) {
                Speed.X = Calc.Approach(Speed.X, 0f, 1000f * Engine.DeltaTime);
            }
        }
        return 11;
    }

    public IEnumerator DummyWalkTo(float x, bool walkBackwards = false, float speedMultiplier = 1f, bool keepWalkingIntoWalls = false) {
        StateMachine.State = 11;
        if (Math.Abs(X - x) > 4f && !Dead) {
            DummyMoving = true;
            if (walkBackwards) {
                Facing = (Facings)Math.Sign(X - x);
            }
            else {
                Facing = (Facings)Math.Sign(x - X);
            }
            while (Math.Abs(x - X) > 4f && Scene != null && (keepWalkingIntoWalls || !CollideCheck<Solid>(Position + Vector2.UnitX * Math.Sign(x - X)))) {
                Speed.X = Calc.Approach(Speed.X, (float)Math.Sign(x - X) * 64f * speedMultiplier, 1000f * Engine.DeltaTime);
                yield return null;
            }

            DummyMoving = false;
        }
    }

    public IEnumerator DummyWalkToExact(int x, bool walkBackwards = false, float speedMultiplier = 1f, bool cancelOnFall = false) {
        StateMachine.State = 11;
        if (X == (float)x) {
            yield break;
        }
        DummyMoving = true;
        if (walkBackwards) {
            Facing = (Facings)Math.Sign(X - (float)x);
        }
        else {
            Facing = (Facings)Math.Sign((float)x - X);
        }
        int last = Math.Sign(X - (float)x);
        while (!Dead && X != (float)x && !CollideCheck<Solid>(Position + new Vector2((float)Facing, 0f)) && (!cancelOnFall || OnGround())) {
            Speed.X = Calc.Approach(Speed.X, (float)Math.Sign((float)x - X) * 64f * speedMultiplier, 1000f * Engine.DeltaTime);
            int num = Math.Sign(X - (float)x);
            if (num != last) {
                X = x;
                break;
            }
            last = num;
            yield return null;
        }
        Speed.X = 0f;
        DummyMoving = false;
    }

    public IEnumerator DummyRunTo(float x, bool fastAnim = false) {
        StateMachine.State = 11;
        if (Math.Abs(X - x) > 4f) {
            DummyMoving = true;

            Facing = (Facings)Math.Sign(x - X);
            while (Math.Abs(X - x) > 4f) {
                Speed.X = Calc.Approach(Speed.X, (float)Math.Sign(x - X) * 90f, 1000f * Engine.DeltaTime);
                yield return null;
            }
            DummyMoving = false;
        }
    }

    public int FrozenUpdate() {
        return 17;
    }


    public int orig_get_MaxDashes() {
        if (SaveData.Instance.Assists.DashMode != 0 && !level.InCutscene) {
            return 2;
        }
        return Inventory.Dashes;
    }

    public void AddedToScene(Scene scene) {
        // we don't call base.Added(Scene scene) so actualDepth is not affected
        // and we don't call Scene.Add(DummyPlayer dp), so only DummyPlayer can detect other entites, but other entities can't detect it
        // (it's not actually in this scene!)
        typeof(Entity).GetPropertyInfo("Scene").SetValue(this, scene);
    }

    public override void Added(Scene scene) {
        AddedToScene(scene);

        if (OverrideIntroType.HasValue) {
            IntroType = OverrideIntroType.Value;
            OverrideIntroType = null;
        }

        level = SceneAs<Level>();
        Dashes = MaxDashes;
        SpawnFacingTrigger spawnFacingTrigger = CollideFirst<SpawnFacingTrigger>();
        if (spawnFacingTrigger != null) {
            Facing = spawnFacingTrigger.Facing;
        }
        else if (base.X > (float)level.Bounds.Center.X && IntroType != IntroTypes.None) {
            Facing = Facings.Left;
        }
        switch (IntroType) {
            case IntroTypes.Respawn:
                StateMachine.State = 14;
                JustRespawned = true;
                break;
            case IntroTypes.WalkInRight:
                IntroWalkDirection = Facings.Right;
                StateMachine.State = 12;
                break;
            case IntroTypes.WalkInLeft:
                IntroWalkDirection = Facings.Left;
                StateMachine.State = 12;
                break;
            case IntroTypes.Jump:
                StateMachine.State = 13;
                break;
            case IntroTypes.WakeUp:
                Facing = Facings.Right;
                StateMachine.State = 15;
                break;
            case IntroTypes.None:
                StateMachine.State = 0;
                break;
            case IntroTypes.Fall:
                StateMachine.State = 18;
                break;
            case IntroTypes.TempleMirrorVoid:
                StateMachine.State = 11;
                break;
            case IntroTypes.ThinkForABit:
                StateMachine.State = 25;
                break;
        }
        IntroType = IntroTypes.Transition;
        PreviousPosition = Position;
    }

    public override void Update() {
        return;
    }
    public void D_Update() {
        foreach (KeyValuePair<float, Coroutine> pair in BeforePlayer) {
            if (pair.Value is { } coroutine && coroutine.Active) {
                coroutine.Update();
            };
        }

        if (!UpdateBeforeWind) {
            WindMove(level.Wind * 0.1f * Engine.DeltaTime);
        }

        if (SaveData.Instance.Assists.InfiniteStamina) {
            Stamina = 110f;
        }
        PreviousPosition = Position;

        climbTriggerDir = 0;
        if (SaveData.Instance.Assists.Hiccups) {
            if (hiccupTimer <= 0f) {
                hiccupTimer = HiccupRandom.Range(1.2f, 1.8f);
            }
            if (Ducking) {
                hiccupTimer -= Engine.DeltaTime * 0.5f;
            }
            else {
                hiccupTimer -= Engine.DeltaTime;
            }
            if (hiccupTimer <= 0f) {
                HiccupJump();
            }
        }
        if (gliderBoostTimer > 0f) {
            gliderBoostTimer -= Engine.DeltaTime;
        }
        if (lowFrictionStopTimer > 0f) {
            lowFrictionStopTimer -= Engine.DeltaTime;
        }
        if (explodeLaunchBoostTimer > 0f) {
            if (Input.MoveX.Value == Math.Sign(explodeLaunchBoostSpeed)) {
                Speed.X = explodeLaunchBoostSpeed;
                explodeLaunchBoostTimer = 0f;
            }
            else {
                explodeLaunchBoostTimer -= Engine.DeltaTime;
            }
        }
        StrawberryCollectResetTimer -= Engine.DeltaTime;
        if (StrawberryCollectResetTimer <= 0f) {
            StrawberryCollectIndex = 0;
        }
        if (JustRespawned && Speed != Vector2.Zero) {
            JustRespawned = false;
        }
        if (StateMachine.State == 9) {
            bool flag2 = (OnSafeGround = false);
            onGround = flag2;
        }
        else if (Speed.Y >= 0f) {
            Platform platform = CollideFirst<Solid>(Position + Vector2.UnitY);
            if (platform == null) {
                platform = CollideFirstOutside<JumpThru>(Position + Vector2.UnitY);
            }
            if (platform != null) {
                onGround = true;
                OnSafeGround = platform.Safe;
            }
            else {
                bool flag2 = (OnSafeGround = false);
                onGround = flag2;
            }
        }
        else {
            bool flag2 = (OnSafeGround = false);
            onGround = flag2;
        }
        if (StateMachine.State == 3) {
            OnSafeGround = true;
        }
        /*
        if (OnSafeGround) {
            foreach (SafeGroundBlocker component in base.Scene.Tracker.GetComponents<SafeGroundBlocker>()) {
                if (component.Check(this)) {
                    OnSafeGround = false;
                    break;
                }
            }
        }
        */
        if (onGround) {
            highestAirY = base.Y;
        }
        else {
            highestAirY = Math.Min(base.Y, highestAirY);
        }
        if (wallSlideDir != 0) {
            wallSlideTimer = Math.Max(wallSlideTimer - Engine.DeltaTime, 0f);
            wallSlideDir = 0;
        }
        if (wallBoostTimer > 0f) {
            wallBoostTimer -= Engine.DeltaTime;
            if (moveX == wallBoostDir) {
                Speed.X = 130f * (float)moveX;
                Stamina += 27.5f;
                wallBoostTimer = 0f;
            }
        }
        if (onGround && StateMachine.State != 1) {
            AutoJump = false;
            Stamina = 110f;
            wallSlideTimer = 1.2f;
        }
        if (dashAttackTimer > 0f) {
            dashAttackTimer -= Engine.DeltaTime;
        }
        if (onGround) {
            dreamJump = false;
            jumpGraceTimer = 0.1f;
        }
        else if (jumpGraceTimer > 0f) {
            jumpGraceTimer -= Engine.DeltaTime;
        }
        if (dashCooldownTimer > 0f) {
            dashCooldownTimer -= Engine.DeltaTime;
        }
        if (dashRefillCooldownTimer > 0f) {
            dashRefillCooldownTimer -= Engine.DeltaTime;
        }
        else if (SaveData.Instance.Assists.DashMode == Assists.DashModes.Infinite && !level.InCutscene) {
            RefillDash();
        }
        else if (!Inventory.NoRefills) {
            if (StateMachine.State == 3) {
                RefillDash();
            }
            else if (onGround && (CollideCheck<Solid, NegaBlock>(Position + Vector2.UnitY) || CollideCheckOutside<JumpThru>(Position + Vector2.UnitY)) && (!CollideCheck<Spikes>(Position) || SaveData.Instance.Assists.Invincible)) {
                RefillDash();
            }
        }
        if (varJumpTimer > 0f) {
            varJumpTimer -= Engine.DeltaTime;
        }
        if (AutoJumpTimer > 0f) {
            if (AutoJump) {
                AutoJumpTimer -= Engine.DeltaTime;
                if (AutoJumpTimer <= 0f) {
                    AutoJump = false;
                }
            }
            else {
                AutoJumpTimer = 0f;
            }
        }
        if (forceMoveXTimer > 0f) {
            forceMoveXTimer -= Engine.DeltaTime;
            moveX = forceMoveX;
        }
        else {
            moveX = Input.MoveX.Value;
            climbHopSolid = null;
        }
        if (climbHopSolid != null && !climbHopSolid.Collidable) {
            climbHopSolid = null;
        }
        else if (climbHopSolid != null && climbHopSolid.Position != climbHopSolidPosition) {
            Vector2 vector = climbHopSolid.Position - climbHopSolidPosition;
            climbHopSolidPosition = climbHopSolid.Position;
            MoveHExact((int)vector.X);
            MoveVExact((int)vector.Y);
        }
        if (noWindTimer > 0f) {
            noWindTimer -= Engine.DeltaTime;
        }
        if (moveX != 0 && InControl && StateMachine.State != 1 && StateMachine.State != 8 && StateMachine.State != 5 && StateMachine.State != 6) {
            Facings facings = (Facings)moveX;
            Facing = facings;
        }
        lastAim = Input.GetAimVector(Facing);
        if (wallSpeedRetentionTimer > 0f) {
            if (Math.Sign(Speed.X) == -Math.Sign(wallSpeedRetained)) {
                wallSpeedRetentionTimer = 0f;
            }
            else if (!CollideCheck<Solid>(Position + Vector2.UnitX * Math.Sign(wallSpeedRetained))) {
                Speed.X = wallSpeedRetained;
                wallSpeedRetentionTimer = 0f;
            }
            else {
                wallSpeedRetentionTimer -= Engine.DeltaTime;
            }
        }
        if (hopWaitX != 0) {
            if (Math.Sign(Speed.X) == -hopWaitX || Speed.Y > 0f) {
                hopWaitX = 0;
            }
            else if (!CollideCheck<Solid>(Position + Vector2.UnitX * hopWaitX)) {
                lowFrictionStopTimer = 0.15f;
                Speed.X = hopWaitXSpeed;
                hopWaitX = 0;
            }
        }
        if (minHoldTimer > 0f) {
            minHoldTimer -= Engine.DeltaTime;
        }
        if (launched) {
            if (Speed.LengthSquared() < 19600f) {
                launched = false;
            }
            else {
                launchedTimer += Engine.DeltaTime;
                if (launchedTimer >= 0.5f) {
                    launched = false;
                    launchedTimer = 0f;
                }
            }
        }
        else {
            launchedTimer = 0f;
        }
        base.Update();

        if (!onGround && Speed.Y <= 0f && (StateMachine.State != 1 || lastClimbMove == -1) && CollideCheck<JumpThru>() && !JumpThruBoostBlockedCheck()) {
            MoveV(-40f * Engine.DeltaTime);
        }
        if (!onGround && DashAttacking && DashDir.Y == 0f && (CollideCheck<Solid>(Position + Vector2.UnitY * 3f) || CollideCheckOutside<JumpThru>(Position + Vector2.UnitY * 3f)) && !DashCorrectCheck(Vector2.UnitY * 3f)) {
            MoveVExact(3);
        }
        if (Speed.Y > 0f && CanUnDuck && base.Collider != starFlyHitbox && !onGround && jumpGraceTimer <= 0f) {
            Ducking = false;
        }
        if (StateMachine.State != 9 && StateMachine.State != 22) {
            MoveH(Speed.X * Engine.DeltaTime, onCollideH);
        }
        if (StateMachine.State != 9 && StateMachine.State != 22) {
            MoveV(Speed.Y * Engine.DeltaTime, onCollideV);
        }
        if (StateMachine.State == 3) {
            if (Speed.Y < 0f && Speed.Y >= -60f && _IsOverWater()) {
                while (!SwimCheck()) {
                    Speed.Y = 0f;
                    if (MoveVExact(1)) {
                        break;
                    }
                }
            }
        }
        else if (StateMachine.State == 0 && SwimCheck()) {
            StateMachine.State = 3;
        }
        else if (StateMachine.State == 1 && SwimCheck()) {
            Water water = CollideFirst<Water>(Position);
            if (water != null && base.Center.Y < water.Center.Y) {
                while (SwimCheck() && !MoveVExact(-1)) {
                }
                if (SwimCheck()) {
                    StateMachine.State = 3;
                }
            }
            else {
                StateMachine.State = 3;
            }
        }

        UpdateCarry();


        if (!Dead && StateMachine.State != 21) {
            Collider collider = base.Collider;
            base.Collider = hurtbox;
            foreach (PlayerCollider component2 in base.Scene.Tracker.GetComponents<PlayerCollider>()) {
                if (component2.Check(this) && Dead) {
                    base.Collider = collider;
                    return;
                }
            }
            if (base.Collider == hurtbox) {
                base.Collider = collider;
            }
        }


        if (InControl && !Dead && StateMachine.State != 9 && EnforceLevelBounds) {
            level.EnforceBounds(this);
        }
  

        wasOnGround = onGround;
        windMovedUp = false;

        if (UpdateBeforeWind) {
            WindMove(level.Wind * 0.1f * Engine.DeltaTime);
        }

        foreach (KeyValuePair<float, Coroutine> pair in AfterPlayer) {
            if (pair.Value is { } coroutine && coroutine.Active) {
                coroutine.Update();
            };
        }

    }

    public bool _IsOverWater() {
        Rectangle bounds = base.Collider.Bounds;
        bounds.Height += 2;
        return base.Scene.CollideCheck<Water>(bounds);
    }


    public PlayerDeadBody orig_Die(Vector2 direction, bool evenIfInvincible = false, bool registerDeathInStats = true) {
        /*
        Session session = level.Session;
        bool flag = !evenIfInvincible && SaveData.Instance.Assists.Invincible;
        if (!Dead && !flag && StateMachine.State != 18) {
            
            if (registerDeathInStats) {
                session.Deaths++;
                session.DeathsInCurrentLevel++;
                SaveData.Instance.AddDeath(session.Area);
            }
            Dead = true;
            
            base.Depth = -1000000;
            Speed = Vector2.Zero;
            StateMachine.Locked = true;
            Collidable = false;
            Drop();
            if (LastBooster != null) {
                LastBooster.PlayerDied();
            }
            
            PlayerDeadBody playerDeadBody = new PlayerDeadBody(this, direction);
            
            return playerDeadBody;
        }
        */
        return null;
    }

    public void orig_BoostBegin() {
        RefillDash();
        RefillStamina();
        if (Holding != null) {
            Drop();
        }
    }

    public void orig_WindMove(Vector2 move) {
        if (JustRespawned || !(noWindTimer <= 0f) || !InControl || StateMachine.State == 4 || StateMachine.State == 2 || StateMachine.State == 10) {
            return;
        }
        if (move.X != 0f && StateMachine.State != 1) {
            if (!CollideCheck<Solid>(Position + Vector2.UnitX * -Math.Sign(move.X) * 3f)) {
                if (Ducking && onGround) {
                    move.X *= 0f;
                }
                if (move.X < 0f) {
                    move.X = Math.Max(move.X, (float)level.Bounds.Left - (base.ExactPosition.X + base.Collider.Left));
                }
                else {
                    move.X = Math.Min(move.X, (float)level.Bounds.Right - (base.ExactPosition.X + base.Collider.Right));
                }
                MoveH(move.X);
            }
        }
        if (move.Y == 0f) {
            return;
        }
        if (!(base.Bottom > (float)level.Bounds.Top) || (!(Speed.Y < 0f) && OnGround())) {
            return;
        }
        if (StateMachine.State == 1) {
            if (!(move.Y > 0f) || !(climbNoMoveTimer <= 0f)) {
                return;
            }
            move.Y *= 0.4f;
        }
        if (move.Y < 0f) {
            windMovedUp = true;
        }
        MoveV(move.Y);
    }

    public void orig_WallJump(int dir) {
        Ducking = false;
        Input.Jump.ConsumeBuffer();
        jumpGraceTimer = 0f;
        varJumpTimer = 0.2f;
        AutoJump = false;
        dashAttackTimer = 0f;
        gliderBoostTimer = 0f;
        wallSlideTimer = 1.2f;
        wallBoostTimer = 0f;
        lowFrictionStopTimer = 0.15f;
        if (Holding != null && Holding.SlowFall) {
            forceMoveX = dir;
            forceMoveXTimer = 0.26f;
        }
        else if (moveX != 0) {
            forceMoveX = dir;
            forceMoveXTimer = 0.16f;
        }
        if (base.LiftSpeed == Vector2.Zero) {
            Solid solid = CollideFirst<Solid>(Position + Vector2.UnitX * 3f * -dir);
            if (solid != null) {
                base.LiftSpeed = solid.LiftSpeed;
            }
        }
        Speed.X = 130f * (float)dir;
        Speed.Y = -105f;
        Speed += LiftBoost;
        varJumpSpeed = Speed.Y;
        LaunchedBoostCheck();
    }

    public Vector2 ExplodeLaunch(Vector2 from, bool snapUp = true) {
        return ExplodeLaunch(from, snapUp, sidesOnly: false);
    }

    public bool orig_Pickup(Holdable pickup) {
        if (pickup.Pickup(this)) {
            Ducking = false;
            Holding = pickup;
            minHoldTimer = 0.35f;
            return true;
        }
        return false;
    }

    public void BeforeSideTransition() {
        TransitionOrDead = true;
    }

    public void BeforeDownTransition() {
        if (StateMachine.State != 5 && StateMachine.State != 18 && StateMachine.State != 19) {
            StateMachine.State = 0;
            Speed.Y = Math.Max(0f, Speed.Y);
            AutoJump = false;
            varJumpTimer = 0f;
        }
        foreach (Entity entity in base.Scene.Tracker.GetEntities<Platform>()) {
            if (!(entity is SolidTiles) && CollideCheckOutside(entity, Position + Vector2.UnitY * base.Height)) {
                entity.Collidable = false;
            }
        }
        TransitionOrDead = true;
    }

    public void BeforeUpTransition() {
        Speed.X = 0f;
        if (StateMachine.State != 5 && StateMachine.State != 18 && StateMachine.State != 19) {
            varJumpSpeed = (Speed.Y = -105f);
            if (StateMachine.State == 10) {
                StateMachine.State = 13;
            }
            else {
                StateMachine.State = 0;
            }
            AutoJump = true;
            AutoJumpTimer = 0f;
            varJumpTimer = 0.2f;
        }
        dashCooldownTimer = 0.2f;
        TransitionOrDead = true;
    }


    public override void SceneBegin(Scene scene) {
        base.SceneBegin(scene);
    }

    public void orig_SceneEnd(Scene scene) {
        base.SceneEnd(scene);
    }
}

public static class SimulatorExtension {
    public static bool Check(this Holdable holdable, DummyPlayer player) {
        Collider collider = holdable.Entity.Collider;
        if (holdable.PickupCollider != null) {
            holdable.Entity.Collider = holdable.PickupCollider;
        }
        bool result = player.CollideCheck(holdable.Entity);
        holdable.Entity.Collider = collider;
        return result;
    }

    public static bool DashCorrectCheck(this LedgeBlocker blocker, DummyPlayer player) {
        if (blocker.Blocking && player.CollideCheck(blocker.Entity, player.Position)) {
            // todo: some issue with trigger spikes
            return true;
        }
        return false;
    }

public static bool Pickup(this Holdable holdable, DummyPlayer player) {
        return false;
        /*
        if (cannotHoldTimer > 0f || base.Scene == null || base.Entity.Scene == null) {
            return false;
        }
        idleDepth = base.Entity.Depth;
        base.Entity.Depth = player.Depth - 1;
        base.Entity.Visible = true;
        Holder = player;
        if (OnPickup != null) {
            OnPickup();
        }
        return true;
        */
    }

    public static bool HopBlockCheck(this LedgeBlocker blocker, DummyPlayer player) {

        return false;
    }

    public static void EnforceBounds(this Level level, DummyPlayer player) {

        Rectangle bounds = level.Bounds;
        Rectangle rectangle = new Rectangle((int)level.Camera.Left, (int)level.Camera.Top, 320, 180);

        if (level.CameraLockMode == CameraLockModes.FinalBoss && player.Left < (float)rectangle.Left) {
            player.Left = rectangle.Left;
            player.OnBoundsH();
        }
        else if (player.Left < (float)bounds.Left) {
            if (player.Top >= (float)bounds.Top && player.Bottom < (float)bounds.Bottom && level.Session.MapData.CanTransitionTo(level, player.Center + Vector2.UnitX * -8f)) {
                player.BeforeSideTransition();
                
                return;
            }
            player.Left = bounds.Left;
            player.OnBoundsH();
        }
        TheoCrystal entity = level.Tracker.GetEntity<TheoCrystal>();
        if (level.CameraLockMode == CameraLockModes.FinalBoss && player.Right > (float)rectangle.Right && rectangle.Right < bounds.Right - 4) {
            player.Right = rectangle.Right;
            player.OnBoundsH();
        }
        else if (entity != null && (player.Holding == null || !player.Holding.IsHeld) && player.Right > (float)(bounds.Right - 1)) {
            player.Right = bounds.Right - 1;
        }
        else if (player.Right > (float)bounds.Right) {
            if (player.Top >= (float)bounds.Top && player.Bottom < (float)bounds.Bottom && level.Session.MapData.CanTransitionTo(level, player.Center + Vector2.UnitX * 8f)) {
                player.BeforeSideTransition();
                return;
            }
            player.Right = bounds.Right;
            player.OnBoundsH();
        }
        if (level.CameraLockMode != 0 && player.Top < (float)rectangle.Top) {
            player.Top = rectangle.Top;
            player.OnBoundsV();
        }
        else if (player.CenterY < (float)bounds.Top) {
            if (level.Session.MapData.CanTransitionTo(level, player.Center - Vector2.UnitY * 12f)) {
                player.BeforeUpTransition();
                
                return;
            }
            if (player.Top < (float)(bounds.Top - 24)) {
                player.Top = bounds.Top - 24;
                player.OnBoundsV();
            }
        }
        if (level.CameraLockMode != 0 && rectangle.Bottom < bounds.Bottom - 4 && player.Top > (float)rectangle.Bottom) {
            if (SaveData.Instance.Assists.Invincible) {
                player.Bounce(rectangle.Bottom);
            }
            else {
                player.Die(Vector2.Zero);
            }
        }
        else if (player.Bottom > (float)bounds.Bottom && level.Session.MapData.CanTransitionTo(level, player.Center + Vector2.UnitY * 12f) && !level.Session.LevelData.DisableDownTransition) {
            if (!player.CollideCheck<Solid>(player.Position + Vector2.UnitY * 4f)) {
                player.BeforeDownTransition();
            }
        }
        else if (player.Top > (float)bounds.Bottom && SaveData.Instance.Assists.Invincible) {
            player.Bounce(bounds.Bottom);
        }
        else if (player.Top > (float)(bounds.Bottom + 4)) {
            player.Die(Vector2.Zero);
        }
    }
}