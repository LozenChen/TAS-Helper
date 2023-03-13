using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;

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
        TimeActive = self.TimeActive;
    }
    public static void Initialize() {
        if (ModUtils.GetType("FrostHelper", "FrostHelper.CustomSpinner") is { } frostSpinnerType) {
            ModHazardTypes.Add(frostSpinnerType, Tuple.Create(spinner, treatSpecial));
        }


    }

    public static Dictionary<Type, Tuple<int, bool>> ModHazardTypes = new();

    internal const int spinner = 0;
    internal const int dust = 1;
    internal const int lightning = 2;
    private const bool treatTrivial = true;
    private const bool treatSpecial = false;
    public static int? HazardType(Entity self) {
        if (self is CrystalStaticSpinner) return spinner;
        if (self is DustStaticSpinner) return dust;
        if (self is Lightning) return lightning;
        Type type = self.GetType();
        if (ModHazardTypes.TryGetValue(type, out Tuple<int, bool> value)) {
            if (value.Item2) {
                return value.Item1;
            }
            if (ModUtils.FrostHelperInstalled) {
                if (self is FrostHelper.CustomSpinner customSpinner) {
                    return customSpinner.HasCollider ? spinner : null;
                }
            }
        }
        return null;
    }
    public static float? GetOffset(Entity self) {
        if (HazardType(self) is not int type) return null;
        string fieldname = type == lightning ? "toggleOffset" : "offset";
        FieldInfo field = self.GetType().GetField(fieldname, BindingFlags.Instance | BindingFlags.NonPublic);
        return (float)field.GetValue(self);
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
        if (isLightning(self)) {
            return self.X + self.Width > CameraPos.X - 16f && self.Y + self.Height > CameraPos.Y - 16f && self.X < CameraPos.X + 320f + 16f && self.Y < CameraPos.Y + 180f + 16f;
        }
        else {
            return self.X > CameraPos.X - 16f && self.Y > CameraPos.Y - 16f && self.X < CameraPos.X + 320f + 16f && self.Y < CameraPos.Y + 180f + 16f;
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

    public static bool InView(Vector2 pos, float Width, float Height, Vector2 CameraPos, bool isLightning) {
        if (isLightning) {
            return pos.X + Width > CameraPos.X - 16f && pos.Y + Height > CameraPos.Y - 16f && pos.X < CameraPos.X + 320f + 16f && pos.Y < CameraPos.Y + 180f + 16f;
        }
        else {
            return pos.X > CameraPos.X - 16f && pos.Y > CameraPos.Y - 16f && pos.X < CameraPos.X + 320f + 16f && pos.Y < CameraPos.Y + 180f + 16f;
        }
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
