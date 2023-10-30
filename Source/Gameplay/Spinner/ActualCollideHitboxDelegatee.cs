using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.TASHelper.Utils;
using TAS.EverestInterop.Hitboxes;
using TAS;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;

internal static class ActualCollideHitboxDelegatee {

    [Initialize]
    private static void Initiailize() {
        typeof(ActualEntityCollideHitbox).GetMethod("SaveActualCollidable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).HookAfter<Entity>(
            e => {
                if (TasHelperSettings.Enabled && SpinnerCalculateHelper.HazardType(e) != null) {
                    LastCollidables[e] = SpinnerCalculateHelper.GetCollidable(e);
                }
            }
        );

        typeof(ActualEntityCollideHitbox).GetMethod("Clear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).HookAfter(() => {
            LastCollidables.Clear();
        });

        typeof(ActualEntityCollideHitbox).GetMethod("LoadActualCollidePosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).IlHook(il => {
            ILCursor cursor = new(il);
            Instruction start = cursor.Next;
            cursor.EmitDelegate(IfGotoLoadNull);
            cursor.Emit(OpCodes.Brfalse, start);
            cursor.Emit(OpCodes.Ldnull).Emit(OpCodes.Ret);
        });
    }

    public static bool IfGotoLoadNull() {
        return protectOrig;
    }

    internal static bool protectOrig = false;

    private static readonly Dictionary<Entity, bool> LastCollidables = new();

    public static void DrawLastFrameHitbox(bool skipCondition, Entity entity, Camera camera, Color color, bool collidable, Action<Entity, Camera, Color, bool> invokeOrig) {
        DrawLastFrameHitbox(skipCondition, entity, camera, color, collidable, (a, b, c, d, isNow) => invokeOrig(a, b, c, d));
    }
    public static void DrawLastFrameHitbox(bool skipCondition, Entity entity, Camera camera, Color color, bool collidable, Action<Entity, Camera, Color, bool, bool> invokeOrig) {
        // currently we don't need an actualCamera...?

        if (Manager.UltraFastForwarding
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

        protectOrig = true;

        Color lastFrameColor =
            TasSettings.ShowActualCollideHitboxes == ActualCollideHitboxType.Append && entity.Position != actualCollidePosition
        ? color.Invert()
                : color;

        if (TasSettings.ShowActualCollideHitboxes == ActualCollideHitboxType.Append) {
            if (entity.Position == actualCollidePosition) {
                invokeOrig(entity, camera, lastFrameColor, actualCollidable, false);

                protectOrig = false;
                return;
            }

            invokeOrig(entity, camera, color, collidable, true);
        }

        Vector2 currentPosition = entity.Position;

        // we assert: invokeOrig only draws player collider, so there's no extra check here
        entity.Position = actualCollidePosition;
        invokeOrig(entity, camera, lastFrameColor, actualCollidable, false);
        entity.Position = currentPosition;

        protectOrig = false;
    }

    public static bool? LoadActualCollidable_TH(this Entity entity) {
        return LastCollidables.TryGetValue(entity, out bool result) ? result : null;
    }
}

