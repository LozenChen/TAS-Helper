using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner.Info;


internal static class HazardTypeHelper {

    private static BitTag IsSpinnerTag;

    private static BitTag IsLightningTag;

    private static BitTag IsDustTag;

    private static int IsSpinnerTagValue;

    private static int IsLightningTagValue;

    private static int IsDustTagValue;

    private static int IsHazardTagValue;

    private static Dictionary<Type, int> HazardTypes_Normal = new();

    private static Dictionary<Type, GetDelegate<Entity, int?>> HazardTypes_Special = new();

    internal static void AddHazardType(Type type, int hazardType) {
        HazardTypes_Normal[type] = hazardType;
    }

    internal static void AddHazardType(Type type, GetDelegate<Entity, int?> hazardTypeGetter) {
        HazardTypes_Special[type] = hazardTypeGetter;
    }

    [Load]
    public static void Load() {
        IL.Monocle.EntityList.UpdateLists += IL_EntityList_UpdateLists;
        On.Celeste.WaveDashPlaybackTutorial.ctor += On_WaveDashPlaybackTutorial_ctor;
    }

    [Unload]
    public static void Unload() {
        IL.Monocle.EntityList.UpdateLists -= IL_EntityList_UpdateLists;
        On.Celeste.WaveDashPlaybackTutorial.ctor -= On_WaveDashPlaybackTutorial_ctor;
    }

    private static void On_WaveDashPlaybackTutorial_ctor(On.Celeste.WaveDashPlaybackTutorial.orig_ctor orig, WaveDashPlaybackTutorial self, string name, Vector2 offset, Vector2 dashDirection0, Vector2 dashDirection1) {
        orig(self, name, offset, dashDirection0, dashDirection1);
        self.tag &= ~IsHazardTagValue;
    }

    [Initialize]
    private static void PrepareTags() {
        TagUtils.SafeAdd("IsSpinner", out IsSpinnerTag);
        TagUtils.SafeAdd("IsLightning", out IsLightningTag);
        TagUtils.SafeAdd("IsDust", out IsDustTag);
        IsSpinnerTagValue = (int)IsSpinnerTag;
        IsLightningTagValue = (int)IsLightningTag;
        IsDustTagValue = (int)IsDustTag;
        IsHazardTagValue = IsSpinnerTagValue | IsLightningTagValue | IsDustTagValue;

        if (ModUtils.GetType("FrostHelper", "FrostHelper.Entities.WallBouncePresentation.WallbouncePlayback") is { } wallbouncePlayBack && wallbouncePlayBack.GetFieldInfo("tag") is { } fieldInfo && wallbouncePlayBack.GetConstructorInfo(new Type[] { typeof(string), typeof(Vector2) }) is { } ctorInfo) {
            ctorInfo.HookAfter<object>(x => fieldInfo.SetValue(x, (int)fieldInfo.GetValue(x) & ~IsHazardTagValue));
        }
    }


    private static void IL_EntityList_UpdateLists(ILContext il) {
        // we assume whether an entity is hazard or not can be determined when it is added to scene (i.e. not when awake)
        ILCursor cursor = new ILCursor(il);
        if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Ldarg_0, ins => ins.MatchCallvirt<EntityList>("get_Scene"), ins => ins.MatchCallvirt<Scene>("get_TagLists"), ins => ins.OpCode == OpCodes.Ldloc_1, ins => ins.MatchCallvirt<TagLists>(nameof(TagLists.EntityAdded)))) { // before the "Scene.TagLists.EntityAdded(entity)" in the toAdd.Count > 0 sentences.
            cursor.MoveAfterLabels();
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate(CheckAndAddTag);
        }
        else {
            throw new Exception($"[TAS Helper] IL Hook SpinnerCalculateHelper.{nameof(IL_EntityList_UpdateLists)} fails.");
        }
    }

    private static void CheckAndAddTag(Entity entity) {
        if (entity is null) {
            return;
        }
        int? value = HazardTypeImpl(entity);
        if (value.HasValue) {
            switch (value.Value) {
                case spinner:
                    entity.AddTag(IsSpinnerTag); break;
                case lightning:
                    entity.AddTag(IsLightningTag); break;
                case dust:
                    entity.AddTag(IsDustTag); break;
                default: break;
            }
        }
    }

    private static int? HazardTypeImpl(Entity self) {
        Type type = self.GetType();
        if (HazardTypes_Normal.TryGetValue(type, out int value)) {
            return value;
        }
        else if (HazardTypes_Special.TryGetValue(type, out GetDelegate<Entity, int?> getter)) {
            return getter(self);
        }
        return null;
        /*
         * NO, if we dont classify it as a hazard, then it may just be a lack of feature
         * but if we classify it as a hazard casually, something bad may happen
         * e.g. SJ2021/SineDustSpinner, it's actually moving!
         * if it's viewed as a hazard, then its hitbox doesn't show for some reason..
        else {
            return self switch {
                // we've checked vanilla types before, but we still need to check here so subclasses of these can be handled correctly when i forget to add some hazard type
                // e.g. AcidHelper, which is a mod contained in Glyph
                CrystalStaticSpinner => spinner,
                DustStaticSpinner => dust,
                Lightning => lightning,
                _ => null
            };
        }
        */
    }

    #region Export Methods
    public static bool IsSpinner(this Entity self) => self.TagCheck(IsSpinnerTagValue);
    public static bool IsLightning(this Entity self) => self.TagCheck(IsLightningTagValue);
    public static bool IsDust(this Entity self) => self.TagCheck(IsDustTagValue);
    public static bool IsHazard(this Entity self) => self.TagCheck(IsHazardTagValue);

    #endregion
}
