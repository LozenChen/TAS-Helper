using Celeste.Mod.TASHelper.Gameplay.Spinner;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using StudioCommunication;
using TAS;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Gameplay;

internal static class ActualCollideHitboxDelegatee {

    [Initialize]
    private static void Initiailize() {

        typeof(ActualEntityCollideHitbox).GetMethod("SaveActualCollidable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).HookAfter<Entity>(
            e => {
                if (TasHelperSettings.Enabled && e.IsLightning()) {
                    LastCollidables[e] = SpinnerCalculateHelper.GetCollidable(e);
                }
            }
        );

        typeof(ActualEntityCollideHitbox).GetMethod("Clear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).HookAfter(() => {
            LastCollidables.Clear();
        });
    }

    private static readonly Dictionary<Entity, bool> LastCollidables = new();

    public static void DrawLastFrameHitbox(bool skipCondition, Entity entity, Camera camera, Color color, bool collidable, Action<Entity, Camera, Color, bool, bool> invokeOrig) {
        // currently we don't need an actualCamera...?

        if (Manager.FastForwarding
            || !TasSettings.ShowHitboxes
            || skipCondition
            || TasSettings.ShowActualCollideHitboxes == ActualCollideHitboxType.Off
            // || entity.Get<PlayerCollider>() == null
            || entity.Scene?.Tracker.GetEntity<Player>() == null
            || entity.LoadActualCollidePosition() is not { } actualCollidePosition
            || entity.LoadActualCollidable_TH() is not { } actualCollidable
            || TasSettings.ShowActualCollideHitboxes == ActualCollideHitboxType.Append && entity.Position == actualCollidePosition &&
            collidable == actualCollidable
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

    public static bool? LoadActualCollidable_TH(this Entity entity) {
        return LastCollidables.TryGetValue(entity, out bool result) ? result : null;
    }
}