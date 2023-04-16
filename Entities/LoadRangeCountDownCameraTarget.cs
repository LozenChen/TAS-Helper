using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Entities;
internal static class LoadRangeCountDownCameraTarget {

    public static void Load() {
        On.Monocle.EntityList.DebugRender += PatchEntityListDebugRender;
    }

    public static void Unload() {
        On.Monocle.EntityList.DebugRender -= PatchEntityListDebugRender;
    }

    public static void DrawLoadRangeColliderCountdown(Entity self, RenderHelper.SpinnerColorIndex index) {
        if (TasHelperSettings.DoNotRenderWhenFarFromView && SpinnerHelper.FarFromRange(self, PlayerHelper.PlayerPosition, PlayerHelper.CameraPosition, 0.25f)) {
            return;
        }
        if (TasHelperSettings.UsingLoadRange) {
            RenderHelper.DrawLoadRangeCollider(self.Position, self.Width, self.Height, PlayerHelper.CameraPosition, SpinnerHelper.isLightning(self));
        }
        if (TasHelperSettings.UsingCountDown) {
            float offset = SpinnerHelper.GetOffset(self).Value;
            Vector2 CountdownPos;
            if (SpinnerHelper.isLightning(self)) {
                CountdownPos = self.Center + new Vector2(-1f, -2f);
            }
            else {
                CountdownPos = self.Position + (TasHelperSettings.UsingLoadRange ? new Vector2(-1f, 3f) : new Vector2(-1f, -2f));
            }
            RenderHelper.DrawCountdown(CountdownPos, SpinnerHelper.PredictCountdown(SpinnerHelper.TimeActive, offset, SpinnerHelper.isDust(self)), index);
        }
    }
    private static void PatchEntityListDebugRender(On.Monocle.EntityList.orig_DebugRender orig, EntityList self, Camera camera) {
        orig(self, camera);
        if (TasHelperSettings.UsingCameraTarget) {
            RenderHelper.DrawCameraTarget(PlayerHelper.PreviousCameraPos, PlayerHelper.CameraPosition, PlayerHelper.CameraTowards);
        }
        if (TasHelperSettings.UsingNearPlayerRange) {
            // to see whether it works, teleport to Farewell [a-01] and updash
            // (teleport modifies your actualDepth, otherwise you need to set depth, or just die in this room)
            RenderHelper.DrawNearPlayerRange(PlayerHelper.PlayerPosition, PlayerHelper.PreviousPlayerPosition, PlayerHelper.PlayerPositionChangedCount);
        }
        if (TasHelperSettings.UsingInViewRange) {
            RenderHelper.DrawInViewRange(PlayerHelper.CameraPosition);
        }
    }
}