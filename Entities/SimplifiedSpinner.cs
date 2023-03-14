using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Entities;
internal static class SimplifiedSpinner {

    // dust will clear sprites immediately
    // spinner will clear sprites after room transition
    public static bool ClearSpinnerSprites => TasHelperSettings.ClearSpinnerSprites;

    public static bool ClearSpinnerSpritesDust() => ClearSpinnerSprites && TasHelperSettings.AlsoClearDust;

    public static bool LevelSpritesCleared = false;
    public static bool LevelSpritesClearedMethod() => LevelSpritesCleared;

    private static bool DebugRendered => HitboxToggle.DrawHitboxes || Engine.Commands.Open || GameplayRenderer.RenderDebug;

    public static void Load() {
        On.Celeste.Level.LoadLevel += TryClearSpinnerSprites;
        On.Monocle.Entity.Render += PatchRender;
        On.Monocle.Entity.DebugRender += PatchDebugRender;

        Type t = typeof(SimplifiedSpinner);
        HookHelper.SkipMethod(t, nameof(ClearSpinnerSpritesDust), "Render", typeof(DustGraphic));
        HookHelper.SkipMethod(t, nameof(ClearSpinnerSpritesDust), "Render", typeof(DustGraphic).GetNestedType("Eyeballs", BindingFlags.NonPublic));
        HookHelper.SkipMethod(t, nameof(LevelSpritesClearedMethod), "CreateSprites", typeof(CrystalStaticSpinner));
        HookHelper.SkipMethod(t, nameof(LevelSpritesClearedMethod), "AddSprite", typeof(CrystalStaticSpinner));
        HookHelper.SkipMethod(t, nameof(LevelSpritesClearedMethod), "Render", typeof(CrystalStaticSpinner).GetNestedType("Border", BindingFlags.NonPublic));
    }

    public static void Unload() {
        On.Celeste.Level.LoadLevel -= TryClearSpinnerSprites;
        On.Monocle.Entity.Render -= PatchRender;
        On.Monocle.Entity.DebugRender -= PatchDebugRender;
    }

    public static void Initialize() {
    }

    private static void TryClearSpinnerSprites(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        LevelSpritesCleared = ClearSpinnerSprites;
        orig(self, playerIntro, isFromLoader);
    }

    //private static void StaticDustSpriteKiller(On.Celeste.DustStaticSpinner.orig_ctor_Vector2_bool_bool orig, DustStaticSpinner self, Vector2 position, bool attachToSolid, bool ignoreSolids) {
    //    orig(self, position, attachToSolid, ignoreSolids);
    //    self.Sprite.Visible = false;
    //}
    // currently i dont want to remove dust graphic for all dust, only remove for static dust
    // however, this hook looks awful

    private static void PatchRender(On.Monocle.Entity.orig_Render orig, Entity self) {
        if (!TasHelperSettings.Enabled || SpinnerHelper.HazardType(self) == null) {
            orig(self);
            return;
        }
        bool NothingToRender = false;
        if (!DebugRendered) {
            if (SpinnerHelper.HazardType(self) == SpinnerHelper.spinner && LevelSpritesCleared) {
                NothingToRender = true;
            }
            else if (SpinnerHelper.HazardType(self) == SpinnerHelper.dust && ClearSpinnerSpritesDust()) {
                NothingToRender = true;
            }
        }
        if (!NothingToRender) {
            orig(self);
            return;
        }
        // unfortunately the DustGraphic that do not come from StaticDust, will also be deleted, it's a bug

        // in this case, we use DebugRender to compensate
        float offset = SpinnerHelper.GetOffset(self).Value;
        Color color = RenderHelper.CycleHitboxColor(self, SpinnerHelper.TimeActive, offset, PlayerHelper.CameraPosition);
        RenderHelper.DrawSpinnerCollider(self.Position, color, self.Collidable, HitboxColor.UnCollidableAlpha, true);
    }


    private static void PatchDebugRender(On.Monocle.Entity.orig_DebugRender orig, Entity self, Camera camera) {
        if (!TasHelperSettings.SpinnerEnabled || SpinnerHelper.HazardType(self) == null) {
            orig(self, camera);
            return;
        }
        float offset = SpinnerHelper.GetOffset(self).Value;

        Color color = RenderHelper.CycleHitboxColor(self, SpinnerHelper.TimeActive, offset, PlayerHelper.CameraPosition);
        // camera.Position is a bit different from CameraPosition, if you use CelesteTAS's center camera
        if (!SpinnerHelper.isLightning(self) && TasHelperSettings.EnableSimplifiedSpinner) {
            RenderHelper.DrawSpinnerCollider(self.Position, color, self.Collidable, HitboxColor.UnCollidableAlpha, true);
        }
        else {
            self.Collider.Render(camera, color * (self.Collidable ? 1f : HitboxColor.UnCollidableAlpha));
        }

        LoadRangeCountDownCameraTarget.DrawLoadRangeColliderCountdown(self);
    }

}