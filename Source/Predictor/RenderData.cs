using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;

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

    public bool Dead;
    public bool Ducking;
    
    public bool OnGround; // ignore Speed.Y check

    public bool LevelNotInControl; // actually next frame will be indeed InControl
    public bool PlayerNotInControl;
    public bool NotInControl;

    public bool OnEntityState;
    
    public int Dashes;

    public bool GliderBoost;
    public bool MinHoldTime;
    public bool OnBounce;
    public bool OnUltra;

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
        state.LevelNotInControl = level.Transitioning || level.Paused || level.Frozen || level.SkippingCutscene ;
        state.PlayerNotInControl = state.StateMachineState > 10 && state.StateMachineState != 19 && state.StateMachineState != 24;
        state.NotInControl = state.LevelNotInControl || state.PlayerNotInControl;
        state.OnEntityState = (state.StateMachineState == 4) || (state.StateMachineState == 7) || (state.StateMachineState == 19) || (state.StateMachineState == 24);
        state.Dashes = player.Dashes;
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

    public RenderData(int index, PlayerState PreviousState, PlayerState CurrentState) {
        this.index = index;
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
        if (!CurrentState.NotInControl && PreviousState.NotInControl) {
            Keyframe |= KeyframeType.GainControl;
        }
        if (CurrentState.NotInControl && !PreviousState.NotInControl) {
            Keyframe |= KeyframeType.LoseControl;
        }
        if (CurrentState.OnEntityState && !PreviousState.OnEntityState) {
            Keyframe |= KeyframeType.OnEntityState;
        }
        if (CurrentState.Dashes > PreviousState.Dashes) {
            Keyframe |= KeyframeType.GainDash; // not correct if you dash on a refill crystal, but i guess it doesn't matter
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
    GainControl = 1 << 6,
    LoseControl = 1 << 7,
    OnEntityState = 1 << 8,
    GainDash = 1 << 9,
    GainUltra = 1 << 10,
    OnBounce = 1 << 11,
}