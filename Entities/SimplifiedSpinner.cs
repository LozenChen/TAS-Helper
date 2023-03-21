using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using TAS.EverestInterop.Hitboxes;
using VivHelper.Entities.Spinner2;

namespace Celeste.Mod.TASHelper.Entities;
internal static class SimplifiedSpinner {

    private static bool DebugRendered => HitboxToggle.DrawHitboxes || Engine.Commands.Open || GameplayRenderer.RenderDebug;

    public static bool SpritesCleared => DebugRendered && TasHelperSettings.ClearSpinnerSprites;

    private static readonly FieldInfo CrysBorderGetter = typeof(CrystalStaticSpinner).GetField("border", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo CrysFillerGetter = typeof(CrystalStaticSpinner).GetField("filler", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly List<FieldInfo> CrysExtraComponentGetter = new();
    public static void Load() {
        On.Monocle.Entity.DebugRender += PatchDebugRender;
        On.Monocle.Scene.BeforeRender += UpdateHazardSpritesVisibility;
    }

    public static void Unload() {
        On.Monocle.Entity.DebugRender -= PatchDebugRender;
        On.Monocle.Scene.BeforeRender -= UpdateHazardSpritesVisibility;
    }

    public static void Initialize() {
        CrysExtraComponentGetter.Add(CrysBorderGetter);
        CrysExtraComponentGetter.Add(CrysFillerGetter);
    }

    private static void UpdateHazardSpritesVisibility(On.Monocle.Scene.orig_BeforeRender orig, Scene self) {
        foreach (Entity entity in self.Entities) {
            if (entity is DustStaticSpinner dust) {
                UpdateComponentVisiblity(dust);
            }
            else if (entity is CrystalStaticSpinner spinner) {
                UpdateComponentVisiblity(spinner);
                foreach (FieldInfo getter in CrysExtraComponentGetter) {
                    object obj = getter.GetValue(spinner);
                    if (obj != null) {
                        obj.SetFieldValue("Visible", !SpritesCleared);
                    }
                }
            }
        }
        orig(self);
    }
    private static void UpdateComponentVisiblity(Entity self) {
        foreach (Component component in self.Components) {
            component.Visible = !SpritesCleared;
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