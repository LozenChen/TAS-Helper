using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Entities;
internal static class SimplifiedSpinner {

    public static bool LevelSpritesCleared = false;

    private static bool DebugRendered => HitboxToggle.DrawHitboxes || Engine.Commands.Open || GameplayRenderer.RenderDebug;

    public static void Load() {
        On.Celeste.Level.LoadLevel += TryClearSpinnerSprites;
        On.Celeste.CrystalStaticSpinner.ctor_Vector2_bool_CrystalColor += SpinnerRenderKiller;
        On.Celeste.CrystalStaticSpinner.Awake += CoreModeListenerKiller;
        On.Monocle.Entity.Render += PatchRender;
        On.Monocle.Entity.DebugRender += PatchDebugRender;
    }

    public static void Unload() {
        On.Celeste.Level.LoadLevel -= TryClearSpinnerSprites;
        On.Celeste.CrystalStaticSpinner.ctor_Vector2_bool_CrystalColor -= SpinnerRenderKiller;
        On.Celeste.CrystalStaticSpinner.Awake -= CoreModeListenerKiller;
        On.Monocle.Entity.Render -= PatchRender;
        On.Monocle.Entity.DebugRender -= PatchDebugRender;
    }

    private static void TryClearSpinnerSprites(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        LevelSpritesCleared = TasHelperSettings.ClearSpinnerSprites;
        orig(self, playerIntro, isFromLoader);
    }

    private static void PatchRender(On.Monocle.Entity.orig_Render orig, Entity self) {
        if (!TasHelperSettings.Enabled || SpinnerHelper.HazardType(self) == null) {
            orig(self);
            return;
        }
        if (!LevelSpritesCleared || DebugRendered || SpinnerHelper.HazardType(self) != SpinnerHelper.spinner) {
            orig(self);
            return;
        }
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


    private static void SpinnerRenderKiller(On.Celeste.CrystalStaticSpinner.orig_ctor_Vector2_bool_CrystalColor orig, CrystalStaticSpinner self, Vector2 position, bool attachToSolid, CrystalColor color) {
        orig(self, position, attachToSolid, color);
        if (LevelSpritesCleared) {
            DynamicData SpinnerData = DynamicData.For(self);
            SpinnerData.Set("expanded", true);
        }
    }

    private static void CoreModeListenerKiller(On.Celeste.CrystalStaticSpinner.orig_Awake orig, CrystalStaticSpinner self, Scene scene) {
        if (LevelSpritesCleared) {
            if (self.Components != null) {
                foreach (Component component in self.Components) {
                    component.EntityAwake();
                }
            }
        }
        else {
            orig(self, scene);
        }
    }

}