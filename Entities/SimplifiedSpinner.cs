using Celeste.Mod.TASHelper.Utils;
using FrostHelper;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using TAS.EverestInterop.Hitboxes;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.TASHelper.Entities;
internal static class SimplifiedSpinner {

    private static bool DebugRendered => HitboxToggle.DrawHitboxes || Engine.Commands.Open || GameplayRenderer.RenderDebug;

    public static bool SpritesCleared => DebugRendered && TasHelperSettings.ClearSpinnerSprites;

    private static List<FieldInfo> CrysExtraComponentGetter = new();
    public static void Load() {
        On.Monocle.Entity.DebugRender += PatchDebugRender;
        On.Celeste.Level.BeforeRender += OnSceneBeforeRender;
    }

    public static void Unload() {
        On.Monocle.Entity.DebugRender -= PatchDebugRender;
        On.Celeste.Level.BeforeRender -= OnSceneBeforeRender;
    }

    public static void Initialize() {
        CrysExtraComponentGetter.Add(typeof(CrystalStaticSpinner).GetField("border", BindingFlags.NonPublic | BindingFlags.Instance));
        CrysExtraComponentGetter.Add(typeof(CrystalStaticSpinner).GetField("filler", BindingFlags.NonPublic | BindingFlags.Instance));

        if (ModUtils.FrostHelperInstalled) {
            typeof(Level).GetMethod("BeforeRender").IlHook((cursor, _) => {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Level>>(FrostBeforeRender);
            });
        }
    }

    private static void OnSceneBeforeRender(On.Celeste.Level.orig_BeforeRender orig, Level self) {
        foreach (Entity dust in self.Tracker.GetEntities<DustStaticSpinner>()) {
            dust.UpdateComponentVisiblity();
        }
        foreach (Entity spinner in self.Tracker.GetEntities<CrystalStaticSpinner>()) {
            spinner.UpdateComponentVisiblity();
            foreach (FieldInfo getter in CrysExtraComponentGetter) {
                object obj = getter.GetValue(spinner);
                if (obj != null) {
                    obj.SetFieldValue("Visible", !SpritesCleared);
                }
            }
        }
        orig(self);
    }
    private static void FrostBeforeRender(Level self) {
        foreach (Entity customSpinner in self.Tracker.GetEntities<FrostHelper.CustomSpinner>()) {
            customSpinner.UpdateComponentVisiblity();
        }
        foreach (Entity renderer in self.Tracker.GetEntities<FrostHelper.SpinnerConnectorRenderer>()) {
            renderer.Visible = !SpritesCleared;
        }
        foreach (Entity renderer in self.Tracker.GetEntities<FrostHelper.SpinnerBorderRenderer>()) {
            renderer.Visible = !SpritesCleared;
        }
        foreach (Entity renderer in self.Tracker.GetEntities<FrostHelper.SpinnerDecoRenderer>()) {
            renderer.Visible = !SpritesCleared;
        }
    }
    private static void UpdateComponentVisiblity(this Entity self) {
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

        RenderHelper.SpinnerColorIndex index = RenderHelper.CycleHitboxColorIndex(self, SpinnerHelper.TimeActive, offset, PlayerHelper.CameraPosition);
        Color color = RenderHelper.GetSpinnerColor(index);
        // camera.Position is a bit different from CameraPosition, if you use CelesteTAS's center camera
        if (!SpinnerHelper.isLightning(self) && TasHelperSettings.EnableSimplifiedSpinner) {
            RenderHelper.DrawSpinnerCollider(self.Position, color, self.Collidable, HitboxColor.UnCollidableAlpha, true);
        }
        else {
            self.Collider.Render(camera, color * (self.Collidable ? 1f : HitboxColor.UnCollidableAlpha));
        }

        LoadRangeCountDownCameraTarget.DrawLoadRangeColliderCountdown(self, index);
    }

}