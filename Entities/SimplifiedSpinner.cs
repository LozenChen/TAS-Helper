using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Entities;
internal static class SimplifiedSpinner {

    // spinner and dust will clear sprites after room transition

    public static bool LevelSpritesCleared = false;
    public static bool LevelSpritesClearedMethod() => LevelSpritesCleared;

    private static bool DebugRendered => HitboxToggle.DrawHitboxes || Engine.Commands.Open || GameplayRenderer.RenderDebug;

    public static void Load() {
        On.Celeste.Level.LoadLevel += TryClearSpinnerSprites;
        On.Monocle.Entity.Render += PatchRender;
        On.Monocle.Entity.DebugRender += PatchDebugRender;

        On.Celeste.DustStaticSpinner.ctor_Vector2_bool_bool += StaticDustSpriteKiller;

        Type t = typeof(SimplifiedSpinner);
        HookHelper.SkipMethod(t, nameof(LevelSpritesClearedMethod), "CreateSprites", typeof(CrystalStaticSpinner));
        HookHelper.SkipMethod(t, nameof(LevelSpritesClearedMethod), "AddSprite", typeof(CrystalStaticSpinner));
        HookHelper.SkipMethod(t, nameof(LevelSpritesClearedMethod), "Render", typeof(CrystalStaticSpinner).GetNestedType("Border", BindingFlags.NonPublic));
    }

    public static void Unload() {
        On.Celeste.DustStaticSpinner.ctor_Vector2_bool_bool -= StaticDustSpriteKiller;

        On.Celeste.Level.LoadLevel -= TryClearSpinnerSprites;
        On.Monocle.Entity.Render -= PatchRender;
        On.Monocle.Entity.DebugRender -= PatchDebugRender;
    }

    public static void Initialize() {
    }

    private static void TryClearSpinnerSprites(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        LevelSpritesCleared = TasHelperSettings.ClearSpinnerSprites;
        orig(self, playerIntro, isFromLoader);
    }

    private static void StaticDustSpriteKiller(On.Celeste.DustStaticSpinner.orig_ctor_Vector2_bool_bool orig, DustStaticSpinner self, Vector2 position, bool attachToSolid, bool ignoreSolids) {
        orig(self, position, attachToSolid, ignoreSolids);
        foreach (Component component in self.Components) {
            component.Visible = !LevelSpritesCleared;
        }
    }
    // How DustStaticSpinner (in the following, call Dust) works:
    // DustStaticSpinner has a component DustGraphic called Sprite
    // DustGraphic has 2 parts to render, DustGraphic.Eyeballs and DustGraphic itself
    // Eyeballs will be added to scene
    // Eyeballs will render if Dust.Visible and DustGraphic.Visible (and itself is Visible)
    // DustGraphic itself is a component
    // If called by GameplayRenderer, it will render if Dust and DustGraphic are Visible
    // However, DustGraphic.Added also adds a component DustEdge to Dust, which has a field Action RenderDust = DustGraphic.Render
    // Entity DustEdges.BeforeUpdate will call every DustEdge's RenderDust when Dust and DustEdge are visible
    // So DustGraphic.Render will be called even if DustGraphic itself is invisible

    // So we need to make DustGraphic and DustEdge invisible, instead of just DustGraphic
    // We should not make Dust itself invivible, otherwise Dust.Render and thus our compensation will not be called 

    private static void PatchRender(On.Monocle.Entity.orig_Render orig, Entity self) {
        if (!TasHelperSettings.Enabled || SpinnerHelper.HazardType(self) == null) {
            orig(self);
            return;
        }
        bool HasRender = DebugRendered || !LevelSpritesCleared || SpinnerHelper.HazardType(self) == SpinnerHelper.lightning;
        if (HasRender) {
            orig(self);
            return;
        }

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