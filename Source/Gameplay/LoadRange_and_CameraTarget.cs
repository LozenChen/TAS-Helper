using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay;
internal static class LoadRange_and_CameraTarget {

    private static readonly Vector2 TopLeft2Center = new(160f, 90f);
    internal static Color InViewRangeColor => TasHelperSettings.InViewRangeColor;
    internal static Color NearPlayerRangeColor => TasHelperSettings.NearPlayerRangeColor;
    internal static Color CameraTargetVectorColor => TasHelperSettings.CameraTargetColor;


    public static void Load() {
        On.Monocle.EntityList.DebugRender += PatchEntityListDebugRender;
    }

    public static void Unload() {
        On.Monocle.EntityList.DebugRender -= PatchEntityListDebugRender;
    }


    private static void PatchEntityListDebugRender(On.Monocle.EntityList.orig_DebugRender orig, EntityList self, Camera camera) {
        orig(self, camera);
        if (TasHelperSettings.UsingCameraTarget) {
            DrawCameraTarget(ActualPosition.PreviousCameraPos, ActualPosition.CameraPosition, ActualPosition.CameraTowards);
        }
        if (TasHelperSettings.UsingNearPlayerRange) {
            // to see whether it works, teleport to Farewell [a-01] and updash
            // (teleport modifies your actualDepth, otherwise you need to set depth, or just die in this room)
            DrawNearPlayerRange(ActualPosition.PlayerPosition, ActualPosition.PreviousPlayerPosition, ActualPosition.PlayerPositionChangedCount);
        }
        if (TasHelperSettings.UsingInViewRange) {
            DrawInViewRange(ActualPosition.CameraPosition);
        }
    }

    public static void DrawInViewRange(Vector2 CameraPosition) {
        float width = TasHelperSettings.InViewRangeWidth;
        float left = (float)Math.Floor(CameraPosition.X - 16f) + 1f;
        float top = (float)Math.Floor(CameraPosition.Y - 16f) + 1f;
        float right = (float)Math.Ceiling(CameraPosition.X + 320f + 16f) - 1f;
        float bottom = (float)Math.Ceiling(CameraPosition.Y + 180f + 16f) - 1f;
        Draw.HollowRect(left, top, right - left + 1, bottom - top + 1, Color.LightBlue * (1f * 0.75f));
        Draw.Rect(left, top, right - left + 1, width, InViewRangeColor * TasHelperSettings.RangeAlpha);
        Draw.Rect(left, bottom - width, right - left + 1, width + 1, InViewRangeColor * TasHelperSettings.RangeAlpha);
        Draw.Rect(left, top + width, width, bottom - top - 2 * width, InViewRangeColor * TasHelperSettings.RangeAlpha);
        Draw.Rect(right - width, top + width, width + 1, bottom - top - 2 * width, InViewRangeColor * TasHelperSettings.RangeAlpha);
    }

    public static void DrawNearPlayerRange(Vector2 PlayerPosition, Vector2 PreviousPlayerPosition, int PlayerPositionChangedCount) {
        float width = TasHelperSettings.NearPlayerRangeWidth;
        Color color = NearPlayerRangeColor;
        float alpha = TasHelperSettings.RangeAlpha;
        Draw.HollowRect(PlayerPosition + new Vector2(-127f, -127f), 255f, 255f, color);
        Draw.Rect(PlayerPosition + new Vector2(-127f, -127f), 255f, width, color * alpha);
        Draw.Rect(PlayerPosition + new Vector2(-127f, 128f - width), 255f, width, color * alpha);
        Draw.Rect(PlayerPosition + new Vector2(-127f, -127f + width), width, 255f - 2 * width, color * alpha);
        Draw.Rect(PlayerPosition + new Vector2(128f - width, -127f + width), width, 255f - 2 * width, color * alpha);
        if (PlayerPositionChangedCount > 1) {
            Color colorInverted = color.Invert();
            Draw.HollowRect(PreviousPlayerPosition + new Vector2(-127f, -127f), 255f, 255f, colorInverted);
            Draw.Rect(PreviousPlayerPosition + new Vector2(-127f, -127f), 255f, width, colorInverted * alpha);
            Draw.Rect(PreviousPlayerPosition + new Vector2(-127f, 128f - width), 255f, width, colorInverted * alpha);
            Draw.Rect(PreviousPlayerPosition + new Vector2(-127f, -127f + width), width, 255f - 2 * width, colorInverted * alpha);
            Draw.Rect(PreviousPlayerPosition + new Vector2(128f - width, -127f + width), width, 255f - 2 * width, colorInverted * alpha);
        }
    }

    public static void DrawCameraTarget(Vector2 PreviousCameraPos, Vector2 CameraPosition, Vector2 CameraTowards) {
        float X1 = (float)Math.Floor(PreviousCameraPos.X + TopLeft2Center.X);
        float Y1 = (float)Math.Floor(PreviousCameraPos.Y + TopLeft2Center.Y);
        float X2 = (float)Math.Round(CameraTowards.X);
        float Y2 = (float)Math.Round(CameraTowards.Y);
        float Xc = (float)Math.Floor(CameraPosition.X + TopLeft2Center.X);
        float Yc = (float)Math.Floor(CameraPosition.Y + TopLeft2Center.Y);
        float Xleft = Math.Min(X1, X2);
        float Xright = Math.Max(X1, X2);
        float Yup = Math.Min(Y1, Y2);
        float Ydown = Math.Max(Y1, Y2);
        Color color = CameraTargetVectorColor * (0.1f * TasHelperSettings.CameraTargetLinkOpacity);
        Draw.Rect(Xleft + 1, Y1, Xright - Xleft - 1f, 1f, color);
        Draw.Rect(X2, Yup + 1f, 1f, Ydown - Yup - 1f, color);
        Draw.Point(new Vector2(X2, Y1), color);
        Draw.Point(new Vector2(Xc, Yc), Color.Lime * 1f);
        Draw.Point(new Vector2(X1, Y1), Color.Lime * 0.6f);
        Draw.Point(CameraTowards, Color.Red * 1f);
        if (Ydown - Yup > 6f) {
            int sign = Math.Sign(Y1 - Y2);
            Draw.Point(new Vector2(X2 - 1f, Y2 + sign * 2f), color);
            Draw.Point(new Vector2(X2 - 1f, Y2 + sign * 3f), color);
            Draw.Point(new Vector2(X2 - 2f, Y2 + sign * 4f), color);
            Draw.Point(new Vector2(X2 - 2f, Y2 + sign * 5f), color);
            Draw.Point(new Vector2(X2 + 1f, Y2 + sign * 2f), color);
            Draw.Point(new Vector2(X2 + 1f, Y2 + sign * 3f), color);
            Draw.Point(new Vector2(X2 + 2f, Y2 + sign * 4f), color);
            Draw.Point(new Vector2(X2 + 2f, Y2 + sign * 5f), color);
        }
    }
}