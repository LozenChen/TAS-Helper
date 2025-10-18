using Monocle;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner.Info;

// Maintain this class

// todo : also maintain SimplifiedSpinner (maybe partially merge into this class?)
internal static class HazardInfoMaintain {

    [Initialize]
    public static void Initialize() {
        Assembly Vanilla = ModUtils.VanillaAssembly;

        HazardInfo.Create(
            Vanilla.GetType("Celeste.CrystalStaticSpinner")!,
            spinner,
            e => (e as CrystalStaticSpinner)!.offset,
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: false
        );

        HazardInfo.Create(
            Vanilla.GetType("Celeste.Lightning")!,
            lightning,
            e => (e as Lightning)!.toggleOffset,
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: false
        );

        HazardInfo.Create(
            Vanilla.GetType("Celeste.DustStaticSpinner")!,
            dust,
            e => (e as DustStaticSpinner)!.offset,
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: false
        );

        if (ModUtils.GetType("FrostHelper", "FrostHelper.CustomSpinner") is { } frostSpinnerType && ModUtils.GetType("FrostHelper", "FrostHelper.CustomSpinnerController") is { } frostControllerType) {
            HazardInfo.Create(
                frostSpinnerType,
                e => e.GetFieldValue<bool>("HasCollider") ? spinner : null,
                "offset",
                hasPeriodicInViewCheck: true,
                modLightningCollidable: null,
                noCycle: e => {
                    if (frostSpinnerType.GetFieldInfo("controller").GetValue(e) is { } controller
                        && frostControllerType.GetFieldInfo("NoCycles").GetValue(controller) is bool b) {
                        return b;
                    }
                    return false;
                },
                recordModPosition: true
            );
        }

        HazardInfo.Create(
            ModUtils.GetType("FrostHelper", "FrostHelper.AttachedLightning"),
            lightning,
            e => (e as Lightning)!.toggleOffset,
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: false
        );

        HazardInfo.Create(
            ModUtils.GetType("VivHelper", "VivHelper.Entities.CustomSpinner"),
            e => e.Collider is null ? null : spinner,
            "offset",
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: true
        );

        HazardInfo.Create(
            ModUtils.GetType("VivHelper", "VivHelper.Entities.AnimatedSpinner"),
            e => e.Collider is null ? null : spinner,
            "offset",
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: false
        );

        HazardInfo.Create(
            ModUtils.GetType("VivHelper", "VivHelper.Entities.MovingSpinner"),
            e => e.Collider is null ? null : spinner,
            "offset",
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: false
        );

        HazardInfo.Create(
            ModUtils.GetType("ChronoHelper", "Celeste.Mod.ChronoHelper.Entities.ShatterSpinner"),
            spinner,
            "offset",
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: true
        );

        if (ModUtils.GetType("ChronoHelper", "Celeste.Mod.ChronoHelper.Entities.DarkLightning") is { } chronoLightningType) {
            HazardInfo.Create(
                chronoLightningType,
                lightning,
                "toggleOffset",
                hasPeriodicInViewCheck: true,
                modLightningCollidable: e => {
                    if (!e.Collidable) {
                        return false;
                    }
                    if (chronoLightningType.GetFieldInfo("disappearing").GetValue(e) is bool b) {
                        return !b;
                    }
                    return true;
                },
                noCycle: null,
                recordModPosition: true
            );
        }

        HazardInfo.Create(
            ModUtils.GetType("XaphanHelper", "Celeste.Mod.XaphanHelper.Entities.CustomSpinner"),
            spinner,
            "offset",
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: true
        );
        // techinically we need to handle its "Hidden" / "AlwaysCollidable" fields, but ... "AlwaysCollidable" is actually just no near player check
        // and "Hidden" is sort of FreezedNeverActivate, but it seems this only happen when some cutscene occurs?

        HazardInfo.Create(
            ModUtils.GetType("ChroniaHelper", "ChroniaHelper.Entities.SeamlessSpinner"),
            spinner,
            "offset",
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: true
        );

        HazardInfo.Create(
            ModUtils.GetType("BrokemiaHelper", "BrokemiaHelper.CassetteSpinner"),
            spinner,
            e => (e as CrystalStaticSpinner)!.offset,
            hasPeriodicInViewCheck: false,
            modLightningCollidable: null,
            noCycle: _ => true,
            recordModPosition: false
        );
        // CassetteSpinner also has a MethodInfo field called offset
        // visible is still governed by offset
        // but collidable is completely determined by cassette, so we consider it as no cycle and no in view behavior

        HazardInfo.Create(
            ModUtils.GetType("IsaGrabBag", "Celeste.Mod.IsaGrabBag.DreamSpinner"),
            spinner,
            _ => 0,
            hasPeriodicInViewCheck: false,
            modLightningCollidable: null,
            noCycle: _ => true,
            recordModPosition: false
        );

        HazardInfo.Create(
            ModUtils.GetType("Glyph", "Celeste.Mod.AcidHelper.Entities.AcidLightning"),
            lightning,
            e => (e as Lightning)!.toggleOffset,
            hasPeriodicInViewCheck: true,
            modLightningCollidable: e => e.Collidable,
            noCycle: null,
            recordModPosition: false
        );
        // this class has its own "toggleOffset" and "disappearing", but they do nothing. And it inherits the Lightning class
        // so it has two PlayerColliders
        // the base PlayerCollider will skip check when base.disappearing
        // but its own PlayerCollider will never skip check, coz it's never disappearing

        HazardInfo.Create(
            ModUtils.GetType("StunningHelper", "Celeste.Mod.StunningHelper.CustomOffsetSpinner"),
            spinner,
            e => (e as CrystalStaticSpinner)!.offset,
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: false
        );

        HazardInfo.Create(
            ModUtils.GetType("StunningHelper", "Celeste.Mod.StunningHelper.CustomOffsetLightning"),
            lightning,
            e => (e as Lightning)!.toggleOffset,
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: false
        );
        HazardInfo.Create(
            ModUtils.GetType("StunningHelper", "Celeste.Mod.StunningHelper.CustomOffsetDustBunny"),
            dust,
            e => (e as DustStaticSpinner)!.offset,
            hasPeriodicInViewCheck: true,
            modLightningCollidable: null,
            noCycle: null,
            recordModPosition: false
        );

        // ModUtils.GetType("Scuffed Helper", "ScuffedHelperCode.RandomSpinner")
        // no, ScuffedHelperCode.RandomSpinner is internel so i can't track it easily
        // this really annoyed me so i won't add support for this

        // Conqueror's Peak has a DestructableSpinner, which is later integrated into ShatterSpinner in ChronoHelper i guess
        // unnecessary to add support for DestructibleSpinner

        // will update this according to https://maddie480.ovh/celeste/custom-entity-catalog

        // [Meaningless] FrostHelper.ArbitraryShapeLightning: have no cycle. its hitbox is ... interesting
        // [BANNED] ScuffedHelper.RandomSpinner: same as crys spinner, but will remove self randomly on load level.
        // [Irrelavent] LunaticHelper.CustomDust: it's a backdrop, not a dust spinner.
    }

    private class HazardInfo {
        internal Type Type;

        internal int? HazardType;

        internal GetDelegate<Entity, int?> HazardTypeGetter;

        internal GetDelegate<object, float> OffsetGetter;

        internal Func<Entity, bool> ModLightningCollidable;

        internal bool HasPeriodicInViewCheck;

        internal Func<Entity, bool> NoCycle;

        internal bool RecordModPosition; // only the base class need this

        private HazardInfo(Type type, bool hasPeriodicInViewCheck, Func<Entity, bool> lightningCollidable, Func<Entity, bool> noCycle, bool recordModPosition) {
            Type = type;
            ModLightningCollidable = lightningCollidable;
            HasPeriodicInViewCheck = hasPeriodicInViewCheck;
            NoCycle = noCycle;
            RecordModPosition = recordModPosition;
        }

        // sadly we don't have a union class
        public static void Create(Type type, int hazardType, string offsetName, bool hasPeriodicInViewCheck, Func<Entity, bool> modLightningCollidable, Func<Entity, bool> noCycle, bool recordModPosition) {
            if (type is null) {
                return;
            }
            HazardInfo instance = new HazardInfo(type, hasPeriodicInViewCheck, modLightningCollidable, noCycle, recordModPosition);
            instance.HazardType = hazardType;
            instance.HazardTypeGetter = null;
            instance.OffsetGetter = type.CreateGetDelegate<object, float>(offsetName);
            instance.SendToHelpers();
        }
        public static void Create(Type type, GetDelegate<Entity, int?> hazardTypeGetter, string offsetName, bool hasPeriodicInViewCheck, Func<Entity, bool> modLightningCollidable, Func<Entity, bool> noCycle, bool recordModPosition) {
            if (type is null) {
                return;
            }
            HazardInfo instance = new HazardInfo(type, hasPeriodicInViewCheck, modLightningCollidable, noCycle, recordModPosition);
            instance.HazardType = null;
            instance.HazardTypeGetter = hazardTypeGetter;
            instance.OffsetGetter = type.CreateGetDelegate<object, float>(offsetName);
            instance.SendToHelpers();
        }
        public static void Create(Type type, int hazardType, GetDelegate<object, float> offsetGetter, bool hasPeriodicInViewCheck, Func<Entity, bool> modLightningCollidable, Func<Entity, bool> noCycle, bool recordModPosition) {
            if (type is null) {
                return;
            }
            HazardInfo instance = new HazardInfo(type, hasPeriodicInViewCheck, modLightningCollidable, noCycle, recordModPosition);
            instance.HazardType = hazardType;
            instance.HazardTypeGetter = null;
            instance.OffsetGetter = offsetGetter;
            instance.SendToHelpers();
        }
        public static void Create(Type type, GetDelegate<Entity, int?> hazardTypeGetter, GetDelegate<object, float> offsetGetter, bool hasPeriodicInViewCheck, Func<Entity, bool> modLightningCollidable, Func<Entity, bool> noCycle, bool recordModPosition) {
            if (type is null) {
                return;
            }
            HazardInfo instance = new HazardInfo(type, hasPeriodicInViewCheck, modLightningCollidable, noCycle, recordModPosition);
            instance.HazardType = null;
            instance.HazardTypeGetter = hazardTypeGetter;
            instance.OffsetGetter = offsetGetter;
            instance.SendToHelpers();
        }

        private void SendToHelpers() {
            if (HazardType is not null) {
                HazardTypeHelper.AddHazardType(Type, HazardType.Value);
            }
            else if (HazardTypeGetter is not null) {
                HazardTypeHelper.AddHazardType(Type, HazardTypeGetter);
            }
            else {
                throw new Exception($"{Type} is never a Hazard ?");
            }

            OffsetHelper.Add(Type, OffsetGetter);
            if (ModLightningCollidable is not null) {
                CollidableHelper.Add(Type, ModLightningCollidable);
            }
            if (!HasPeriodicInViewCheck) {
                SpecialInfoHelper.AddNoPeriodicInViewCheck(Type);
            }
            if (NoCycle is not null) {
                SpecialInfoHelper.AddNoCycle(Type, NoCycle);
            }
            if (RecordModPosition) {
                PositionHelper.Patch(Type);
            }
        }
    }
}