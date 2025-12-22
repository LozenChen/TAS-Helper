using Celeste.Mod.TASHelper.Gameplay.Spinner.Render;
using Microsoft.Xna.Framework;
using Monocle;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;
internal static class SimplifiedSpinner {

    [Load]
    public static void Load() {
        // hook after CelesteTAS.CycleHitboxColor's hook
        using (DetourContextHelper.Use(After: ["*", "CelesteTAS-EverestInterop"], ID: "TAS Helper SimplifiedSpinner")) {
            // CelesteTAS.HitboxOptimized also hooks this, and it'll early return if entity is not in the camera
            // so we need to be after HitboxOptimized hook, which already uses After = {"*"}, so we need even more configs
            On.Monocle.Entity.DebugRender += PatchDebugRender;
        }
    }

    [Unload]
    public static void Unload() {
        On.Monocle.Entity.DebugRender -= PatchDebugRender;
    }

    private static void PatchDebugRender(On.Monocle.Entity.orig_DebugRender orig, Entity self, Camera camera) {
        if (!TasHelperSettings.Enabled || !Info.HazardTypeHelper.IsHazard(self)) {
            orig(self, camera);
            return;
        }

        SpinnerRenderHelper.SpinnerColorIndex index = SpinnerRenderHelper.GetSpinnerColorIndex(self, true);
        Color color = SpinnerRenderHelper.GetSpinnerColor(index);
        bool collidable = Info.CollidableHelper.GetCollidable(self);

        if (self.Right < Left || self.Left > Right || self.Top > Bottom || self.Bottom < Top) {
            /*
             * In CelesteTAS's HitboxOptimized.ModDebugRender we already have:
             * 
                // Do not draw hitboxes of entities outside the camera
                if (self.Collider is not Grid && self is not FinalBossBeam) {
                    int width = camera.Viewport.Width;
                    int height = camera.Viewport.Height;
                    Rectangle bounds = new((int)camera.Left - width / 2, (int)camera.Top - height / 2, width * 2, height * 2);
                    if (self.Right < bounds.Left || self.Left > bounds.Right || self.Top > bounds.Bottom ||
                        self.Bottom < bounds.Top) {
                        return;
                    }
                }
             *
             * We apply a stricter check, coz we know it's ok for hazard
             */
        }
        else {
            if (TasHelperSettings.EnableSimplifiedSpinner && !Info.HazardTypeHelper.IsLightning(self)) {
                ActualCollideHitboxDelegatee.DrawLastFrameHitbox(
                    !TasHelperSettings.ApplyActualCollideHitboxForSpinner,
                    self, camera, color, collidable,
                    Render.SpinnerRenderHelper.DrawSpinnerCollider
                );
            }
            else if (TasHelperSettings.EnableSimplifiedLightning && Info.HazardTypeHelper.IsLightning(self)) {
                ActualCollideHitboxDelegatee.DrawLastFrameHitbox(
                    !TasHelperSettings.ApplyActualCollideHitboxForLightning,
                    self, camera, color, collidable,
                    Render.LightningRenderHelper.DrawOutline
                );
            }
            else {
                self.Collider.Render(camera, color * (collidable ? 1f : HitboxColor.UnCollidableAlpha));
            }
        }
        Countdown.Draw(self, index, collidable);
    }

    [AddDebugRender(before: true)]
    private static void OnDebugRender(EntityList _, Camera camera) {
        Left = camera.Left - Padding;
        Right = camera.Right + Padding;
        Top = camera.Top - Padding;
        Bottom = camera.Bottom + Padding;
    }

    private static float Left;

    private static float Right;

    private static float Top;

    private static float Bottom;

    private const float Padding = 10f;
}