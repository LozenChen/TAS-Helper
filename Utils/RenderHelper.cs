using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using TAS.EverestInterop.Hitboxes;
using VivEntities = VivHelper.Entities;

namespace Celeste.Mod.TASHelper.Utils;

internal static class RenderHelper {
    private static MTexture[] numbers;

    private static readonly Vector2 TopLeft2Center = new(160f, 90f);
    private static Color SpinnerCenterColor = Color.Lime;
    private static Color InViewRangeColor = Color.Yellow * 0.8f;
    private static Color NearPlayerRangeColor = Color.Lime * 0.8f;
    private static Color CameraTargetVectorColor = Color.Goldenrod;


    public static void Initialize() {
        // copied from ExtendedVariants.Entities.DashCountIndicator
        MTexture source = GFX.Game["pico8/font"];
        numbers = new MTexture[10];
        int index = 0;
        for (int i = 104; index < 4; i += 4) {
            numbers[index++] = source.GetSubtexture(i, 0, 3, 5);
        }
        for (int i = 0; index < 10; i += 4) {
            numbers[index++] = source.GetSubtexture(i, 6, 3, 5);
        }

        if (ModUtils.VivHelperInstalled) {
            typeof(RenderHelper).GetMethod("DrawSpinnerCollider").IlHook((cursor, _) => {
                Instruction skipViv = cursor.Next;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(DrawVivCollider);
                cursor.Emit(OpCodes.Brfalse, skipViv);
                cursor.Emit(OpCodes.Ret);
            });
        }

        ColliderListHelper.Initialize();
    }

    public static void Load() {
    }

    public static void Unload() {
    }


    private static Color DefaultColor => TasSettings.EntityHitboxColor;
    private static Color NotInViewColor = Color.Lime;
    private static Color NeverActivateColor = new Color(0.25f, 1f, 1f);
    private static Color ActivatesEveryFrameColor = new Color(0.8f, 0f, 0f);

    public enum SpinnerColorIndex { Default, Group1, Group2, Group3, NotInView, MoreThan3, NeverActivate, ActivatesEveryFrame };
    public static Color GetSpinnerColor(SpinnerColorIndex index) {
        return index switch {
            SpinnerColorIndex.Default => DefaultColor,
            SpinnerColorIndex.Group1 => TasSettings.CycleHitboxColor1,
            SpinnerColorIndex.Group2 => TasSettings.CycleHitboxColor2,
            SpinnerColorIndex.Group3 => TasSettings.CycleHitboxColor3,
            SpinnerColorIndex.MoreThan3 => TasSettings.OtherCyclesHitboxColor,
            SpinnerColorIndex.NotInView => NotInViewColor,
            SpinnerColorIndex.NeverActivate => NeverActivateColor,
            SpinnerColorIndex.ActivatesEveryFrame => ActivatesEveryFrameColor,
        };
    }

    public static void DrawCountdown(Vector2 Position, int CountdownTimer, SpinnerColorIndex index) {
        if (TasHelperSettings.usingHiresFont) {
            string str = index switch {
                SpinnerColorIndex.NeverActivate => "oo",
                SpinnerColorIndex.ActivatesEveryFrame => "0",
                _ => CountdownTimer.ToString(),
            };
            HiresLevelRenderer.Add(new OneFrameTextRenderer(str, (Position + new Vector2(1.5f, -0.5f)) * 6f));
            return;
        }


        if (index == SpinnerColorIndex.NeverActivate) {
            numbers[9].DrawOutline(Position);
            return;
        }
        if (index == SpinnerColorIndex.ActivatesEveryFrame) {
            numbers[0].DrawOutline(Position);
            return;
        }
        if (CountdownTimer > 9) {
            numbers[CountdownTimer / 10].DrawOutline(Position + new Vector2(-4, 0));
            CountdownTimer %= 10;
        }
        numbers[CountdownTimer].DrawOutline(Position);
    }

    public static SpinnerColorIndex CycleHitboxColorIndex(Entity self, float TimeActive, float offset, Vector2 CameraPosition) {
        if (TasHelperSettings.UsingNotInViewColor && !SpinnerHelper.InView(self, CameraPosition) && !(SpinnerHelper.isDust(self))) {
            // NotInView Color is in some sense, not a cycle hitbox color, we make it independent
            return SpinnerColorIndex.NotInView;
        }
        if (!TasHelperSettings.ShowCycleHitboxColors) {
            return SpinnerColorIndex.Default;
        }
        if (SpinnerHelper.NoCycle(self)) {
            return SpinnerColorIndex.ActivatesEveryFrame;
        }
        int group = SpinnerHelper.CalculateSpinnerGroup(TimeActive, offset);
        if (TimeActive >= 524288f) {
            return group < 3 ? SpinnerColorIndex.ActivatesEveryFrame : SpinnerColorIndex.NeverActivate;
        }
        return group switch {
            0 => SpinnerColorIndex.Group1,
            1 => SpinnerColorIndex.Group2,
            2 => SpinnerColorIndex.Group3,
            > 2 => SpinnerColorIndex.MoreThan3
        };
    }

    public static void DrawSpinnerCollider(Entity self, Color color) {
        if (OnGrid(self)) {
            DrawVanillaCollider(self.Position, color, self.Collidable, HitboxColor.UnCollidableAlpha, true);
        }
        else {
            DrawComplexSpinnerCollider(self, color);
        }
    }

    public static bool DrawVivCollider(Entity self, Color color) {
        if (self is VivEntities.CustomSpinner spinner) {
            if (OnGrid(self)) {
                string[] hitboxString = SpinnerHelper.VivHitboxStringGetter.GetValue(spinner) as string[];
                float scale = spinner.scale;
                string key = ColliderListHelper.ColliderListKey(hitboxString, scale);
                if (ColliderListHelper.CachedRectangle.TryGetValue(key, out ColliderListHelper.ColliderListValue value)) {
                    int x = (int)Math.Floor(self.Position.X);
                    int y = (int)Math.Floor(self.Position.Y);
                    color *= spinner.Collidable ? 1f : HitboxColor.UnCollidableAlpha;
                    Color insideColor = color * TasHelperSettings.SpinnerFillerAlpha;
                    foreach (Rectangle outline in value.Outline) {
                        Monocle.Draw.Rect(outline.at(x, y), color);
                    }
                    foreach (Rectangle inside in value.Inside) {
                        Monocle.Draw.Rect(inside.at(x, y), insideColor);
                    }
                    return true;
                }
            }
            DrawComplexSpinnerCollider(spinner, color);
            return true;
        }
        return false;
    }
    public static bool OnGrid(Entity self) {
        return self.Position.X == Math.Floor(self.Position.X) && self.Position.Y == Math.Floor(self.Position.Y);
    }

    public static void DrawComplexSpinnerCollider(Entity spinner, Color color) {
        color *= spinner.Collidable ? 1f : HitboxColor.UnCollidableAlpha;
        Collider[] list = (spinner.Collider as ColliderList).colliders;
        foreach (Collider collider in list) {
            if (collider is Hitbox hitbox) {
                Monocle.Draw.HollowRect(hitbox, color);
            }
        }
        foreach (Collider collider in list) {
            if (collider is Circle circle) {
                Monocle.Draw.Circle(circle.AbsolutePosition, circle.Radius, color, 4);
            }
        }
    }

    public static void DrawVanillaCollider(Vector2 Position, Color color, bool Collidable, float alpha, bool Filled) {
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
            Monocle.Draw.Rect(x - 5f, y - 4f, 10f, 8f, color);
            Monocle.Draw.Rect(x - 4f, y - 5f, 8f, 1f, color);
            Monocle.Draw.Rect(x - 4f, y + 4f, 8f, 1f, color);
            Monocle.Draw.Rect(x - 7f, y - 2f, 1f, 2f, color);
            Monocle.Draw.Rect(x + 6f, y - 2f, 1f, 2f, color);
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

    public static void DrawNearPlayerRange(Vector2 PlayerPosition, Vector2 PreviousPlayerPosition, int PlayerPositionChangedCount) {
        float width = (float)TasHelperSettings.NearPlayerRangeWidth;
        Color color = NearPlayerRangeColor;
        float alpha = TasHelperSettings.RangeAlpha;
        Monocle.Draw.HollowRect(PlayerPosition + new Vector2(-127f, -127f), 255f, 255f, color);
        Monocle.Draw.Rect(PlayerPosition + new Vector2(-127f, -127f), 255f, width, color * alpha);
        Monocle.Draw.Rect(PlayerPosition + new Vector2(-127f, 128f - width), 255f, width, color * alpha);
        Monocle.Draw.Rect(PlayerPosition + new Vector2(-127f, -127f + width), width, 255f - 2 * width, color * alpha);
        Monocle.Draw.Rect(PlayerPosition + new Vector2(128f - width, -127f + width), width, 255f - 2 * width, color * alpha);
        if (PlayerPositionChangedCount > 1) {
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
        float X2 = (float)Math.Round(CameraTowards.X);
        float Y2 = (float)Math.Round(CameraTowards.Y);
        float Xc = (float)Math.Floor(CameraPosition.X + TopLeft2Center.X);
        float Yc = (float)Math.Floor(CameraPosition.Y + TopLeft2Center.Y);
        float Xleft = Math.Min(X1, X2);
        float Xright = Math.Max(X1, X2);
        float Yup = Math.Min(Y1, Y2);
        float Ydown = Math.Max(Y1, Y2);
        Color color = CameraTargetVectorColor * (0.1f * TasHelperSettings.CameraTargetLinkOpacity);
        Monocle.Draw.Rect(Xleft + 1, Y1, Xright - Xleft - 1f, 1f, color);
        Monocle.Draw.Rect(X2, Yup + 1f, 1f, Ydown - Yup - 1f, color);
        Monocle.Draw.Point(new Vector2(X2, Y1), color);
        Monocle.Draw.Point(new Vector2(Xc, Yc), Color.Lime * 1f);
        Monocle.Draw.Point(new Vector2(X1, Y1), Color.Lime * 0.6f);
        Monocle.Draw.Point(CameraTowards, Color.Red * 1f);
        if (Ydown - Yup > 6f) {
            int sign = Math.Sign(Y1 - Y2);
            Monocle.Draw.Point(new Vector2(X2 - 1f, Y2 + sign * 2f), color);
            Monocle.Draw.Point(new Vector2(X2 - 1f, Y2 + sign * 3f), color);
            Monocle.Draw.Point(new Vector2(X2 - 2f, Y2 + sign * 4f), color);
            Monocle.Draw.Point(new Vector2(X2 - 2f, Y2 + sign * 5f), color);
            Monocle.Draw.Point(new Vector2(X2 + 1f, Y2 + sign * 2f), color);
            Monocle.Draw.Point(new Vector2(X2 + 1f, Y2 + sign * 3f), color);
            Monocle.Draw.Point(new Vector2(X2 + 2f, Y2 + sign * 4f), color);
            Monocle.Draw.Point(new Vector2(X2 + 2f, Y2 + sign * 5f), color);
        }
    }

}

public static class ColliderListHelper {

    public struct ColliderListValue {
        public List<Rectangle> Outline;
        public List<Rectangle> Inside;
        public ColliderListValue(List<Rectangle> Outline, List<Rectangle> Inside) {
            this.Outline = Outline.GetRange(0, Outline.Count);
            this.Inside = Inside.GetRange(0, Inside.Count);
        }
    }

    public static List<Rectangle> Clone(List<Rectangle> list) {
        return list.GetRange(0, list.Count);
    }

    public static string ColliderListKey(string hitboxString, float scale) {
        return hitboxString + "//" + scale.ToString();
    }

    public static string ColliderListKey(string[] hitboxString, float scale) {
        return String.Join("|", hitboxString) + "//" + scale.ToString();
    }

    public static Rectangle at(this Rectangle rect, int x, int y) {
        return new(rect.X + x, rect.Y + y, rect.Width, rect.Height);
    }

    public static Dictionary<string, ColliderListValue> CachedRectangle = new();
    // i can make it add rectangles dynamically, but there is some bug, so i just add manually for now
    public static void Initialize() {
        List<Rectangle> outlineC6 = new();
        List<Rectangle> insideC6 = new();
        outlineC6.Add(new(-4, -6, 8, 1));
        outlineC6.Add(new(-4, +5, 8, 1));
        outlineC6.Add(new(-6, -4, 1, 8));
        outlineC6.Add(new(+5, -4, 1, 8));
        outlineC6.Add(new(-5, -5, 1, 1));
        outlineC6.Add(new(+4, -5, 1, 1));
        outlineC6.Add(new(-5, +4, 1, 1));
        outlineC6.Add(new(+4, +4, 1, 1));
        insideC6.Add(new(-5, -4, 10, 8));
        insideC6.Add(new(-4, -5, 8, 1));
        insideC6.Add(new(-4, +4, 8, 1));
        CachedRectangle.Add(ColliderListKey("C:6;0,0", 1f), new ColliderListValue(outlineC6, insideC6));

        List<Rectangle> outlineVanilla = Clone(outlineC6);
        List<Rectangle> insideVanilla = Clone(insideC6);
        outlineVanilla.Add(new(-8, -3, 1, 4));
        outlineVanilla.Add(new(+7, -3, 1, 4));
        outlineVanilla.Add(new(-7, -3, 1, 1));
        outlineVanilla.Add(new(-7, 0, 1, 1));
        outlineVanilla.Add(new(+6, -3, 1, 1));
        outlineVanilla.Add(new(+6, 0, 1, 1));
        insideVanilla.Add(new(-7, -2, 1, 2));
        insideVanilla.Add(new(+6, -2, 1, 2));
        CachedRectangle.Add(ColliderListKey("C:6;0,0|R:16,4;-8,*1@-4", 1f), new ColliderListValue(outlineVanilla, insideVanilla));
        CachedRectangle.Add(ColliderListKey("C:6;0,0|R:16,*4;-8,*-3", 1f), new ColliderListValue(outlineVanilla, insideVanilla));

        List<Rectangle> outlineIeverted = Clone(outlineC6);
        List<Rectangle> insideIeverted = Clone(insideC6);
        outlineIeverted.Add(new(-8, -1, 1, 4));
        outlineIeverted.Add(new(+7, -1, 1, 4));
        outlineIeverted.Add(new(-7, -1, 1, 1));
        outlineIeverted.Add(new(-7, 2, 1, 1));
        outlineIeverted.Add(new(+6, -1, 1, 1));
        outlineIeverted.Add(new(+6, 2, 1, 1));
        insideIeverted.Add(new(-7, 0, 1, 2));
        insideIeverted.Add(new(+6, 0, 1, 2));
        CachedRectangle.Add(ColliderListKey("C:6;0,0|R:16,*4;-8,*-1", 1f), new ColliderListValue(outlineIeverted, insideIeverted));
        CachedRectangle.Add(ColliderListKey("C:6;0,0|R:16,4;-8,*-1", 1f), new ColliderListValue(outlineIeverted, insideIeverted));

        List<Rectangle> outlineC8 = new();
        List<Rectangle> insideC8 = new();
        outlineC8.Add(new(-4, -8, 8, 1));
        outlineC8.Add(new(4, -7, 2, 1));
        outlineC8.Add(new(6, -6, 1, 2));
        outlineC8.Add(new(7, -4, 1, 8));
        outlineC8.Add(new(6, 4, 1, 2));
        outlineC8.Add(new(4, 6, 2, 1));
        outlineC8.Add(new(-4, 7, 8, 1));
        outlineC8.Add(new(-6, 6, 2, 1));
        outlineC8.Add(new(-7, 4, 1, 2));
        outlineC8.Add(new(-8, -4, 1, 8));
        outlineC8.Add(new(-7, -6, 1, 2));
        outlineC8.Add(new(-6, -7, 2, 1));
        insideC8.Add(new(-4, -7, 8, 1));
        insideC8.Add(new(-6, -6, 12, 2));
        insideC8.Add(new(-7, -4, 14, 8));
        insideC8.Add(new(-6, 4, 12, 2));
        insideC8.Add(new(-4, 6, 8, 1));
        CachedRectangle.Add(ColliderListKey("C:8;0,0", 1f), new ColliderListValue(outlineC8, insideC8));
    }
}