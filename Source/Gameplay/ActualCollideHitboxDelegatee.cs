using Celeste.Mod.TASHelper.Gameplay.Spinner;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System.Runtime.CompilerServices;
using TAS;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Gameplay;

internal static class ActualCollideHitboxDelegatee {

    [Initialize]
    private static void Initiailize() {

        typeof(ActualEntityCollideHitbox).GetMethod("SaveActualCollidable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).HookAfter<Entity>(
            e => {
                if (TasHelperSettings.Enabled && e.isHazard()) {
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
            cursor.EmitDelegate(LoadNull);
            // in Everest stable 4335, cursor.Emit(OpCodes.Ldnull) will make Everest don't open
            // while in stable 4351, it's ok
            // i guess Ldnull is also not technically correct
            cursor.Emit(OpCodes.Ret);
        });
    }

    public static bool IfGotoLoadNull() {
        return protectOrig || protectOrig_2;
    }

    private static Vector2? LoadNull() {
        return null;
    }

    public static void StopActualCollideHitbox() {
        protectOrig_2 = true;
    }

    public static void RecoverActualCollideHitbox() {
        protectOrig_2 = false;
    }

    internal static bool protectOrig = false;

    private static bool protectOrig_2 = false;

    private static readonly Dictionary<Entity, bool> LastCollidables = new();

    public static void DrawLastFrameHitbox(bool skipCondition, Entity entity, Camera camera, Color color, bool collidable, Action<Entity, Camera, Color, bool> invokeOrig) {
        DrawLastFrameHitbox(skipCondition, entity, camera, color, collidable, (a, b, c, d, isNow) => invokeOrig(a, b, c, d));
    }

    public static void DrawLastFrameHitbox(bool skipCondition, Entity entity, Camera camera, Color color, bool collidable, Action<Entity, Camera, Color, bool, bool> invokeOrig) {
        DrawLastFrameHitboxImpl(skipCondition, entity, camera, color, collidable, (a, b, c, d, e) => {
            protectOrig = true; // orig may contain Hitbox.DebugRender, which is already handled. if we make actual collide hitbox handle it again, there will be some issue
            invokeOrig(a, b, c, d, e);
            protectOrig = false;
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawLastFrameHitboxImpl(bool skipCondition, Entity entity, Camera camera, Color color, bool collidable, Action<Entity, Camera, Color, bool, bool> invokeOrig) {
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

