using Microsoft.Xna.Framework;
using Monocle;
using TAS.EverestInterop.Hitboxes;
using TAS.Module;

namespace Celeste.Mod.TASHelper.Utils;

internal static class RenderHelper {
    private static Color B = Color.Black;
    private static Color W = Color.White;
    private static Color N = Color.Transparent;
    private static Color c0 = W;
    private static Color c1 = W;
    private static Color c2 = W;
    private static Color c3 = W;
    private static Color c4 = W;
    private static Color c5 = W;
    private static Color c6 = W;
    private static Color c7 = W;
    private static Color c8 = W;
    private static Color c9 = W;
    private static Color[,,] DrawNumber = new Color[,,] { { { B, B, B, B, B }, { B, c0, c0, c0, B }, { B, c0, B, c0, B }, { B, c0, B, c0, B }, { B, c0, B, c0, B }, { B, c0, c0, c0, B }, { B, B, B, B, B } }, { { B, B, B, B, N }, { B, c1, c1, B, N }, { B, B, c1, B, N }, { N, B, c1, B, N }, { B, B, c1, B, B }, { B, c1, c1, c1, B }, { B, B, B, B, B } }, { { B, B, B, B, B }, { B, c2, c2, c2, B }, { B, B, B, c2, B }, { B, c2, c2, c2, B }, { B, c2, B, B, B }, { B, c2, c2, c2, B }, { B, B, B, B, B } }, { { B, B, B, B, B }, { B, c3, c3, c3, B }, { B, B, B, c3, B }, { B, c3, c3, c3, B }, { B, B, B, c3, B }, { B, c3, c3, c3, B }, { B, B, B, B, B } }, { { B, B, B, B, B }, { B, c4, B, c4, B }, { B, c4, B, c4, B }, { B, c4, c4, c4, B }, { B, B, B, c4, B }, { N, N, B, c4, B }, { N, N, B, B, B } }, { { B, B, B, B, B }, { B, c5, c5, c5, B }, { B, c5, B, B, B }, { B, c5, c5, c5, B }, { B, B, B, c5, B }, { B, c5, c5, c5, B }, { B, B, B, B, B } }, { { B, B, B, B, B }, { B, c6, c6, c6, B }, { B, c6, B, B, B }, { B, c6, c6, c6, B }, { B, c6, B, c6, B }, { B, c6, c6, c6, B }, { B, B, B, B, B } }, { { B, B, B, B, B }, { B, c7, c7, c7, B }, { B, B, B, c7, B }, { N, N, B, c7, B }, { N, N, B, c7, B }, { N, N, B, c7, B }, { N, N, B, B, B } }, { { B, B, B, B, B }, { B, c8, c8, c8, B }, { B, c8, B, c8, B }, { B, c8, c8, c8, B }, { B, c8, B, c8, B }, { B, c8, c8, c8, B }, { B, B, B, B, B } }, { { B, B, B, B, B }, { B, c9, c9, c9, B }, { B, c9, B, c9, B }, { B, c9, c9, c9, B }, { B, B, B, c9, B }, { B, c9, c9, c9, B }, { B, B, B, B, B } } };



    private static readonly Vector2 TopLeft2Center = new(160f, 90f);
    private static Color SpinnerCenterColor = Color.Lime;
    private static Color HazardNotInViewColor = Color.Lime;
    private static Color InViewRangeColor = Color.Yellow;
    private static Color NearPlayerRangeColor = Color.Lime;
    private static Color CameraSpeedColor = Color.Goldenrod * 0.4f;

    public static void Initialize() {
    }

    public static Color GetColor(int index) {
        return index switch {
            0 => CelesteTasSettings.Instance.CycleHitboxColor1,
            1 => CelesteTasSettings.Instance.CycleHitboxColor2,
            2 => CelesteTasSettings.Instance.CycleHitboxColor3,
            3 => CelesteTasSettings.Instance.OtherCyclesHitboxColor,
        };
    }
    public static void DrawCountdown(Vector2 Position, int CountdownTimer) {
        if (CountdownTimer > 9) {
            for (int i = 0; i <= 6; i++) {
                for (int j = 0; j <= 4; j++) {
                    Monocle.Draw.Point(Position + new Vector2(j - 4, i), DrawNumber[CountdownTimer / 10, i, j]);
                }
            }
            CountdownTimer %= 10;
        }
        for (int i = 0; i <= 6; i++) {
            for (int j = 0; j <= 4; j++) {
                Monocle.Draw.Point(Position + new Vector2(j, i), DrawNumber[CountdownTimer, i, j]);
            }
        }
    }

    public static void DrawCycleHitboxColor(Entity self, Camera camera, float TimeActive, float offset, Vector2 CameraPosition) {
        Color color;
        if (!TasHelperSettings.ShowCycleHitboxColors) {
            color = CelesteTasSettings.Instance.EntityHitboxColor;
        }
        else if (TasHelperSettings.isUsingInViewRange && !SpinnerHelper.InView(self, CameraPosition) && !(SpinnerHelper.isDust(self))) {
            color = HazardNotInViewColor;
        }
        else {
            int group = SpinnerHelper.CalculateSpinnerGroup(TimeActive, offset);
            color = GetColor(group);
            if (TimeActive >= 524288f && group < 3) {
                color = GetColor(0);
            }
        }
        if (SpinnerHelper.isLightning(self)) {
            self.Collider.Render(camera, color * (self.Collidable ? 1f : HitboxColor.UnCollidableAlpha));
        }
        else {
            if (TasHelperSettings.EnableSimplifiedSpinner) {
                DrawSpinnerCollider(self.Position, color, self.Collidable, HitboxColor.UnCollidableAlpha, true);
            }
            else {
                self.Collider.Render(camera, color * (self.Collidable ? 1f : HitboxColor.UnCollidableAlpha));
            }
        }
    }

    public static void DrawSpinnerCollider(Vector2 Position, Color color, bool Collidable, float alpha, bool Filled) {
        float x = Position.X;
        float y = Position.Y;
        color *= Collidable ? 1f : alpha;
        Monocle.Draw.Rect(x - 4f, y - 6f, 8f, 1f, color);
        Monocle.Draw.Rect(x - 4f, y + 5f, 8f, 1f, color);
        Monocle.Draw.Rect(x - 6f, y - 4f, 1f, 8f, color);
        Monocle.Draw.Rect(x + 5f, y - 4f, 1f, 8f, color);
        Monocle.Draw.Rect(x - 8f, y - 3f, 1f, 4f, color);
        Monocle.Draw.Rect(x + 7f, y - 3f, 1f, 4f, color);
        Monocle.Draw.Point(new(x - 5f, y - 5f), color);
        Monocle.Draw.Point(new(x + 4f, y - 5f), color);
        Monocle.Draw.Point(new(x - 5f, y + 4f), color);
        Monocle.Draw.Point(new(x + 4f, y + 4f), color);
        Monocle.Draw.Point(new(x - 7f, y - 3f), color);
        Monocle.Draw.Point(new(x - 7f, y), color);
        Monocle.Draw.Point(new(x + 6f, y - 3f), color);
        Monocle.Draw.Point(new(x + 6f, y), color);

        if (Filled) {
            color *= TasHelperSettings.SpinnerFillerAlpha;
            Monocle.Draw.Rect(x - 4f, y - 5f, 8f, 10f, color);
            Monocle.Draw.Rect(x - 7f, y - 2f, 1f, 2f, color);
            Monocle.Draw.Rect(x + 6f, y - 2f, 1f, 2f, color);
            Monocle.Draw.Rect(x - 5f, y - 4f, 1f, 8f, color);
            Monocle.Draw.Rect(x + 4f, y - 4f, 1f, 8f, color);
        }
    }

    public static void DrawLoadRangeCollider(Vector2 Position, float Width, float Height, Vector2 CameraPos, bool isLightning) {
        if (isLightning) {
            if (SpinnerHelper.InView(Position, Width, Height, CameraPos, true)) {
                // do nothing
            }
            else {
                Monocle.Draw.HollowRect(Position, Width + 1, Height + 1, SpinnerCenterColor);
            }
        }
        else {
            Monocle.Draw.Point(Position, SpinnerCenterColor);
            Monocle.Draw.Point(Position + new Vector2(-1f, -1f), SpinnerCenterColor);
            Monocle.Draw.Point(Position + new Vector2(-1f, 1f), SpinnerCenterColor);
            Monocle.Draw.Point(Position + new Vector2(1f, -1f), SpinnerCenterColor);
            Monocle.Draw.Point(Position + new Vector2(1f, 1f), SpinnerCenterColor);
        }
    }

    public static void DrawInViewRange(Vector2 CameraPosition) {
        float width = (float)TasHelperSettings.InViewRangeWidth;
        float left = (float)Math.Floor(CameraPosition.X - 16f) + 1f;
        float top = (float)Math.Floor(CameraPosition.Y - 16f) + 1f;
        float right = (float)Math.Ceiling(CameraPosition.X + 320f + 16f) - 1f;
        float bottom = (float)Math.Ceiling(CameraPosition.Y + 180f + 16f) - 1f;
        Monocle.Draw.HollowRect(left, top, right - left + 1, bottom - top + 1, Color.LightBlue * (1f * 0.75f));
        Monocle.Draw.Rect(left, top, right - left + 1, width, InViewRangeColor * TasHelperSettings.RangeAlpha);
        Monocle.Draw.Rect(left, bottom - width, right - left + 1, width + 1, InViewRangeColor * TasHelperSettings.RangeAlpha);
        Monocle.Draw.Rect(left, top + width, width, bottom - top - 2 * width, InViewRangeColor * TasHelperSettings.RangeAlpha);
        Monocle.Draw.Rect(right - width, top + width, width + 1, bottom - top - 2 * width, InViewRangeColor * TasHelperSettings.RangeAlpha);
    }

    public static void DrawNearPlayerRange(Vector2 PlayerPosition, Vector2 PreviousPlayerPosition, int PlayerPositionChanged) {
        float width = (float)TasHelperSettings.NearPlayerRangeWidth;
        Color color = NearPlayerRangeColor;
        float alpha = TasHelperSettings.RangeAlpha;
        Monocle.Draw.HollowRect(PlayerPosition + new Vector2(-127f, -127f), 255f, 255f, color);
        Monocle.Draw.Rect(PlayerPosition + new Vector2(-127f, -127f), 255f, width, color * alpha);
        Monocle.Draw.Rect(PlayerPosition + new Vector2(-127f, 128f - width), 255f, width, color * alpha);
        Monocle.Draw.Rect(PlayerPosition + new Vector2(-127f, -127f + width), width, 255f - 2 * width, color * alpha);
        Monocle.Draw.Rect(PlayerPosition + new Vector2(128f - width, -127f + width), width, 255f - 2 * width, color * alpha);
        if (PlayerPositionChanged > 1) {
            Color colorInverted = Monocle.Calc.Invert(color);
            Monocle.Draw.HollowRect(PreviousPlayerPosition + new Vector2(-127f, -127f), 255f, 255f, colorInverted);
            Monocle.Draw.Rect(PreviousPlayerPosition + new Vector2(-127f, -127f), 255f, width, colorInverted * alpha);
            Monocle.Draw.Rect(PreviousPlayerPosition + new Vector2(-127f, 128f - width), 255f, width, colorInverted * alpha);
            Monocle.Draw.Rect(PreviousPlayerPosition + new Vector2(-127f, -127f + width), width, 255f - 2 * width, colorInverted * alpha);
            Monocle.Draw.Rect(PreviousPlayerPosition + new Vector2(128f - width, -127f + width), width, 255f - 2 * width, colorInverted * alpha);
        }
    }

    public static void DrawCameraTarget(Vector2 PreviousCameraPos, Vector2 CameraPosition, Vector2 CameraTowards) {
        float X1 = (float)Math.Floor(PreviousCameraPos.X + TopLeft2Center.X);
        float Y1 = (float)Math.Floor(PreviousCameraPos.Y + TopLeft2Center.Y);
        float X2 = (float)Math.Floor(CameraPosition.X + TopLeft2Center.X);
        float Y2 = (float)Math.Floor(CameraPosition.Y + TopLeft2Center.Y);
        Monocle.Draw.Line(X1, Y1, CameraTowards.X, Y1, CameraSpeedColor);
        Monocle.Draw.Line(CameraTowards.X, Y1, CameraTowards.X, CameraTowards.Y, CameraSpeedColor);
        Monocle.Draw.Point(new Vector2(X1, Y1), Color.Lime * 0.6f);
        Monocle.Draw.Point(new Vector2(X2, Y2), Color.Lime * 1f);
        Monocle.Draw.Point(CameraTowards, Color.Lime * 0.3f);
    }
}