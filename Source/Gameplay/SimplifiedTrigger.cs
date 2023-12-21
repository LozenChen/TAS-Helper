using Celeste.Mod.TASHelper.Utils;
using MonoMod.Cil;
using Monocle;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;
using Celeste.Mod.Entities;

namespace Celeste.Mod.TASHelper.Gameplay;
public static class SimplifiedTrigger {

    public static bool Enabled = true;

    public static bool HideCameraTriggers = true;

    public static bool HideGoldBerryCollectTrigger = true;

    [Load]
    private static void Load() {
        On.Celeste.Level.LoadLevel += OnLoadLevel;
        using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop" }, ID = "TAS Helper SimplifiedTrigger" }) {
            IL.Monocle.Entity.DebugRender += ModDebugRender;
        }
    }

    [Unload]
    private static void Unload() {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
        IL.Monocle.Entity.DebugRender -= ModDebugRender;
    }

    [Initialize]
    private static void Initialize() {
        HandleVanillaTrigger();
        HandleEverestTrigger();
        HandleCameraTrigger();
        HandleExtendedVariantTrigger();
        HandleOtherMods();
    }

    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        orig(level, playerIntro, isFromLoader);
        TriggerInfoBuilder.Build(level);
    }

    private static void ModDebugRender(ILContext il) {
        ILCursor ilCursor = new(il);
        Instruction start = ilCursor.Next;
        ilCursor.Emit(OpCodes.Ldarg_0)
            .EmitDelegate<Func<Entity, bool>>(IsUnimportantTrigger);
        ilCursor.Emit(OpCodes.Brfalse, start).Emit(OpCodes.Ret);
    }

    public static bool IsUnimportantTrigger(Entity entity) {
        return UnimportantTriggers.Contains(entity) && !TAS.EverestInterop.InfoHUD.InfoWatchEntity.WatchingEntities.Contains(entity);
    }

    internal static HashSet<Entity> UnimportantTriggers = new();

    private static List<Func<Entity, bool>> UnimportantCheckers = new();

    public static readonly List<string> RemainingTriggersList = new();

    public static string RemainingTriggers => "\n" + string.Join("\n", RemainingTriggersList);

    private static void AddCheck(Func<Entity, bool> condition) {
        UnimportantCheckers.Add(condition);
    }

    [Tracked]
    private class TriggerInfoBuilder : Entity {

        private static bool lastInstanceRemoved = false;
        public TriggerInfoBuilder() {
            Tag = Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate;
            lastInstanceRemoved = Engine.Scene.Tracker.GetEntities<TriggerInfoBuilder>().IsEmpty();
            Add(new Coroutine(CreateRemainingTriggerList()));
        }

        public override void Removed(Scene scene) {
            lastInstanceRemoved = true;
            base.Removed(scene);    
        }

        public static void Build(Level level) {
            if (!Enabled) {
                level.Tracker.GetEntities<TriggerInfoBuilder>().ForEach(x => x.RemoveSelf());
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
            while (!lastInstanceRemoved) { // which is the same time when triggers in last room also get removed (e.g. in transition routine)
                yield return null;
            }
            UnimportantTriggers.Clear();
            RemainingTriggersList.Clear();
            foreach (Entity entity in level.Tracker.GetEntities<Trigger>()) {
                bool debugVisible = true;
                foreach (Func<Entity, bool> checker in UnimportantCheckers) {
                    if (checker(entity)) {
                        UnimportantTriggers.Add(entity);
                        debugVisible = false;
                        Logger.Log(LogLevel.Verbose, "TAS Helper", $"Hide Trigger: {entity.GetEntityId()}");
                        break;
                    }
                }
                if (debugVisible) {
                    RemainingTriggersList.Add(GetTriggerInfo(entity));
                }
            }
            Active = false;
        }
    }

    public static string GetTriggerInfo(Entity trigger) {
        // todo: provide more info for e.g. FlagTrigger, CameraTriggers
        return trigger.GetEntityId();
    }

    private static void HandleVanillaTrigger() {
        AddCheck(entity => vanillaTriggers.Contains(entity.GetType()));
        AddCheck(entity => HideGoldBerryCollectTrigger && entity.GetType() == typeof(GoldBerryCollectTrigger));
    }

    private static readonly HashSet<Type> vanillaTriggers = new() { typeof(BirdPathTrigger), typeof(BlackholeStrengthTrigger), typeof(AmbienceParamTrigger), typeof(MoonGlitchBackgroundTrigger), typeof(BloomFadeTrigger), typeof(LightFadeTrigger), typeof(AltMusicTrigger), typeof(MusicTrigger), typeof(MusicFadeTrigger) };

    private static void HandleEverestTrigger() {
        AddCheck(entity => everestTriggers.Contains(entity.GetType()));
    }

    private static readonly HashSet<Type> everestTriggers = new() { typeof(AmbienceTrigger), typeof(AmbienceVolumeTrigger), typeof(CustomBirdTutorialTrigger), typeof(MusicLayerTrigger) };

    private static void HandleCameraTrigger() {
        AddCheck(entity => HideCameraTriggers && cameraTriggers.Contains(entity.GetType()));
    }
    private static readonly HashSet<Type> cameraTriggers = new() { typeof(CameraOffsetTrigger), typeof(CameraTargetTrigger), typeof(CameraAdvanceTargetTrigger), typeof(SmoothCameraOffsetTrigger) };

    private static void HandleExtendedVariantTrigger() {
        // we need to handle AbstractExtendedVariantTrigger<T> and ExtendedVariantTrigger
        // we check its "variantChange" field to determine if it's unimportant

        if (ModUtils.GetType("ExtendedVariantMode", "ExtendedVariants.Module.ExtendedVariantsModule+Variant") is { } variantEnumType && variantEnumType.IsEnum) {
            List<string> ignoreVariantString = new() { "RoomLighting", "RoomBloom", "GlitchEffect", "ColorGrading", "ScreenShakeIntensity", "AnxietyEffect", "BlurLevel", "ZoomLevel", "BackgroundBrightness", "DisableMadelineSpotlight", "ForegroundEffectOpacity", "MadelineIsSilhouette", "DashTrailAllTheTime", "FriendlyBadelineFollower", "MadelineHasPonytail", "MadelineBackpackMode", "BackgroundBlurLevel", "AlwaysInvisible", "DisplaySpeedometer", "DisableKeysSpotlight", "SpinnerColor", "InvisibleMotion", "PlayAsBadeline" };
            extendedVariants = ignoreVariantString.Select(x => GetEnum(variantEnumType, x)).Where(x => x is not null).ToList();

            GetTypes("ExtendedVariantMode", new string[] { 
                "ExtendedVariants.Entities.Legacy.ExtendedVariantTrigger", 
                "ExtendedVariants.Entities.Legacy.ExtendedVariantFadeTrigger",
                "ExtendedVariants.Entities.ForMappers.FloatExtendedVariantFadeTrigger"
            }).ForEach(type => {
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

    private static void HandleOtherMods() {
        if (ModUtils.GetType("SkinModHelper", "SkinModHelper.SkinSwapTrigger") is { } skinSwapTrigger) {
            otherModsTypes.Add(skinSwapTrigger);
        }
        // TODO : Support more mods

        if (otherModsTypes.IsNotEmpty()) {
            AddCheck(entity => otherModsTypes.Contains(entity.GetType()));
        }
    }

    private static HashSet<Type> otherModsTypes = new();

    private static object GetEnum(Type enumType, string value) {
        if (long.TryParse(value.ToString(), out long longValue)) {
            return Enum.ToObject(enumType, longValue);
        }
        else {
            try {
                return Enum.Parse(enumType, value, true);
            }
            catch {
                return null;
            }
        }
    }

    private static List<Type> GetTypes(string modName, string[] typeNames) {
        List<Type> results = new();
        foreach (string name in typeNames) {
            if (ModUtils.GetType(modName, name) is { } type) {
                results.Add(type);
            }
        }
        return results;
    }
}