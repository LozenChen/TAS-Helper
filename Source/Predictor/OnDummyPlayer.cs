using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using System.Collections;
using Monocle;
using static Celeste.Spring;

namespace Celeste.Mod.TASHelper.Predictor;

public static class OnDummyPlayer {
    public static bool Check(this PlayerCollider pc, DummyPlayer player) {
        Collider collider = pc.Collider;
        if (pc.FeatherCollider != null && player.StateMachine.State == 19) {
            collider = pc.FeatherCollider;
        }
        if (collider == null) {
            if (player.CollideCheck(pc.Entity)) {
                OnCollide(pc, player);
                return true;
            }
            return false;
        }
        Collider collider2 = pc.Entity.Collider;
        pc.Entity.Collider = collider;
        bool num = player.CollideCheck(pc.Entity);
        pc.Entity.Collider = collider2;
        if (num) {
            OnCollide(pc, player);
            return true;
        }
        return false;
    }

    public static void OnCollide(PlayerCollider pc, DummyPlayer player) {
        switch (pc.Entity) {
            case Spring:
                OnPlayer(pc.Entity as Spring, player);
                return;
            case Refill:
                OnPlayer(pc.Entity as Refill, player);
                return;
            default:
                return;
        }
    }

    private static void OnPlayer(Spring spring, DummyPlayer player) {
        if (player.StateMachine.State == 9 || !spring.GetFieldValue<bool>("playerCanUse")) {
            return;
        }
        if (spring.Orientation == Orientations.Floor) {
            if (player.Speed.Y >= 0f) {
                player.SuperBounce(spring.Top);
            }
            return;
        }
        if (spring.Orientation == Orientations.WallLeft) {
            if (player.SideBounce(1, spring.Right, spring.CenterY)) {
            }
            return;
        }
        if (spring.Orientation == Orientations.WallRight) {
            if (player.SideBounce(-1, spring.Left, spring.CenterY)) {
            }
            return;
        }
    }

    private static void OnPlayer(Refill refill, DummyPlayer player) {
        if (player.UseRefill(refill.GetFieldValue<bool>("twoDashes"))) {
            player.AddCoroutine(new Coroutine(Routine()),-100f);
        }
        
        static IEnumerator Routine() {
            InputManager.Freeze(0.05f);
            yield return null;
        }
    }
}
