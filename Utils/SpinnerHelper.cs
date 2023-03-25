using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using VivEntites = VivHelper.Entities;
// VivHelper namespace has a VivHelper class.... so if we want to visit VivHelper.Entities, we should use VivEntities

namespace Celeste.Mod.TASHelper.Utils;

public static class SpinnerHelper {

    public static float TimeActive = 0f;
    public static void Load() {
        On.Monocle.Scene.BeforeUpdate += PatchBeforeUpdate;
    }
    public static void Unload() {
        On.Monocle.Scene.BeforeUpdate -= PatchBeforeUpdate;
    }
    private static void PatchBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
        orig(self);
        TimeActive = self.TimeActive;
    }

    private static void DictionaryAdderNormal(Type type, string offsetName, int HazardType) {
        HazardTypesTreatNormal.Add(type, HazardType);
        OffsetGetters.Add(type, type.CreateGetDelegate<object, float>(offsetName));
    }
    private static void DictionaryAdderSpecial(Type type, string offsetName, GetDelegate<object, int?> HazardTypeGetter) {
        HazardTypesTreatSpecial.Add(type, HazardTypeGetter);
        OffsetGetters.Add(type, type.CreateGetDelegate<object, float>(offsetName));
    }

    public static void Initialize() {
        Assembly Vanilla = ModUtils.VanillaAssembly;
        DictionaryAdderNormal(Vanilla.GetType("Celeste.CrystalStaticSpinner"), "offset", spinner);
        DictionaryAdderNormal(Vanilla.GetType("Celeste.Lightning"), "toggleOffset", lightning);
        DictionaryAdderNormal(Vanilla.GetType("Celeste.DustStaticSpinner"), "offset", dust);

        if (ModUtils.GetType("FrostHelper", "FrostHelper.CustomSpinner") is { } frostSpinnerType) {
            DictionaryAdderSpecial(frostSpinnerType, "offset", FrostSpinnerHazardType);
        }

        if (ModUtils.GetType("VivHelper", "VivHelper.Entities.CustomSpinner") is { } vivSpinnerType) {
            DictionaryAdderSpecial(vivSpinnerType, "offset", VivSpinnerHazardType);
        }

        if (ModUtils.FrostHelperInstalled) {
            VivHitboxStringGetter = typeof(VivEntites.CustomSpinner).GetField("hitboxString" , BindingFlags.NonPublic | BindingFlags.Instance);
            typeof(SpinnerHelper).GetMethod("NoCycle").IlHook((cursor, _) => {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(FrostPatch);
            });
        }

    }

    private static Dictionary<Type, int> HazardTypesTreatNormal = new();

    private static Dictionary<Type, GetDelegate<object, int?>> HazardTypesTreatSpecial = new();

    private static Dictionary<Type, GetDelegate<object, float>> OffsetGetters = new();

    public static FieldInfo VivHitboxStringGetter;

    private static bool ModNoCycle = false;
    public static bool NoCycle(Entity self) {
        bool ModNoCycleCopy = ModNoCycle;
        ModNoCycle = false;
        return ModNoCycleCopy;
    }

    private static void FrostPatch(Entity self) {
        if (ModNoCycle) {
            return;
        }
        if (self is FrostHelper.CustomSpinner spinner) {
            if (typeof(FrostHelper.CustomSpinner).GetFieldInfo("controller").GetValue(spinner) is FrostHelper.CustomSpinnerController controller) {
                ModNoCycle = controller.NoCycles;
                return;
            }
        }
    }

    internal const int spinner = 0;
    internal const int dust = 1;
    internal const int lightning = 2;

    public static int? HazardType(Entity self) {
        Type type = self.GetType();
        if (HazardTypesTreatNormal.TryGetValue(type, out int value)) {
            return value;
        }
        else if (HazardTypesTreatSpecial.TryGetValue(type, out GetDelegate<object, int?> getter)) {
            return getter(self);
        }
        return null;
    }

    private static int? FrostSpinnerHazardType(Object self) {
        if (self is FrostHelper.CustomSpinner customSpinner) {
            return customSpinner.HasCollider ? spinner : null;
        }
        return null;
    }

    private static int? VivSpinnerHazardType(Object self) {
        if (self is VivEntites.CustomSpinner customSpinner) {
            return customSpinner.Collider is null ? null : spinner;
        }
        return null;
    }

    public static float? GetOffset(Entity self) {
        if (OffsetGetters.TryGetValue(self.GetType(), out GetDelegate<object, float> getter)) {
            return getter(self);
        }
        return null;
    }
    public static bool isSpinnner(Entity self) {
        return HazardType(self) == spinner;
    }
    public static bool isLightning(Entity self) {
        return HazardType(self) == lightning;
    }
    public static bool isDust(Entity self) {
        return HazardType(self) == dust;
    }

    public static bool InView(Entity self, Vector2 CameraPos) {
        float zoom = PlayerHelper.CameraZoom;
        if (isLightning(self)) {
            return self.X + self.Width > CameraPos.X - 16f && self.Y + self.Height > CameraPos.Y - 16f && self.X < CameraPos.X + 320f * zoom + 16f && self.Y < CameraPos.Y + 180f * zoom + 16f;
        }
        else {
            return self.X > CameraPos.X - 16f && self.Y > CameraPos.Y - 16f && self.X < CameraPos.X + 320f * zoom + 16f && self.Y < CameraPos.Y + 180f * zoom + 16f;
        }
    }
    public static bool InView(Vector2 pos, float Width, float Height, Vector2 CameraPos, bool isLightning) {
        float zoom = PlayerHelper.CameraZoom;
        if (isLightning) {
            return pos.X + Width > CameraPos.X - 16f && pos.Y + Height > CameraPos.Y - 16f && pos.X < CameraPos.X + 320f * zoom + 16f && pos.Y < CameraPos.Y + 180f * zoom + 16f;
        }
        else {
            return pos.X > CameraPos.X - 16f && pos.Y > CameraPos.Y - 16f && pos.X < CameraPos.X + 320f * zoom + 16f && pos.Y < CameraPos.Y + 180f * zoom + 16f;
        }
    }

    public static bool FarFromRange(Entity self, Vector2 PlayerPosition, Vector2 CameraPos, float scale) {
        if (isLightning(self)) {
            if (self.X + self.Width < CameraPos.X - 320f * scale - 16f || self.Y + self.Height < CameraPos.Y - 180f * scale - 16f || self.X > CameraPos.X + 320f * scale + 320f + 16f || self.Y > CameraPos.Y + 180f * scale + 180f + 16f) {
                return true;
            }
        }
        else {
            if (self.X < CameraPos.X - 320f * scale - 16f || self.Y < CameraPos.Y - 180f * scale - 16f || self.X > CameraPos.X + 320f * scale + 320f + 16f || self.Y > CameraPos.Y + 180f * scale + 180f + 16f) {
                return (Math.Abs(self.X - PlayerPosition.X) > 128f + 128f * 2f * scale || Math.Abs(self.Y - PlayerPosition.Y) > 128f + 128f * 2f * scale);
            }
        }
        return false;
    }

    public static int PredictCountdown(float TimeActive, float offset, bool isDust) {
        float interval = isDust ? 0.05f : TasHelperSettings.SpinnerInterval;
        for (int i = 0; i < TasHelperSettings.SpinnerCountdownUpperBound; i++) {
            if (Math.Floor((TimeActive - offset - Monocle.Engine.DeltaTime) / interval) < Math.Floor((TimeActive - offset) / interval)) {
                return i;
            }
            else {
                TimeActive += Monocle.Engine.DeltaTime;
            }
        }
        return TasHelperSettings.SpinnerCountdownUpperBound;
    }

    public static int CalculateSpinnerGroup(float TimeActive, float offset) {
        int CountdownTimer = 0;
        while (Math.Floor((TimeActive - offset - Monocle.Engine.DeltaTime) / 0.05f) >= Math.Floor((TimeActive - offset) / 0.05f) && CountdownTimer < 3) {
            TimeActive += Monocle.Engine.DeltaTime;
            CountdownTimer++;
        }
        if (CountdownTimer < 3) {
            return (CountdownTimer + TAS.EverestInterop.Hitboxes.CycleHitboxColor.GroupCounter) % 3;
        }
        else return 3;
    }
}
