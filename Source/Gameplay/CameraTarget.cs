using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay;
internal static class CameraTarget {

    private static readonly Vector2 TopLeft2Center = new(160f, 90f);
    internal static Color CameraTargetVectorColor => TasHelperSettings.CameraTargetColor;

    [AddDebugRender]
    private static void PatchEntityListDebugRender() {
        if (TasHelperSettings.UsingCameraTarget) {
            DrawCameraTarget(Spinner.Info.PositionHelper.PreviousCameraPos, Spinner.Info.PositionHelper.CameraPosition, Spinner.Info.PositionHelper.CameraTowards);
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