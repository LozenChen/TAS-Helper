using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using TAS.EverestInterop.Hitboxes;
using VivEntities = VivHelper.Entities;
using Mono.Cecil.Cil;

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
    }

    public static void Load() {
    }

    public static void Unload() {
    }

    private static Color DefaultColor => TasSettings.EntityHitboxColor;
    private static Color NotInViewColor = Color.Lime;
    private static Color NeverActivateColor = new Color(0.25f, 1f, 1f);
    private static Color ActivatesEveryFrameColor = new Color(0.8f, 0f, 0f);

    public enum SpinnerColorIndex { Default, Group1, Group2, Group3, NotInView, MoreThan3, NeverActivate, ActivatesEveryFrame};
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
        if (!TasHelperSettings.ShowCycleHitboxColors) {
            return SpinnerColorIndex.Default;
        }
        if (TasHelperSettings.isUsingInViewRange && !SpinnerHelper.InView(self, CameraPosition) && !(SpinnerHelper.isDust(self))) {
            return SpinnerColorIndex.NotInView;
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
            >2 => SpinnerColorIndex.MoreThan3
        } ;
    }

    public static void DrawSpinnerCollider(Entity self, Color color) {
        DrawVanillaCollider(self.Position, color, self.Collidable, HitboxColor.UnCollidableAlpha, true);
    }

    public static bool DrawVivCollider(Entity self, Color color) {
        if (self is VivEntities.CustomSpinner spinner) {
            color *= spinner.Collidable ? 1f : HitboxColor.UnCollidableAlpha;
            Color innerColor = color * TasHelperSettings.SpinnerFillerAlpha;
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
            return true;
        }
        return false;
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
