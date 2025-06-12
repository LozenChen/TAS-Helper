using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner.Info;

internal static class InViewHelper {

    public static bool LightningInView(Entity self, Vector2 CameraPos) {
        float zoom = PositionHelper.CameraZoom;
        return self.X + self.Width > CameraPos.X - 16f && self.Y + self.Height > CameraPos.Y - 16f && self.X < CameraPos.X + 320f * zoom + 16f && self.Y < CameraPos.Y + 180f * zoom + 16f;
    }
    public static bool InView(Entity self, Vector2 CameraPos) {
        float zoom = PositionHelper.CameraZoom;
        if (HazardTypeHelper.IsLightning(self)) {
            // i guess this order of comparison is more efficient
            return self.X + self.Width > CameraPos.X - 16f && self.Y + self.Height > CameraPos.Y - 16f && self.X < CameraPos.X + 320f * zoom + 16f && self.Y < CameraPos.Y + 180f * zoom + 16f;
        }
        else {
            return self.X > CameraPos.X - 16f && self.Y > CameraPos.Y - 16f && self.X < CameraPos.X + 320f * zoom + 16f && self.Y < CameraPos.Y + 180f * zoom + 16f;
        }
    }
    public static bool InView(Vector2 pos, float Width, float Height, Vector2 CameraPos, bool isLightning) {
        float zoom = PositionHelper.CameraZoom;
        if (isLightning) {
            return pos.X < CameraPos.X + 320f * zoom + 16f && pos.Y < CameraPos.Y + 180f * zoom + 16f && pos.Y + Height > CameraPos.Y - 16f && pos.X + Width > CameraPos.X - 16f;
        }
        else {
            return pos.X < CameraPos.X + 320f * zoom + 16f && pos.Y < CameraPos.Y + 180f * zoom + 16f && pos.Y > CameraPos.Y - 16f && pos.X > CameraPos.X - 16f;
        }
    }
}