using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using System.Runtime.CompilerServices;
// VivHelper namespace has a VivHelper class.... so if we want to visit VivHelper.Entities, we should use VivEntities

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;

public static class SpinnerCalculateHelper {

    public static float TimeActive = 0f;

    public static float[] PredictLoadTimeActive = new float[10];
    public static float[] PredictUnloadTimeActive = new float[100];

    [Load]
    public static void Load() {
        On.Monocle.Scene.AfterUpdate += PatchAfterUpdate;
    }

    [Unload]
    public static void Unload() {
        On.Monocle.Scene.AfterUpdate -= PatchAfterUpdate;
    }

    private static void PatchAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
        orig(self);
        PreSpinnerCalculate(self);
    }

    // JIT optimization may cause PredictLoadTimeActive[2] != 524288f when TimeActive = 524288f
    [MethodImpl(MethodImplOptions.NoOptimization)]
    internal static void PreSpinnerCalculate(Scene self) {
        if (!TasHelperSettings.Enabled) {
            return;
        }
        float time = TimeActive = self.TimeActive;
        for (int i = 0; i <= 9; i++) {
            PredictLoadTimeActive[i] = PredictUnloadTimeActive[i] = time;
            time += Engine.DeltaTime;
        }
        for (int i = 10; i <= 99; i++) {
            PredictUnloadTimeActive[i] = time;
            time += Engine.DeltaTime;
        }
        // this must be before tas mod's FreeCameraHitbox.SubHudRendererOnBeforeRender, otherwise spinners will flash if you zoom out in center camera mode
    }

    private static void DictionaryAdderNormal(Type type, string offsetName, int HazardType) {
        HazardTypesTreatNormal.Add(type, HazardType);
        OffsetGetters.Add(type, type.CreateGetDelegate<object, float>(offsetName));
    }

    private static void DictionaryAdderSpecial(Type type, string offsetName, GetDelegate<object, int?> HazardTypeGetter) {
        HazardTypesTreatSpecial.Add(type, HazardTypeGetter);
        OffsetGetters.Add(type, type.CreateGetDelegate<object, float>(offsetName));
    }

    [Initialize]
    public static void Initialize() {
        Assembly Vanilla = ModUtils.VanillaAssembly;
        DictionaryAdderNormal(Vanilla.GetType("Celeste.CrystalStaticSpinner"), "offset", spinner);
        DictionaryAdderNormal(Vanilla.GetType("Celeste.Lightning"), "toggleOffset", lightning);
        DictionaryAdderNormal(Vanilla.GetType("Celeste.DustStaticSpinner"), "offset", dust);
        // for some reasons mentioned below, subclass should be considered different, so we add these three types into dictionary, instead of manually check "if (entity is CrystalStaticSpinner) ..."

        if (ModUtils.GetType("FrostHelper", "FrostHelper.CustomSpinner") is { } frostSpinnerType && ModUtils.GetType("FrostHelper", "FrostHelper.CustomSpinnerController") is { } frostControllerType) {
            DictionaryAdderSpecial(frostSpinnerType, "offset", e => e.GetFieldValue<bool>("HasCollider") ? spinner : null);
            NoCycleTypes.Add(frostSpinnerType, e => {
                if (frostSpinnerType.GetFieldInfo("controller").GetValue(e) is { } controller && frostControllerType.GetFieldInfo("NoCycles").GetValue(controller) is bool b) {
                    return b;
                }
                return false;
            });
        }

        if (ModUtils.GetType("FrostHelper", "FrostHelper.AttachedLightning") is { } frostAttLightningType) {
            DictionaryAdderNormal(frostAttLightningType, "toggleOffset", lightning);
            // this is a subclass of Lightning. CelesteTAS use "... is Lightning ..." so it applies to this derived class. I want to be more precise so i implement it type by type. So CassetteSpinner can be elsewhere carefully included
        }

        //if (ModUtils.GetType("FrostHelper", "FrostHelper.ArbitraryShapeLightning") is { } frostArbLightningType) {
        //    HazardTypesTreatNormal.Add(frostArbLightningType, lightning);
        //    OffsetGetters.Add(frostArbLightningType, _ => 0);
        //}
        // i can do this and hook its debug render alone, but that would be... really no difference from its original implmentation

        if (ModUtils.GetType("VivHelper", "VivHelper.Entities.CustomSpinner") is { } vivSpinnerType) {
            VivSpinnerType = vivSpinnerType;
            DictionaryAdderSpecial(vivSpinnerType, "offset", e => e.GetFieldValue("Collider") is null ? null : spinner);
            VivHitboxStringGetter = vivSpinnerType.GetField("hitboxString", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        if (ModUtils.GetType("VivHelper", "VivHelper.Entities.AnimatedSpinner") is { } vivAnimSpinnerType) {
            DictionaryAdderSpecial(vivAnimSpinnerType, "offset", e => e.GetFieldValue("Collider") is null ? null : spinner);
        }

        if (ModUtils.GetType("VivHelper", "VivHelper.Entities.MovingSpinner") is { } vivMoveSpinnerType) {
            DictionaryAdderSpecial(vivMoveSpinnerType, "offset", e => e.GetFieldValue("Collider") is null ? null : spinner);
        }

        if (ModUtils.GetType("ChronoHelper", "Celeste.Mod.ChronoHelper.Entities.ShatterSpinner") is { } chronoSpinnerType) {
            DictionaryAdderNormal(chronoSpinnerType, "offset", spinner);
        }

        if (ModUtils.GetType("ChronoHelper", "Celeste.Mod.ChronoHelper.Entities.DarkLightning") is { } chronoLightningType) {
            DictionaryAdderNormal(chronoLightningType, "toggleOffset", lightning);
            LightningCollidable.Add(chronoLightningType, e => {
                if (chronoLightningType.GetFieldInfo("disappearing").GetValue(e) is bool b) {
                    return b;
                }
                return true;
            });
        }

        if (ModUtils.GetType("BrokemiaHelper", "BrokemiaHelper.CassetteSpinner") is { } cassetteSpinnerType) {
            HazardTypesTreatNormal.Add(cassetteSpinnerType, spinner);
            OffsetGetters.Add(cassetteSpinnerType, OffsetGetters[typeof(CrystalStaticSpinner)]);
            NoPeriodicInViewCheckTypes.Add(cassetteSpinnerType);
            NoCycleTypes.Add(cassetteSpinnerType, _ => true);
            // CassetteSpinner also has a MethodInfo field called offset
            // visible is still governed by offset
            // but collidable is completely determined by cassette, so we consider it as no cycle and no in view behavior
        }

        if (ModUtils.GetType("IsaGrabBag", "Celeste.Mod.IsaGrabBag.DreamSpinner") is { } dreamSpinnerType) {
            HazardTypesTreatNormal.Add(dreamSpinnerType, spinner);
            OffsetGetters.Add(dreamSpinnerType, _ => 0);
            NoPeriodicInViewCheckTypes.Add(dreamSpinnerType);
            NoCycleTypes.Add(dreamSpinnerType, _ => true);
        }


        //if (ModUtils.GetType("Scuffed Helper", "ScuffedHelperCode.RandomSpinner") is { } randomSpinnerType) {
        //    DictionaryAdderNormal(randomSpinnerType, "offset", spinner);
        //}
        // no, ScuffedHelperCode.RandomSpinner is internel so i can't track it easily
        // this really annoyed me so i won't add support for this

        // Conqueror's Peak has a DestructableSpinner, which is later integrated into ShatterSpinner in ChronoHelper i guess
        // unnecessary to add support for DestructibleSpinner

        // will update this according to https://maddie480.ovh/celeste/custom-entity-catalog

        // [Done] IsaGrabBag.DreamSpinner: used in UltraDifficult. have no cycle. static. affected by in view. need to clear sprites and simplify
        // [Meaningless] FrostHelper.ArbitraryShapeLightning: have no cycle. its hitbox is ... interesting
        // [Done] BrokemiaHelper.CassetteSpinner: brokemia defines a CassetteEntity interface, which is basically same as CassetteBlock? and CassetteSpinner is a crys spinner with CassetteEntity interface, its update is also affected. only need to clear sprites and simplify
        // [BANNED] ScuffedHelper.RandomSpinner: same as crys spinner, but will remove self randomly on load level.
        // [Irrelavent] LunaticHelper.CustomDust: it's a backdrop, not a dust spinner.
    }

    private static Dictionary<Type, int> HazardTypesTreatNormal = new();

    private static Dictionary<Type, GetDelegate<object, int?>> HazardTypesTreatSpecial = new();

    private static Dictionary<Type, GetDelegate<object, float>> OffsetGetters = new();

    private static List<Type> NoPeriodicInViewCheckTypes = new();

    private static Dictionary<Type, Func<Entity, bool>> LightningCollidable = new();

    private static Dictionary<Type, Func<Entity, bool>> NoCycleTypes = new();

    public static FieldInfo VivHitboxStringGetter;

    private static Type VivSpinnerType;

    public static bool NoCycle(Entity self) {
        if (NoCycleTypes.TryGetValue(self.GetType(), out Func<Entity, bool> func)) {
            return func(self);
        }
        return false;
    }

    public static bool IsVivSpinner(Entity self) {
        return self.GetType().IsSameOrSubclassOf(VivSpinnerType);
    }

    public static bool GetCollidable(Entity self) {
        if (LightningCollidable.TryGetValue(self.GetType(), out Func<Entity, bool> func)) {
            return func(self);
        }
        if (self is Lightning lightning) {
            // FrostHelper.AttachedLightning inherits from Lightning, so no need to check before
            return lightning.Collidable && !lightning.disappearing;
        }
        return self.Collidable;
    }

    public static bool NoPeriodicCheckInViewBehavior(Entity self) {
        if (self.isDust()) {
            return true;
        }
        if (NoPeriodicInViewCheckTypes.Contains(self.GetType())) {
            return true;
        }
        return false;
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

    public static float? GetOffset(Entity self) {
        if (OffsetGetters.TryGetValue(self.GetType(), out GetDelegate<object, float> getter)) {
            return getter(self);
        }
        return null;
    }
    public static bool isSpinnner(this Entity self) {
        return HazardType(self) == spinner;
    }
    public static bool isLightning(this Entity self) {
        return HazardType(self) == lightning;
    }
    public static bool isDust(this Entity self) {
        return HazardType(self) == dust;
    }

    public static bool LightningInView(Entity self, Vector2 CameraPos) {
        float zoom = ActualPosition.CameraZoom;
        return self.X + self.Width > CameraPos.X - 16f && self.Y + self.Height > CameraPos.Y - 16f && self.X < CameraPos.X + 320f * zoom + 16f && self.Y < CameraPos.Y + 180f * zoom + 16f;
    }
    public static bool InView(Entity self, Vector2 CameraPos) {
        float zoom = ActualPosition.CameraZoom;
        if (self.isLightning()) {
            // i guess this order of comparison is more efficient
            return self.X + self.Width > CameraPos.X - 16f && self.Y + self.Height > CameraPos.Y - 16f && self.X < CameraPos.X + 320f * zoom + 16f && self.Y < CameraPos.Y + 180f * zoom + 16f;
        }
        else {
            return self.X > CameraPos.X - 16f && self.Y > CameraPos.Y - 16f && self.X < CameraPos.X + 320f * zoom + 16f && self.Y < CameraPos.Y + 180f * zoom + 16f;
        }
    }
    public static bool InView(Vector2 pos, float Width, float Height, Vector2 CameraPos, bool isLightning) {
        float zoom = ActualPosition.CameraZoom;
        if (isLightning) {
            return pos.X < CameraPos.X + 320f * zoom + 16f && pos.Y < CameraPos.Y + 180f * zoom + 16f && pos.Y + Height > CameraPos.Y - 16f && pos.X + Width > CameraPos.X - 16f;
        }
        else {
            return pos.X < CameraPos.X + 320f * zoom + 16f && pos.Y < CameraPos.Y + 180f * zoom + 16f && pos.Y > CameraPos.Y - 16f && pos.X > CameraPos.X - 16f;
        }
    }

    public static bool FarFromRange(Entity self, Vector2 PlayerPosition, float scale) {
        if (TasSettings.CenterCamera) {
            return FarFromRangeImpl(self, PlayerPosition, ActualPosition.CenterCameraPosition, scale) && FarFromRangeImpl(self, PlayerPosition, ActualPosition.CameraPosition, scale);
        }
        else {
            return FarFromRangeImpl(self, PlayerPosition, ActualPosition.CameraPosition, scale);
        }
    }

    private static bool FarFromRangeImpl(Entity self, Vector2 PlayerPosition, Vector2 CameraPos, float scale) {
        if (self.isLightning()) {
            if (self.X > CameraPos.X + 320f * scale + 320f + 16f || self.Y > CameraPos.Y + 180f * scale + 180f + 16f || self.Y + self.Height < CameraPos.Y - 180f * scale - 16f || self.X + self.Width < CameraPos.X - 320f * scale - 16f) {
                return true;
            }
        }
        else {
            if (self.X > CameraPos.X + 320f * scale + 336f || self.Y > CameraPos.Y + 180f * scale + 196f || self.Y < CameraPos.Y - 180f * scale - 16f || self.X < CameraPos.X - 320f * scale - 16f) {
                return Math.Abs(self.X - PlayerPosition.X) > 128f + 256f * scale || Math.Abs(self.Y - PlayerPosition.Y) > 128f + 256f * scale;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OnInterval(float TimeActive, float interval, float offset, float DeltaTime) {
        return Math.Floor(((double)TimeActive - offset - DeltaTime) / interval) < Math.Floor(((double)TimeActive - offset) / interval);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OnInterval(float TimeActive, float interval, float offset) {
        // this function should match https://github.com/EverestAPI/Everest/commit/fa7fc64f74a904eaf4d56f508d25561dde26597f
        return Math.Floor(((double)TimeActive - offset - Engine.DeltaTime) / interval) < Math.Floor(((double)TimeActive - offset) / interval);
    }

    public static int PredictCountdown(float offset, bool isDust) {
        float interval = isDust ? 0.05f : TasHelperSettings.SpinnerInterval;
        if (TasHelperSettings.SpinnerCountdownLoad) {
            for (int i = 0; i < 9; i++) {
                if (OnInterval(PredictLoadTimeActive[i], interval, offset)) return i;
            }
            return 9;
        }
        else {
            for (int i = 0; i < 99; i++) {
                if (OnInterval(PredictUnloadTimeActive[i], interval, offset)) return i;
            }
            return 99;
        }
    }

    public static int CalculateSpinnerGroup(float offset) {
        if (OnInterval(PredictLoadTimeActive[0], 0.05f, offset)) {
            return TAS.EverestInterop.Hitboxes.CycleHitboxColor.GroupCounter;
        }
        if (OnInterval(PredictLoadTimeActive[1], 0.05f, offset)) {
            return (1 + TAS.EverestInterop.Hitboxes.CycleHitboxColor.GroupCounter) % 3;
        }
        if (OnInterval(PredictLoadTimeActive[2], 0.05f, offset)) {
            return (2 + TAS.EverestInterop.Hitboxes.CycleHitboxColor.GroupCounter) % 3;
        }
        return 3;
    }
}
