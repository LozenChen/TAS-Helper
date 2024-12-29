using Microsoft.Xna.Framework;
using Monocle;
using StudioCommunication;
using TAS;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Gameplay;

internal static class ActualCollideHitboxDelegatee {

    // WARN: invokeOrig must contain no Hitbox.Render / Circle.Render / ColliderList.Render
    public static void DrawLastFrameHitbox(bool skipCondition, Entity entity, Camera camera, Color color, bool collidable, Action<Entity, Camera, Color, bool, bool> invokeOrig) {
        // currently we don't need an actualCamera...?

        if (Manager.FastForwarding
            || !TasSettings.ShowHitboxes
            || skipCondition
            || TasSettings.ShowActualCollideHitboxes == ActualCollideHitboxType.Off
            // || entity.Get<PlayerCollider>() == null
            || entity.Scene?.Tracker.GetEntity<Player>() == null
            || entity.LoadActualCollidePosition() is not { } actualCollidePosition
            || TasSettings.ShowActualCollideHitboxes == ActualCollideHitboxType.Append && entity.Position == actualCollidePosition &&
            collidable == entity.LoadActualCollidable()
           ) {
            invokeOrig(entity, camera, color, collidable, true);
            return;
        }

        Color lastFrameColor =
            TasSettings.ShowActualCollideHitboxes == ActualCollideHitboxType.Append && entity.Position != actualCollidePosition
        ? color.Invert()
                : color;

        if (TasSettings.ShowActualCollideHitboxes == ActualCollideHitboxType.Append) {
            if (entity.Position == actualCollidePosition) {
                invokeOrig(entity, camera, lastFrameColor, entity.LoadActualCollidable(), false);
                return;
            }

            invokeOrig(entity, camera, color, collidable, true);
        }

        Vector2 currentPosition = entity.Position;

        // we assert: invokeOrig only draws player collider, so there's no extra check here
        entity.Position = actualCollidePosition;
        invokeOrig(entity, camera, lastFrameColor, entity.LoadActualCollidable(), false);
        entity.Position = currentPosition;
    }
}