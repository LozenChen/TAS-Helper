using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner.Info;

internal static class InViewHelper {

    public static List<Entity> GetNotInViewLightnings(this Level level) {
        Vector2 CameraPos = PositionHelper.CameraPosition;
        float zoom = PositionHelper.CameraZoom;
        float l = CameraPos.X - 16f;
        float r = CameraPos.X + 320f * zoom + 16f;
        float u = CameraPos.Y - 16f;
        float d = CameraPos.Y + 180f * zoom + 16f;
        return [.. level.GetLightnings().Where(e => e.X + e.Width <= l || e.Y + e.Height <= u || e.X >= r || e.Y >= d)];
    }

    public static bool LightningInView(Entity self, Vector2 CameraPos) {
        float zoom = PositionHelper.CameraZoom;
        return self.X + self.Width > CameraPos.X - 16f && self.Y + self.Height > CameraPos.Y - 16f && self.X < CameraPos.X + 320f * zoom + 16f && self.Y < CameraPos.Y + 180f * zoom + 16f;
    }
    public static bool InView(Entity self, Vector2 CameraPos) {
        float zoom = PositionHelper.CameraZoom;
        if (self.IsLightning()) {
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