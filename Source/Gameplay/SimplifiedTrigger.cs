using Celeste.Mod.Entities;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using TAS.EverestInterop.Hitboxes;
using TAS.EverestInterop.InfoHUD;

namespace Celeste.Mod.TASHelper.Gameplay;
public static class SimplifiedTrigger {

    public static bool Enabled => TasHelperSettings.EnableSimplifiedTriggers;

    public static bool MaybeEnabled => TasHelperSettings.Enabled && TasHelperSettings.EnableSimplifiedTriggersMode != Module.TASHelperSettings.SimplifiedGraphicsMode.Off;

    public static bool HideCameraTriggers => TasHelperSettings.HideCameraTriggers;

    public static bool HideGoldBerryCollectTrigger => TasHelperSettings.HideGoldBerryCollectTrigger;

    public static Color CameraTriggerColor => CustomColors.CameraTriggerColor;


    [Load]
    private static void Load() {
        using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop" }, ID = "TAS Helper SimplifiedTrigger" }) {
            IL.Monocle.Entity.DebugRender += ModDebugRender;
        }
    }

    [Unload]
    private static void Unload() {
        IL.Monocle.Entity.DebugRender -= ModDebugRender;
    }

    [Initialize]
    private static void Initialize() {
        HandleVanillaTrigger();
        HandleEverestTrigger();
        HandleBerryTrigger();
        HandleCameraTrigger(); // now that Loenn 0.8 supports trigger color, should we somehow use that?
        HandleExtendedVariantTrigger();
        HandleContortHelperTrigger();
        HandleOtherMods();
        HandleNonTriggerTrigger();
        typeof(HitboxColor).GetMethodInfo("GetCustomColor", new Type[] { typeof(Color), typeof(Entity) }).IlHook(ModGetCustomColor);
        typeof(InfoWatchEntity).GetMethod("FindClickedEntities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).IlHook(ModFindClickedEntities);
    }

    private static void ModDebugRender(ILContext il) {
        ILCursor ilCursor = new(il);
        Instruction start = ilCursor.Next;
        ilCursor.Emit(OpCodes.Ldarg_0)
            .EmitDelegate<Func<Entity, bool>>(IsUnimportantTrigger);
        ilCursor.Emit(OpCodes.Brfalse, start).Emit(OpCodes.Ret);
    }

    private static void ModFindClickedEntities(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        cursor.Goto(-1);
        cursor.EmitDelegate(FilterOutUnimportantTrigger);
    }

    private static List<Entity> FilterOutUnimportantTrigger(List<Entity> list) {
        return list.Where(e => !IsUnimportantTrigger(e)).ToList();
    }

    private static void ModGetCustomColor(ILContext il) {
        ILCursor cursor = new(il);
        if (cursor.TryGotoNext(ins => ins.MatchLdsfld(typeof(HitboxColor), nameof(HitboxColor.RespawnTriggerColor)), ins => ins.OpCode == OpCodes.Stloc_1, ins => ins.OpCode == OpCodes.Br_S)) {
            ILLabel label = (ILLabel)cursor.Next.Next.Next.Operand;
            cursor.Goto(0);
            if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Ldarg_1, ins => ins.MatchIsinst<ChangeRespawnTrigger>(), ins => ins.OpCode == OpCodes.Brtrue_S)) {
                Instruction next = cursor.Next;
                cursor.MoveAfterLabels();
                cursor.EmitDelegate(CameraTriggerColorEnabled);
                cursor.Emit(OpCodes.Brfalse, next);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(IsCameraTrigger);
                cursor.Emit(OpCodes.Brfalse, next);
                cursor.EmitDelegate(GetCameraTriggerColor);
                cursor.Emit(OpCodes.Stloc_1);
                cursor.Emit(OpCodes.Br, label);
            }
        }
    }

    private static bool CameraTriggerColorEnabled() {
        return TasHelperSettings.EnableCameraTriggerColor;
    }

    private static Color GetCameraTriggerColor() {
        return CameraTriggerColor;
    }

    public static bool IsUnimportantTrigger(Entity entity) {
        return Enabled && UnimportantTriggers.Contains(entity) && !TAS.EverestInterop.InfoHUD.InfoWatchEntity.WatchingEntities.Contains(entity);
    }

    public static bool IsCameraTrigger(Entity entity) {
        return cameraTriggers.Contains(entity.GetType());
    }

    internal static HashSet<Entity> UnimportantTriggers = new();

    private static readonly List<Func<Entity, bool>> UnimportantCheckers = new();

    public static readonly List<string> RemainingTriggersList = new();

    // we leave it to people who are curious about this
    public static string RemainingTriggers => "\n" + string.Join("\n", RemainingTriggersList);

    private static void AddCheck(Func<Entity, bool> condition) {
        UnimportantCheckers.Add(condition);
    }

    [Tracked]
    private class TriggerInfoBuilder : Entity {

        private static bool lastInstanceRemoved = false;
        public TriggerInfoBuilder() {
            Tag = Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate;
            lastInstanceRemoved = Engine.Scene.Tracker.SafeGetEntities<TriggerInfoBuilder>().IsEmpty();
            Add(new Coroutine(CreateRemainingTriggerList()));
        }

        public override void Removed(Scene scene) {
            lastInstanceRemoved = true;
            base.Removed(scene);
        }

        [LoadLevel]
        public static void Build(Level level) {
            if (!MaybeEnabled) {
                level.Tracker.SafeGetEntities<TriggerInfoBuilder>().ForEach(x => x.RemoveSelf());
                UnimportantTriggers.Clear();
                RemainingTriggersList.Clear();
                lastInstanceRemoved = true;
                return;
            }
            level.Add(new TriggerInfoBuilder());
        }

        private System.Collections.IEnumerator CreateRemainingTriggerList() {
            if (Engine.Scene is not Level level) {
                yield break;
            }
            UnimportantTriggers.Clear();
            foreach (Entity entity in level.Tracker.GetEntities<Trigger>()) {
                foreach (Func<Entity, bool> checker in UnimportantCheckers) {
                    if (checker(entity)) {
                        UnimportantTriggers.Add(entity);
                        //Logger.Log(LogLevel.Verbose, "TAS Helper", $"Hide Trigger: {entity.GetEntityId()}");
                        break;
                    }
                }
            }
            foreach (Type type in nonTriggerTypes) {
                if (level.Tracker.Entities.TryGetValue(type, out List<Entity> list)) {
                    foreach (Entity entity in list) {
                        UnimportantTriggers.Add(entity);
                        //Logger.Log(LogLevel.Verbose, "TAS Helper", $"Hide Entity: {entity.GetEntityId()}");
                    }
                }
            }
            while (!lastInstanceRemoved) { // which is the same time when triggers in last room also get removed (e.g. in transition routine)
                yield return null;
            }
            // i just realize that i can use a TransitionListener.OnInEnd instead...
            RemainingTriggersList.Clear();
            foreach (Entity entity in level.Tracker.GetEntities<Trigger>().Where(x => !UnimportantTriggers.Contains(x))) {
                RemainingTriggersList.Add(GetTriggerInfo(entity));
            }
            Active = false;
        }
    }

    public static string GetTriggerInfo(Entity trigger) {
        // todo: provide more info for e.g. FlagTrigger, CameraTriggers, TriggerTrigger
        // update: Done, see AutoWatch
        return trigger.GetEntityId();
    }

    private static void HandleVanillaTrigger() {
        AddCheck(entity => vanillaTriggers.Contains(entity.GetType()));
    }

    private static readonly HashSet<Type> vanillaTriggers = new() { typeof(BirdPathTrigger), typeof(BlackholeStrengthTrigger), typeof(AmbienceParamTrigger), typeof(MoonGlitchBackgroundTrigger), typeof(BloomFadeTrigger), typeof(LightFadeTrigger), typeof(AltMusicTrigger), typeof(MusicTrigger), typeof(MusicFadeTrigger) };

    private static void HandleEverestTrigger() {
        AddCheck(entity => everestTriggers.Contains(entity.GetType()));
    }

    private static readonly HashSet<Type> everestTriggers = new() { typeof(AmbienceTrigger), typeof(AmbienceVolumeTrigger), typeof(CustomBirdTutorialTrigger), typeof(MusicLayerTrigger) };

    private static void HandleBerryTrigger() {
        AddCheck(entity => HideGoldBerryCollectTrigger && goldBerryTriggers.Contains(entity.GetType()));
        GetTypes("CollabUtils2", "Celeste.Mod.CollabUtils2.Triggers.SpeedBerryCollectTrigger", "Celeste.Mod.CollabUtils2.Triggers.SilverBerryCollectTrigger").ForEach(x => goldBerryTriggers.Add(x));
    }

    internal static void OnHideBerryChange(bool enable) {
        if (Engine.Scene is not Level level) {
            return;
        }
        if (enable) {
            foreach (Entity entity in level.Tracker.GetEntities<Trigger>()) {
                if (goldBerryTriggers.Contains(entity.GetType())) {
                    UnimportantTriggers.Add(entity);
                }
            }
        }
        else {
            UnimportantTriggers.RemoveWhere(x => goldBerryTriggers.Contains(x.GetType()));
        }
    }

    private static readonly HashSet<Type> goldBerryTriggers = new() { typeof(GoldBerryCollectTrigger) };

    private static void HandleCameraTrigger() {
        AddCheck(entity => HideCameraTriggers && cameraTriggers.Contains(entity.GetType()));
        AddTypes("ContortHelper", "ContortHelper.PatchedCameraAdvanceTargetTrigger", "ContortHelper.PatchedCameraOffsetTrigger", "ContortHelper.PatchedCameraTargetTrigger", "ContortHelper.PatchedSmoothCameraOffsetTrigger");
        AddTypes("FrostHelper", "FrostHelper.EasedCameraZoomTrigger");
        AddTypes("FurryHelper", "Celeste.Mod.FurryHelper.MomentumCameraOffsetTrigger");
        AddTypes("HonlyHelper", "Celeste.Mod.HonlyHelper.CameraTargetCornerTrigger", "Celeste.Mod.HonlyHelper.CameraTargetCrossfadeTrigger");
        AddTypes("MaxHelpingHand", "Celeste.Mod.MaxHelpingHand.Triggers.CameraCatchupSpeedTrigger", "Celeste.Mod.MaxHelpingHand.Triggers.CameraOffsetBorder", "Celeste.Mod.MaxHelpingHand.Triggers.OneWayCameraTrigger");
        AddTypes("Sardine7", "Celeste.Mod.Sardine7.Triggers.SmoothieCameraTargetTrigger");
        AddTypes("VivHelper", "VivHelper.Triggers.InstantLockingCameraTrigger", "VivHelper.Triggers.MultiflagCameraTargetTrigger");
        AddTypes("XaphanHelper", "Celeste.Mod.XaphanHelper.Triggers.CameraBlocker");

        void AddTypes(string modName, params string[] typeNames) {
            AddTypesImpl(cameraTriggers, modName, typeNames);
        }
    }

    internal static void OnHideCameraChange(bool enable) {
        if (Engine.Scene is not Level level) {
            return;
        }
        if (enable) {
            foreach (Entity entity in level.Tracker.GetEntities<Trigger>()) {
                if (cameraTriggers.Contains(entity.GetType())) {
                    UnimportantTriggers.Add(entity);
                }
            }
        }
        else {
            UnimportantTriggers.RemoveWhere(x => cameraTriggers.Contains(x.GetType()));
        }
    }

    private static readonly HashSet<Type> cameraTriggers = new() { typeof(CameraOffsetTrigger), typeof(CameraTargetTrigger), typeof(CameraAdvanceTargetTrigger), typeof(SmoothCameraOffsetTrigger) };

    private static void HandleExtendedVariantTrigger() {
        // we need to handle AbstractExtendedVariantTrigger<T> and ExtendedVariantTrigger
        // we check its "variantChange" field to determine if it's unimportant

        if (ModUtils.GetType("ExtendedVariantMode", "ExtendedVariants.Module.ExtendedVariantsModule+Variant") is { } variantEnumType && variantEnumType.IsEnum) {
            List<string> ignoreVariantString = new() { "RoomLighting", "RoomBloom", "GlitchEffect", "ColorGrading", "ScreenShakeIntensity", "AnxietyEffect", "BlurLevel", "ZoomLevel", "BackgroundBrightness", "DisableMadelineSpotlight", "ForegroundEffectOpacity", "MadelineIsSilhouette", "DashTrailAllTheTime", "FriendlyBadelineFollower", "MadelineHasPonytail", "MadelineBackpackMode", "BackgroundBlurLevel", "AlwaysInvisible", "DisplaySpeedometer", "DisableKeysSpotlight", "SpinnerColor", "InvisibleMotion", "PlayAsBadeline" };
            extendedVariants = ignoreVariantString.Select(x => GetEnum(variantEnumType, x)).Where(x => x is not null).ToList();

            GetTypes("ExtendedVariantMode",
                "ExtendedVariants.Entities.Legacy.ExtendedVariantTrigger",
                "ExtendedVariants.Entities.Legacy.ExtendedVariantFadeTrigger",
                "ExtendedVariants.Entities.ForMappers.FloatExtendedVariantFadeTrigger"
            ).ForEach(type => {
                AddCheck(x =>
                       x.GetType() == type
                    && x.GetFieldValue("variantChange") is { } variantChange
                    && extendedVariants.Contains(variantChange)
                );
            });

            if (ModUtils.GetType("ExtendedVariantMode", "ExtendedVariants.Entities.ForMappers.AbstractExtendedVariantTrigger`1") is { } abstractExtendedVariantTriggerType) {
                AddCheck(x =>
                       x.GetType().BaseType is Type type
                    && type.IsGenericType
                    && type.GetGenericTypeDefinition() == abstractExtendedVariantTriggerType
                    && x.GetFieldValue("variantChange") is { } variantChange
                    && extendedVariants.Contains(variantChange)
                );
            }
        }
    }

    private static List<object> extendedVariants = new();

    private static void HandleContortHelperTrigger() {
        GetTypes("ContortHelper", "ContortHelper.AnxietyEffectTrigger", "ContortHelper.BloomRendererModifierTrigger", "ContortHelper.BurstEffectTrigger", "ContortHelper.BurstRemoverTrigger", "ContortHelper.ClearCustomEffectsTrigger", "ContortHelper.CustomConfettiTrigger", "ContortHelper.CustomEffectTrigger", "ContortHelper.EffectBooleanArrayParameterTrigger", "ContortHelper.EffectBooleanParameterTrigger", "ContortHelper.EffectColorParameterTrigger", "ContortHelper.EffectFloatArrayParameterTrigger", "ContortHelper.EffectFloatParameterTrigger", "ContortHelper.EffectIntegerArrayParameterTrigger", "ContortHelper.EffectIntegerParameterTrigger", "ContortHelper.EffectMatrixParameterTrigger", "ContortHelper.EffectQuaternionParameterTrigger", "ContortHelper.EffectStringParameterTrigger", "ContortHelper.EffectVector2ParameterTrigger", "ContortHelper.EffectVector3ParameterTrigger", "ContortHelper.EffectVector4ParameterTrigger", "ContortHelper.FlashTrigger", "ContortHelper.GlitchEffectTrigger", "ContortHelper.LightningStrikeTrigger", "ContortHelper.MadelineSpotlightModifierTrigger", "ContortHelper.RandomSoundTrigger", "ContortHelper.ReinstateParametersTrigger", "ContortHelper.RumbleTrigger", "ContortHelper.ScreenWipeModifierTrigger", "ContortHelper.ShakeTrigger", "ContortHelper.SpecificLightningStrikeTrigger").ForEach(x => contortTriggerTypes.Add(x));
        if (contortTriggerTypes.IsNotNullOrEmpty()) {
            AddCheck(entity => contortTriggerTypes.Contains(entity.GetType()));
        }
    }

    private static readonly HashSet<Type> contortTriggerTypes = new();
    private static void HandleOtherMods() {
        // https://maddie480.ovh/celeste/custom-entity-catalog
        // to reduce work, i will not check those mods which are used as dependency by less than 5 mods
        // last major update date: 2023.12.21, there are 426 triggers in the list

        AddTypes("AurorasHelper", "Celeste.Mod.AurorasHelper.ResetMusicTrigger", "Celeste.Mod.AurorasHelper.PlayAudioTrigger", "Celeste.Mod.AurorasHelper.ShowSubtitlesTrigger");
        AddTypes("AvBdayHelper2021", "Celeste.Mod.AvBdayHelper.Code.Triggers.ScreenShakeTrigger");
        AddTypes("CherryHelper", "Celeste.Mod.CherryHelper.AudioPlayTrigger");
        AddTypes("ColoredLights", "ColoredLights.FlashlightColorTrigger");
        AddTypes("CommunalHelper", "Celeste.Mod.CommunalHelper.Triggers.AddVisualToPlayerTrigger", "Celeste.Mod.CommunalHelper.Triggers.CassetteMusicFadeTrigger", "Celeste.Mod.CommunalHelper.Triggers.CloudscapeColorTransitionTrigger", "Celeste.Mod.CommunalHelper.Triggers.CloudscapeLightningConfigurationTrigger", "Celeste.Mod.CommunalHelper.Triggers.MusicParamTrigger", "Celeste.Mod.CommunalHelper.Triggers.SoundAreaTrigger", "Celeste.Mod.CommunalHelper.Triggers.StopLightningControllerTrigger");
        AddTypes("CrystallineHelper", "vitmod.BloomStrengthTrigger", "Celeste.Mod.Code.Entities.RoomNameTrigger");
        AddTypes("CustomPoints", "Celeste.Mod.CustomPoints.PointsTrigger");
        AddTypes("DJMapHelper", "Celeste.Mod.DJMapHelper.Triggers.ChangeSpinnerColorTrigger", "Celeste.Mod.DJMapHelper.Triggers.ColorGradeTrigger");
        AddTypes("FactoryHelper", "FactoryHelper.Triggers.SteamWallColorTrigger");
        AddTypes("FemtoHelper", "ParticleRemoteEmit");
        AddTypes("FlaglinesAndSuch", "FlaglinesAndSuch.FlagLightFade", "FlaglinesAndSuch.MusicIfFlag");
        AddTypes("FrostHelper", "FrostHelper.AnxietyTrigger", "FrostHelper.BloomColorFadeTrigger", "FrostHelper.BloomColorPulseTrigger", "FrostHelper.BloomColorTrigger", "FrostHelper.DoorDisableTrigger", "FrostHelper.LightningColorTrigger", "FrostHelper.RainbowBloomTrigger", "FrostHelper.StylegroundMoveTrigger", "FrostHelper.Triggers.StylegroundBlendStateTrigger", "FrostHelper.Triggers.LightingBaseColorTrigger");
        AddTypes("JungleHelper", "Celeste.Mod.JungleHelper.Triggers.GeckoTutorialTrigger", "Celeste.Mod.JungleHelper.Triggers.UIImageTrigger", "Celeste.Mod.JungleHelper.Triggers.UITextTrigger");
        AddTypes("Long Name Helper by Helen, Helen's Helper, hELPER", "Celeste.Mod.hELPER.ColourChangeTrigger", "Celeste.Mod.hELPER.SpriteReplaceTrigger");
        AddTypes("MaxHelpingHand", "Celeste.Mod.MaxHelpingHand.Triggers.AllBlackholesStrengthTrigger", "Celeste.Mod.MaxHelpingHand.Triggers.FloatFadeTrigger", "Celeste.Mod.MaxHelpingHand.Triggers.ColorGradeFadeTrigger", "Celeste.Mod.MaxHelpingHand.Triggers.GradientDustTrigger", "Celeste.Mod.MaxHelpingHand.Triggers.MadelinePonytailTrigger", "Celeste.Mod.MaxHelpingHand.Triggers.MadelineSilhouetteTrigger", "Celeste.Mod.MaxHelpingHand.Triggers.PersistentMusicFadeTrigger", "Celeste.Mod.MaxHelpingHand.Triggers.RainbowSpinnerColorFadeTrigger", "Celeste.Mod.MaxHelpingHand.Triggers.RainbowSpinnerColorTrigger", "Celeste.Mod.MaxHelpingHand.Triggers.SetBloomBaseTrigger", "Celeste.Mod.MaxHelpingHand.Triggers.SetBloomStrengthTrigger", "Celeste.Mod.MaxHelpingHand.Triggers.SetDarknessAlphaTrigger");
        AddTypes("MoreDasheline", "MoreDasheline.HairColorTrigger");
        AddTypes("Sardine7", "Celeste.Mod.Sardine7.Triggers.AmbienceTrigger");
        AddTypes("ShroomHelper", "Celeste.Mod.ShroomHelper.Triggers.GradualChangeColorGradeTrigger", "Celeste.Mod.ShroomHelper.Triggers.MultilayerMusicFadeTrigger");
        AddTypes("SkinModHelper", "SkinModHelper.SkinSwapTrigger");
        AddTypes("SkinModHelperPlus", "Celeste.Mod.SkinModHelper.EntityReskinTrigger", "Celeste.Mod.SkinModHelper.SkinSwapTrigger");
        AddTypes("VivHelper", "VivHelper.Triggers.ActivateCPP", "VivHelper.Triggers.ConfettiTrigger", "VivHelper.Triggers.FlameLightSwitch", "VivHelper.Triggers.FlameTravelTrigger", "VivHelper.Triggers.FollowerDistanceModifierTrigger", "VivHelper.Triggers.RefillCancelParticleTrigger", "VivHelper.Triggers.SpriteEntityActor");
        AddTypes("XaphanHelper", "Celeste.Mod.XaphanHelper.Triggers.FlagMusicFadeTrigger", "Celeste.Mod.XaphanHelper.Triggers.MultiLightFadeTrigger", "Celeste.Mod.XaphanHelper.Triggers.MultiMusicTrigger");
        AddTypes("YetAnotherHelper", "Celeste.Mod.YetAnotherHelper.Triggers.LightningStrikeTrigger", "Celeste.Mod.YetAnotherHelper.Triggers.RemoveLightSourcesTrigger");

        if (otherModsTypes.IsNotEmpty()) {
            AddCheck(entity => otherModsTypes.Contains(entity.GetType()));
        }

        void AddTypes(string modName, params string[] typeNames) {
            AddTypesImpl(otherModsTypes, modName, typeNames);
        }
    }

    private static HashSet<Type> otherModsTypes = new();

    private static void HandleNonTriggerTrigger() {
        // to be fair it's kind of complement of simplified graphics feature, instead of simplified triggers
        // here we collect those entities which are not hidden by CelesteTAS and look like triggers

        // we will use [Tracked(true)], so no need to add every entity type
        AddTypes("StyleMaskHelper", "Celeste.Mod.StyleMaskHelper.Entities.Mask"); // their colliders are only used to determine if these masks should be used i guess
        AddTypes("StrawberryJam2021", "Celeste.Mod.StrawberryJam2021.StylegroundMasks.Mask");
        AddTypes("Celeste", "Celeste.SpawnFacingTrigger");

        void AddTypes(string modName, params string[] typeNames) {
            foreach (string name in typeNames) {
                if (ModUtils.GetType(modName, name) is { } type) {
                    nonTriggerTypes.Add(type);
                    LevelExtensions.AddToTracker(type, true);
                }
            }
        }
    }

    private static HashSet<Type> nonTriggerTypes = new();

    private static object GetEnum(Type enumType, string value) {
        if (long.TryParse(value.ToString(), out long longValue)) {
            return Enum.ToObject(enumType, longValue);
        }
        else {
            try {
                return Enum.Parse(enumType, value, true);
            }
            catch {
#pragma warning disable CS8603
                return null;
#pragma warning restore CS8603
            }
        }
    }

    private static List<Type> GetTypes(string modName, params string[] typeNames) {
        List<Type> results = new();
        foreach (string name in typeNames) {
            if (ModUtils.GetType(modName, name) is { } type) {
                results.Add(type);
            }
        }
        return results;
    }

    private static void AddTypesImpl(HashSet<Type> set, string modName, params string[] typeNames) {
        foreach (string name in typeNames) {
            if (ModUtils.GetType(modName, name) is { } type) {
                set.Add(type);
            }
        }
    }
}