using Microsoft.Xna.Framework;
using Monocle;
using StudioCommunication;
using TAS.Gameplay.Hitboxes;

namespace Celeste.Mod.TASHelper.Gameplay;

internal static class ActualCollideHitboxDelegatee {

    public static bool Disabled => ActualCollideHitbox.Disabled;
    public static void DrawLastFrameHitbox(bool skipCondition, Entity entity, Camera camera, Color color, bool collidable, Action<Entity, Camera, Color, bool, bool> invokeOrig) {
        // currently we don't need an actualCamera...?

        if (skipCondition
            || Disabled
            || TasSettings.ShowActualCollideHitboxes == ActualCollideHitboxType.Off
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

        bool actualCollidable = entity.LoadActualCollidable() ?? false;

        if (TasSettings.ShowActualCollideHitboxes == ActualCollideHitboxType.Append) {
            if (entity.Position == actualCollidePosition) {
                invokeOrig(entity, camera, lastFrameColor, actualCollidable, false);
                return;
            }

            invokeOrig(entity, camera, color, collidable, true);
        }

        Vector2 currentPosition = entity.Position;

        // we assert: invokeOrig only draws player collider, so there's no extra check here
        entity.Position = actualCollidePosition;
        invokeOrig(entity, camera, lastFrameColor, actualCollidable, false);
        entity.Position = currentPosition;
    }
}