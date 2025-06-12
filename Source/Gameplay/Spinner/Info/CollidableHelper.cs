using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner.Info;

internal static class CollidableHelper {

    private static Dictionary<Type, Func<Entity, bool>> LightningCollidable = new();

    internal static void Add(Type modLightning, Func<Entity, bool> lightningCollidableGetter) {
        LightningCollidable[modLightning] = lightningCollidableGetter;
    }
    public static bool GetCollidable(Entity self) {
        if (LightningCollidable.TryGetValue(self.GetType(), out Func<Entity, bool> func)) {
            return func(self);
        }
        if (self is Lightning lightning) {
            // FrostHelper.AttachedLightning inherits from Lightning, so no need to check before
            return lightning.Collidable && !lightning.disappearing;
        }

        // for future modification: ASSUMPTION: ActualCollideHitboxDelegatee.Initialize assumes that only lightning has special checks
        return self.Collidable;
    }
}