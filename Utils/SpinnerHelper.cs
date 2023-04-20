using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using System.Reflection;
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
            DictionaryAdderSpecial(vivSpinnerType, "offset", VivSpinnerHazardType);
        }

        if (ModUtils.GetType("VivHelper", "VivHelper.Entities.AnimatedSpinner") is { } vivAnimSpinnerType) {
            DictionaryAdderSpecial(vivAnimSpinnerType, "offset", VivSpinnerHazardType);
        }

        if (ModUtils.GetType("VivHelper", "VivHelper.Entities.MovingSpinner") is { } vivMoveSpinnerType) {
            DictionaryAdderSpecial(vivMoveSpinnerType, "offset", VivSpinnerHazardType);
        }

        if (ModUtils.GetType("ChronoHelper", "Celeste.Mod.ChronoHelper.Entities.ShatterSpinner") is { } chronoSpinnerType) {
            DictionaryAdderNormal(chronoSpinnerType, "offset", spinner);
        }

        if (ModUtils.GetType("ChronoHelper", "Celeste.Mod.ChronoHelper.Entities.DarkLightning") is { } chronoLightningType) {
            DictionaryAdderNormal(chronoLightningType, "toggleOffset", lightning);
        }

        if (ModUtils.GetType("BrokemiaHelper", "BrokemiaHelper.CassetteSpinner") is { } cassetteSpinnerType) {
            HazardTypesTreatNormal.Add(cassetteSpinnerType, spinner);
            OffsetGetters.Add(cassetteSpinnerType, OffsetGetters[typeof(CrystalStaticSpinner)]);
            // CassetteSpinner also has a MethodInfo field called offset
            // visible is still governed by offset
            // but collidable is completely determined by cassette, so we consider it as no cycle
        }

        if (ModUtils.GetType("IsaGrabBag", "Celeste.Mod.IsaGrabBag.DreamSpinner") is { } dreamSpinnerType) {
            HazardTypesTreatNormal.Add(dreamSpinnerType, spinner);
            OffsetGetters.Add(dreamSpinnerType, _ => 0);
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

        if (ModUtils.IsaGrabBagInstalled) {
            typeof(SpinnerHelper).GetMethod("NoCycle").IlHook((cursor, _) => {
                Instruction skipIsaGrabBag = cursor.Next;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(IsaGrabBagPatch);
                cursor.Emit(OpCodes.Brfalse, skipIsaGrabBag);
                cursor.Emit(OpCodes.Ldc_I4_1);
                cursor.Emit(OpCodes.Ret);
            });
        }

        if (ModUtils.BrokemiaHelperInstalled) {
            typeof(SpinnerHelper).GetMethod("NoCycle").IlHook((cursor, _) => {
                Instruction skipBrokemia = cursor.Next;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(BrokemiaPatch);
                cursor.Emit(OpCodes.Brfalse, skipBrokemia);
                cursor.Emit(OpCodes.Ldc_I4_1);
                cursor.Emit(OpCodes.Ret);
            });
        }

        if (ModUtils.FrostHelperInstalled) {
            typeof(SpinnerHelper).GetMethod("NoCycle").IlHook((cursor, _) => {
                Instruction skipFrost = cursor.Next;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(FrostPatch);
                cursor.Emit(OpCodes.Brfalse, skipFrost);
                cursor.Emit(OpCodes.Ldc_I4_1);
                cursor.Emit(OpCodes.Ret);
            });
        }

        if (ModUtils.VivHelperInstalled) {
            SetVivHitboxStringGetter();
        }
    }

    private static Dictionary<Type, int> HazardTypesTreatNormal = new();

    private static Dictionary<Type, GetDelegate<object, int?>> HazardTypesTreatSpecial = new();

    private static Dictionary<Type, GetDelegate<object, float>> OffsetGetters = new();

    public static FieldInfo VivHitboxStringGetter;

    private static void SetVivHitboxStringGetter() {
        VivHitboxStringGetter = typeof(VivEntites.CustomSpinner).GetField("hitboxString", BindingFlags.NonPublic | BindingFlags.Instance);
    }
    public static bool NoCycle(Entity self) {
        return false;
    }
    private static bool IsaGrabBagPatch(Entity self) {
        return self is IsaGrabBag.DreamSpinner;
    }
    private static bool BrokemiaPatch(Entity self) {
        return self is BrokemiaHelper.CassetteSpinner;
    }

    private static bool FrostPatch(Entity self) {
        if (self is FrostHelper.CustomSpinner spinner) {
            if (typeof(FrostHelper.CustomSpinner).GetFieldInfo("controller").GetValue(spinner) is FrostHelper.CustomSpinnerController controller) {
                return controller.NoCycles;
            }
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
            if (self.X < CameraPos.X - 320f * scale - 16f || self.Y < CameraPos.Y - 180f * scale - 16f || self.X > CameraPos.X + 320f * scale + 336f || self.Y > CameraPos.Y + 180f * scale + 196f) {
                return (Math.Abs(self.X - PlayerPosition.X) > 128f + 256f * scale || Math.Abs(self.Y - PlayerPosition.Y) > 128f + 256f * scale);
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
