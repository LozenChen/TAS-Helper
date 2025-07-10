using Celeste.Mod.TASHelper.Utils;
using Monocle;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner.Info;


internal static class SpecialInfoHelper {

    private static List<Type> NoPeriodicInViewCheckTypes = new();

    private static Dictionary<Type, Func<Entity, bool>> NoCycleTypes = new();

    internal static Type VivSpinnerType;

    private static FieldInfo VivHitboxStringGetter;

    internal static Type ChroniaSpinnerType;

    internal static Type CassetteSpinnerType;

    internal static void AddNoPeriodicInViewCheck(Type noPeriodicInViewCheckType) {
        NoPeriodicInViewCheckTypes.Add(noPeriodicInViewCheckType);
    }

    internal static void AddNoCycle(Type noCycleType, Func<Entity, bool> noCycleGetter) {
        NoCycleTypes[noCycleType] = noCycleGetter;
    }
    public static bool NoCycle(Entity self) {
        if (NoCycleTypes.TryGetValue(self.GetType(), out Func<Entity, bool> func)) {
            return func(self);
        }
        return false;
    }

    public static bool NoPeriodicCheckInViewBehavior(Entity self) {
        if (self.IsDust()) {
            return true;
        }
        if (NoPeriodicInViewCheckTypes.Contains(self.GetType())) {
            return true;
        }
        return false;
    }

    [Initialize]
    private static void Initialize() {
        VivSpinnerType = ModUtils.GetType("VivHelper", "VivHelper.Entities.CustomSpinner");
        VivHitboxStringGetter = VivSpinnerType?.GetField("hitboxString", BindingFlags.NonPublic | BindingFlags.Instance);
        ChroniaSpinnerType = ModUtils.GetType("ChroniaHelper", "ChroniaHelper.Entities.SeamlessSpinner");
        CassetteSpinnerType = ModUtils.GetType("BrokemiaHelper", "BrokemiaHelper.CassetteSpinner");
    }
    public static bool IsVivSpinner(Entity self) {
        return self.GetType().IsSameOrSubclassOf(VivSpinnerType);
    }

    public static bool IsChroniaSpinner(Entity self) {
        return self.GetType().IsSameOrSubclassOf(ChroniaSpinnerType);
    }
    public static bool IsCassetteSpinner(Entity self) {
        return self.GetType().IsSameOrSubclassOf(CassetteSpinnerType);
    }

    public static string[] GetVivHitboxString(Entity spinner) {
        return VivHitboxStringGetter.GetValue(spinner) as string[];
    }

    public static string[] GetChroniaHitboxString(Entity spinner) {
        if (spinner.SourceData is { } data && data.Attr("hitboxType") is { } hitboxType && hitboxType.IsNotNullOrEmpty()) {
            return hitboxType switch {
                "loosened" => ["C:6;0,0"],
                "seamlessRound" => ["C:8;0,0"],
                "seamlessSquare" => ["R:16,16;-8,-8"],
                "custom" => ["UNEXPECTED"],
                _ => ["C:6;0,0", "R:16,4;-8,-3"],
            };
        }

        if (spinner.Collider is ColliderList list) {
            switch (list.colliders.Length) {
                case 1: {
                        Collider c = list.colliders.First();
                        if (IsCircle(c, out float radius)) {
                            if (radius == 6f) {
                                return ["C:6;0,0"];
                            }
                            else if (radius == 8f) {
                                return ["C:8;0,0"];
                            }
                        }
                        else if (c is Hitbox hb && hb.width == 16f && hb.height == 16f && hb.Position.X == -8f && hb.Position.Y == -8f) {
                            return ["R:16,16;-8,-8"];
                        }
                        break;
                    }
                case 2: {
                        if (IsCircle(list.colliders.First(), out float radius) && radius == 6f && list.colliders[1] is Hitbox hb && hb.width == 16f && hb.height == 4f && hb.Position.X == -8f && hb.Position.Y == -3f) {
                            return ["C:6;0,0", "R:16,4;-8,-3"];
                        }
                        break;
                    }
                default: {
                        break;
                    }
            }
        }
        return ["UNEXPECTED"];

        static bool IsCircle(Collider collider, out float radius) {
            if (collider is Circle circle) {
                radius = circle.Radius;
                return true;
            }
            radius = 0f;
            return false;
        }
    }
}