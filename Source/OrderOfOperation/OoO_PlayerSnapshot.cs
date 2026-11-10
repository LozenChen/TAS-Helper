using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.OrderOfOperation;

// Snapshot of the player fields that the "Auto" for-each modes compare.
internal readonly struct OoO_PlayerSnapshot {
    private readonly Vector2 position;
    private readonly Vector2 speed;
    private readonly Vector2 movementCounter;
    private readonly Vector2 liftSpeed;
    private readonly float liftSpeedTimer;
    private readonly bool dead;
    private readonly bool ducking;
    private readonly int state;
    private readonly int dashes;

    private OoO_PlayerSnapshot(Player player) {
        position = player.Position;
        speed = player.Speed;
        movementCounter = player.movementCounter;
        liftSpeed = player.LiftBoost;
        liftSpeedTimer = player.liftSpeedTimer;
        dead = player.Dead;
        ducking = player.Ducking;
        state = player.StateMachine.State;
        dashes = player.Dashes;
    }

    public static OoO_PlayerSnapshot Capture() {
        Player player = Engine.Scene?.Tracker.GetEntity<Player>();
        return player is null ? default : new OoO_PlayerSnapshot(player);
    }

    public bool Equals(OoO_PlayerSnapshot other) {
        return position == other.position
            && speed == other.speed
            && movementCounter == other.movementCounter
            && liftSpeed == other.liftSpeed
            && liftSpeedTimer == other.liftSpeedTimer
            && dead == other.dead
            && ducking == other.ducking
            && state == other.state
            && dashes == other.dashes;
    }
}
