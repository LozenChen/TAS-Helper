using Celeste.Mod.TASHelper.Gameplay;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Predictor;


// todo: modify EarlyStopCheck (and maybe StartPredictCheck), e.g. support showing when we will regain control
// and add options to EarlyStopCheck

public class PlayerState {
    public bool LevelHasPlayer;
    public float x;
    public float y;
    public float width;
    public float height;

    public int StateMachineState;

    public bool Dead = true; // if Player does not exist, it's also considered dead
    public bool Ducking;

    public bool OnGround; // ignore Speed.Y check

    public bool LevelNotInControl = true; // actually next frame will be indeed InControl
    public bool PlayerNotInControl = true; // given it default values, so if player does not exist, it still works
    public bool NotInControl = true;

    public bool OnEntityState;
    public int Dashes;
    public bool CanDash;

    public bool GliderBoost;
    public bool MinHoldTime;
    public bool OnBounce;
    public bool OnUltra;
    public bool OnRefillDash;
    public float SpeedXBeforeUltra;
    public Vector2 RespawnPoint;
    public bool EngineFreeze;
    public bool Transitioning;
    public float WallSpeedRetained;

    public PlayerState() {

    }

    public static PlayerState GetState() {
        PlayerState state = new();
        if (Engine.Scene is not Level level || player is null) {
            state.LevelHasPlayer = false;
            return state;
        }
        state.LevelHasPlayer = true;
        state.x = player.Collider.Left + player.X;
        state.y = player.Collider.Top + player.Y;
        state.width = player.Collider.Width;
        state.height = player.Collider.Height;
        state.StateMachineState = player.StateMachine.State;
        state.Dead = player.Dead;
        state.Ducking = player.Ducking;
        state.OnGround = player.OnGround() && player.Speed.Y >= 0f;
        state.LevelNotInControl = level.Transitioning || level.Paused || level.Frozen || level.SkippingCutscene;
        state.PlayerNotInControl = state.StateMachineState > 10 && state.StateMachineState != 19 && state.StateMachineState != 24;
        state.NotInControl = state.LevelNotInControl || state.PlayerNotInControl;
        state.OnEntityState = (state.StateMachineState == 4) || (state.StateMachineState == 7) || (state.StateMachineState == 19) || (state.StateMachineState == 24);
        state.Dashes = player.Dashes;
        state.CanDash = player.dashCooldownTimer <= 0f && player.Dashes > 0;
        state.RespawnPoint = level.Session.RespawnPoint ?? Vector2.Zero;
        state.EngineFreeze = Core.ThisPredictedFrameFreezed;
        state.OnBounce = PlayerStateUtils.AnyBounce && !state.EngineFreeze;
        state.OnUltra = PlayerStateUtils.Ultra && !state.EngineFreeze && Math.Abs(PlayerStateUtils.SpeedBeforeUltra.X) >= TasHelperSettings.UltraSpeedLowerLimit;
        state.OnRefillDash = PlayerStateUtils.RefillDash && !state.EngineFreeze;
        state.SpeedXBeforeUltra = state.OnUltra ? PlayerStateUtils.SpeedBeforeUltra.X : 0f;
        state.Transitioning = level.Transitioning;
        state.WallSpeedRetained = player.wallSpeedRetentionTimer > 0f ? player.wallSpeedRetained : 0f;
        return state;
    }
}

public struct RenderData {
    public int index;
    public bool visible;
    public float x;
    public float y;
    public float width;
    public float height;
    public KeyframeType Keyframe;
    public bool addTime;

    public RenderData(int index, PlayerState PreviousState, PlayerState CurrentState) {
        this.index = index;
        addTime = false;
        Keyframe = new();
        if (CurrentState.LevelHasPlayer) {
            x = CurrentState.x;
            y = CurrentState.y;
            width = CurrentState.width;
            height = CurrentState.height;
            visible = true;
            Keyframe |= KeyframeType.NotNull;
        }
        else {
            x = 0f; y = 0f; width = 0f; height = 0f; visible = false;
            return;
        }
        if (CurrentState.Dead && !PreviousState.Dead) {
            Keyframe |= KeyframeType.GainDead;
        }
        if (CurrentState.Ducking && !PreviousState.Ducking) {
            Keyframe |= KeyframeType.GainDuck;
        }
        if (!CurrentState.Ducking && PreviousState.Ducking) {
            Keyframe |= KeyframeType.LoseDuck;
        }
        if (CurrentState.OnGround && !PreviousState.OnGround) {
            Keyframe |= KeyframeType.GainOnGround;
        }
        if (!CurrentState.OnGround && PreviousState.OnGround) {
            Keyframe |= KeyframeType.LoseOnGround;
        }
        if (!CurrentState.PlayerNotInControl && PreviousState.PlayerNotInControl) {
            Keyframe |= KeyframeType.GainPlayerControl;
        }
        if (CurrentState.PlayerNotInControl && !PreviousState.PlayerNotInControl) {
            Keyframe |= KeyframeType.LosePlayerControl;
        }
        if (CurrentState.OnEntityState && !PreviousState.OnEntityState) {
            Keyframe |= KeyframeType.OnEntityState;
        }
        if (CurrentState.OnRefillDash) {
            Keyframe |= KeyframeType.RefillDash; // not correct if you dash on a refill crystal, but i guess it doesn't matter
        }
        if (CurrentState.OnUltra) {
            Keyframe |= KeyframeType.GainUltra;
        }
        if (CurrentState.OnBounce) {
            Keyframe |= KeyframeType.OnBounce;
        }
        if (CurrentState.StateMachineState == 7 && CurrentState.CanDash && PreviousState.StateMachineState == 7 && !PreviousState.CanDash) {
            Keyframe |= KeyframeType.CanDashInStLaunch;
        }
        if (!CurrentState.LevelNotInControl && PreviousState.LevelNotInControl) {
            Keyframe |= KeyframeType.GainLevelControl;
        }
        if (CurrentState.LevelNotInControl && !PreviousState.LevelNotInControl) {
            Keyframe |= KeyframeType.LoseLevelControl;
        }
        if (CurrentState.RespawnPoint != PreviousState.RespawnPoint) {
            Keyframe |= KeyframeType.RespawnPointChange;
        }
        if (CurrentState.EngineFreeze && !PreviousState.EngineFreeze) {
            Keyframe |= KeyframeType.BeginEngineFreeze;
        }
        if (!CurrentState.EngineFreeze && PreviousState.EngineFreeze) {
            Keyframe |= KeyframeType.EndEngineFreeze;
        }
        if (CurrentState.Transitioning && !PreviousState.Transitioning) {
            Keyframe |= KeyframeType.BeginTransition;
        }
        if (CurrentState.WallSpeedRetained != 0f && CurrentState.WallSpeedRetained != PreviousState.WallSpeedRetained) {
            Keyframe |= KeyframeType.GetRetained;
        }
    }

}

[Flags]
public enum KeyframeType {
    None = 0 << 0,
    NotNull = 1 << 0,
    GainDead = 1 << 1,
    GainDuck = 1 << 2,
    LoseDuck = 1 << 3,
    GainOnGround = 1 << 4,
    LoseOnGround = 1 << 5,
    GainPlayerControl = 1 << 6,
    LosePlayerControl = 1 << 7,
    OnEntityState = 1 << 8,
    RefillDash = 1 << 9,
    GainUltra = 1 << 10,
    OnBounce = 1 << 11,
    CanDashInStLaunch = 1 << 12,
    GainLevelControl = 1 << 13,
    LoseLevelControl = 1 << 14,
    RespawnPointChange = 1 << 15,
    BeginEngineFreeze = 1 << 16,
    EndEngineFreeze = 1 << 17,
    BeginTransition = 1 << 18,
    GetRetained = 1 << 19,
    GainControl = GainPlayerControl | GainLevelControl,
    LoseControl = LosePlayerControl | LoseLevelControl,
}

// todo: even more keyframe type, e.g. keydoor open