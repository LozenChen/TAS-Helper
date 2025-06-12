using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;
internal static class LoadRange {

    internal static Color InViewRangeColor => TasHelperSettings.InViewRangeColor;
    internal static Color NearPlayerRangeColor => TasHelperSettings.NearPlayerRangeColor;

    [AddDebugRender]
    private static void PatchEntityListDebugRender() {
        if (TasHelperSettings.UsingNearPlayerRange) {
            // to see whether it works, teleport to Farewell [a-01] and updash
            // (teleport modifies your actualDepth, otherwise you need to set depth, or just die in this room)
            DrawNearPlayerRange(Info.PositionHelper.PlayerPosition, Info.PositionHelper.PreviousPlayerPosition, Info.PositionHelper.PlayerPositionChangedCount);
        }
        if (TasHelperSettings.UsingInViewRange) {
            // Camera Position can be updated by Player or LookOut
            // ActualPosition.CameraPosition = position on end of frame here
            // there's ooo issue if you are handling Lightning, since Lightning and Player both have Depth 0
            // and ooo issue if you are handling spinner and using LookOut, since they both have Depth -8500
            // if you have bino control storage, then both camera position update can happen
            DrawInViewRange();
        }
    }

    public static void DrawInViewRange() {
        float width = TasHelperSettings.InViewRangeWidth;
        float alpha = TasHelperSettings.RangeAlpha;
        Color color = InViewRangeColor * alpha;

        DrawInViewRangeImpl(Info.PositionHelper.CameraPositionSetLastElement, width, color, borderColor);

        if (Info.PositionHelper.CameraPositionSet.IsNotEmpty()) {
            Color inverted = InViewRangeColor.Invert() * alpha;
            foreach (Vector2 pos in Info.PositionHelper.CameraPositionSet) {
                DrawInViewRangeImpl(pos, width, inverted, borderColorInverted);
            }
        }
    }

    private static Color borderColor = Color.LightBlue * 0.75f;

    private static Color borderColorInverted = borderColor.Invert();

    public static void DrawInViewRangeImpl(Vector2 CameraPosition, float width, Color color, Color border) {
        float left = (float)Math.Floor(CameraPosition.X - 16f) + 1f;
        float top = (float)Math.Floor(CameraPosition.Y - 16f) + 1f;
        float right = (float)Math.Ceiling(CameraPosition.X + 320f + 16f) - 1f;
        float bottom = (float)Math.Ceiling(CameraPosition.Y + 180f + 16f) - 1f;
        Draw.HollowRect(left, top, right - left + 1, bottom - top + 1, border);
        Draw.Rect(left, top, right - left + 1, width, color);
        Draw.Rect(left, bottom - width, right - left + 1, width + 1, color);
        Draw.Rect(left, top + width, width, bottom - top - 2 * width, color);
        Draw.Rect(right - width, top + width, width + 1, bottom - top - 2 * width, color);
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
}