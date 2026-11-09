using Celeste.Mod.TASHelper.Gameplay.Spinner.Render;
using Microsoft.Xna.Framework;
using Monocle;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;
internal static class SimplifiedSpinner {

    [Load]
    public static void Load() {
        // Must stay on the OUTER side of CelesteTAS' Entity.DebugRender hooks.
        // CelesteTAS' CycleHitboxColor/HitboxOptimized hooks draw and/or cull hitboxes themselves:
        // CycleHitboxColor.EntityOnDebugRender renders the collider and returns WITHOUT calling orig
        // for CrystalStaticSpinner/DustStaticSpinner/Lightning, so an inner hook of ours never runs.
        // MonoMod v25 matches Before/After only against the other detour's DetourConfig.Id (exact string),
        // so the id is CelesteTAS' mod name "CelesteTAS", NOT its assembly name "CelesteTAS-EverestInterop"
        // (that fallback only exists in Everest's legacy MonoMod layer). The legacy "*" wildcard does not
        // exist in MonoMod v25, so only concrete ids are used here.
        using (DetourContextHelper.ManagedHook("TAS Helper SimplifiedSpinner").AppliedBefore(DetourContextHelper.CelesteTAS).Use()) {
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
             * Nothing to draw for this hazard, and deliberately no orig() call: for hazards we
             * replace CelesteTAS/vanilla hitbox rendering entirely.
             *
             * CelesteTAS.HitboxOptimized.ModDebugRender culls with this instead
             * (Grid colliders and FinalBossBeam are exempt):
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
             * Since we are the outer hook, that cull never runs for hazards; we apply a stricter
             * camera +/- Padding check here instead, using the bounds set by OnDebugRender below.
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