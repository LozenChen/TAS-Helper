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

        SpinnerColliderHelper.Initialize();
    }

    public static void Load() {
    }

    public static void Unload() {
    }


    private static Color DefaultColor => TasSettings.EntityHitboxColor;
    private static Color NotInViewColor = Color.Lime;
    private static Color NeverActivateColor = new Color(0.25f, 1f, 1f);
    private static Color ActivatesEveryFrameColor = new Color(0.8f, 0f, 0f);
    // ActivatesEveryFrame now consists of 2 cases: (a) update collidability every frame (b) keep collidable forever. The latter is mainly used for some custom hazards

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
            DrawVanillaCollider(self.Position, color, self.Collidable, HitboxColor.UnCollidableAlpha);
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
                string key = SpinnerColliderHelper.SpinnerColliderKey(hitboxString, scale);
                if (SpinnerColliderHelper.SpinnerColliderTextures.TryGetValue(key, out SpinnerColliderHelper.SpinnerColliderValue value)) {
                    color *= spinner.Collidable ? 1f : HitboxColor.UnCollidableAlpha;
                    value.Outline.DrawCentered(self.Position, color);
                    value.Inside.DrawCentered(self.Position, color * TasHelperSettings.SpinnerFillerAlpha);
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

    public static void DrawVanillaCollider(Vector2 Position, Color color, bool Collidable, float alpha) {
        color *= Collidable ? 1f : alpha;
        SpinnerColliderHelper.Vanilla.Outline.DrawCentered(Position, color);
        SpinnerColliderHelper.Vanilla.Inside.DrawCentered(Position, color * TasHelperSettings.SpinnerFillerAlpha);
    }

    public static void DrawLoadRangeCollider(Vector2 Position, float Width, float Height, Vector2 CameraPos, bool isLightning) {
        if (isLightning && TasHelperSettings.UsingInViewRange) {
            // only check in view for lightning
            if (SpinnerHelper.InView(Position, Width, Height, CameraPos, true)) {
                // do nothing
            }
            else {
                Monocle.Draw.HollowRect(Position, Width + 1, Height + 1, SpinnerCenterColor);
            }
        }
        else {
            // spinner use in view for visible, and near player for collidable
            // dust use in view for graphics establish, and near player for collidable
            // so we render the center when using load range
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

public static class SpinnerColliderHelper {

    public struct SpinnerColliderValue {
        public MTexture Outline;
        public MTexture Inside;
        public SpinnerColliderValue(MTexture Outline, MTexture Inside) {
            this.Outline = Outline;
            this.Inside = Inside;
        }
    }

    public static string SpinnerColliderKey(string hitboxString, float scale) {
        return hitboxString + "//" + scale.ToString();
    }

    public static string SpinnerColliderKey(string[] hitboxString, float scale) {
        return String.Join("|", hitboxString) + "//" + scale.ToString();
    }

    public static Dictionary<string, SpinnerColliderValue> SpinnerColliderTextures = new();

    public static SpinnerColliderValue Vanilla;

    public static void Initialize() {
        // learn from https://github.com/EverestAPI/Resources/wiki/Adding-Sprites#using-a-spritebank-file

        // it's quite foolish, as it cant spot identical expressions, i have to manually add some after i find it not working properly

        MTexture C6_o = GFX.Game["TASHelper/SpinnerCollider/C600_outline"];
        MTexture C6_i = GFX.Game["TASHelper/SpinnerCollider/C600_inside"];
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6", 1f), new SpinnerColliderValue(C6_o, C6_i));
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0", 1f), new SpinnerColliderValue(C6_o, C6_i));

        MTexture vanilla_o = GFX.Game["TASHelper/SpinnerCollider/vanilla_outline"];
        MTexture vanilla_i = GFX.Game["TASHelper/SpinnerCollider/vanilla_inside"];
        Vanilla = new SpinnerColliderValue(vanilla_o, vanilla_i);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,4;-8,-3", 1f), Vanilla);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,4;-8,*1@-4", 1f), Vanilla);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,*4;-8,*-3", 1f), Vanilla);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0|R:16,4;-8,*1@-4", 1f), Vanilla);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0|R:16,*4;-8,*-3", 1f), Vanilla);

        MTexture reverted_o = GFX.Game["TASHelper/SpinnerCollider/reverted_outline"];
        MTexture reverted_i = GFX.Game["TASHelper/SpinnerCollider/reverted_inside"];
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,4;-8,-1", 1f), new SpinnerColliderValue(reverted_o, reverted_i));
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,*4;-8,*-1", 1f), new SpinnerColliderValue(reverted_o, reverted_i));
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,4;-8,*-1", 1f), new SpinnerColliderValue(reverted_o, reverted_i));
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0|R:16,*4;-8,*-1", 1f), new SpinnerColliderValue(reverted_o, reverted_i));
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0|R:16,4;-8,*-1", 1f), new SpinnerColliderValue(reverted_o, reverted_i));

        MTexture C800_o = GFX.Game["TASHelper/SpinnerCollider/C800_outline"];
        MTexture C800_i = GFX.Game["TASHelper/SpinnerCollider/C800_inside"];
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:8", 1f), new SpinnerColliderValue(C800_o, C800_i));
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:8;0,0", 1f), new SpinnerColliderValue(C800_o, C800_i));
    }
}