using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using TAS.EverestInterop.Hitboxes;
using VivEntities = VivHelper.Entities;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;

internal static class SpinnerRenderHelper {
    private static MTexture[] numbers;

    [Initialize]
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
            typeof(SpinnerRenderHelper).GetMethod("DrawSpinnerCollider").IlHook((cursor, _) => {
                Instruction skipViv = cursor.Next;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(DrawVivCollider);
                cursor.Emit(OpCodes.Brfalse, skipViv);
                cursor.Emit(OpCodes.Ret);
            });
        }
    }

    internal static Color DefaultColor => TasSettings.EntityHitboxColor;
    internal static Color NotInViewColor => TasHelperSettings.NotInViewColor;
    internal static Color NeverActivateColor => TasHelperSettings.NeverActivateColor;
    internal static Color ActivateEveryFrameColor => TasHelperSettings.ActivateEveryFrameColor;
    // ActivatesEveryFrame now consists of 2 cases: (a) nocycle mod hazards (b) when time freeze

    public enum SpinnerColorIndex { Default, Group1, Group2, Group3, NotInView, MoreThan3, NeverActivate, FreezeActivateEveryFrame, NoCycle };
    public static Color GetSpinnerColor(SpinnerColorIndex index) {
#pragma warning disable CS8524
        return index switch {
            SpinnerColorIndex.Default => DefaultColor,
            SpinnerColorIndex.Group1 => TasSettings.CycleHitboxColor1,
            SpinnerColorIndex.Group2 => TasSettings.CycleHitboxColor2,
            SpinnerColorIndex.Group3 => TasSettings.CycleHitboxColor3,
            SpinnerColorIndex.MoreThan3 => TasSettings.OtherCyclesHitboxColor,
            SpinnerColorIndex.NotInView => NotInViewColor,
            SpinnerColorIndex.NeverActivate => NeverActivateColor,
            SpinnerColorIndex.FreezeActivateEveryFrame => ActivateEveryFrameColor,
            SpinnerColorIndex.NoCycle => ActivateEveryFrameColor,
        };
#pragma warning restore CS8524
    }

    public static SpinnerColorIndex GetSpinnerColorIndex(Entity Hazard, bool checkInView) {
        // we assume you've checked it's a hazard
#pragma warning disable CS8629
        return checkInView ? CycleHitboxColorIndex(Hazard, SpinnerCalculateHelper.GetOffset(Hazard).Value, ActualPosition.CameraPosition) : CycleHitboxColorIndexNoInView(Hazard, SpinnerCalculateHelper.GetOffset(Hazard).Value);
#pragma warning restore CS8629
    }

    internal const int ID_nocycle = -2;
    internal const int ID_infinity = -1;
    internal const int ID_uncollidable_offset = 163; // related codes is based on this constant, hardcoded, so don't change it
    public static void DrawCountdown(Vector2 Position, int CountdownTimer, SpinnerColorIndex index, bool collidable = true) {
        if (TasHelperSettings.usingHiresFont) {
            // when TimeRate > 1, NeverActivate can activate; when TimeRate < 1, FreezeActivatesEveryFrame can take more than 0 frame.
            // so in these cases i just use CountdownTimer
            // here by TimeRate i actually mean DeltaTime / RawDeltaTime
            // note in 2023 Jan, Everest introduced TimeRateModifier in the calculation of Engine.DeltaTime, so it's no longer DeltaTime = RawDeltaTime * TimeRate * TimeRateB
            int ID;
            if (index == SpinnerColorIndex.NoCycle) {
                ID = ID_nocycle;
            }
            else if (index == SpinnerColorIndex.NeverActivate && Engine.DeltaTime <= Engine.RawDeltaTime) {
                ID = ID_infinity;
            }
            else {
                ID = CountdownTimer;
            }
            if (!collidable) {
                ID += ID_uncollidable_offset;
            }
            CountdownRenderer.Add(ID, (Position + new Vector2(1.5f, -0.5f)) * 6f);
            return;
        }
        else {
            Color color = !TasHelperSettings.DarkenWhenUncollidable || collidable ? Color.White : Color.Gray;
            if (index == SpinnerColorIndex.NoCycle) {
                numbers[0].DrawOutline(Position, Vector2.Zero, color);
                return;
            }
            if (index == SpinnerColorIndex.NeverActivate && Engine.DeltaTime <= Engine.RawDeltaTime) {
                numbers[9].DrawOutline(Position, Vector2.Zero, color);
                return;
            }
            //if (index == SpinnerColorIndex.ActivatesEveryFrame) {
            //    numbers[0].DrawOutline(Position);
            //    return;
            //}
            if (CountdownTimer > 9) {
                numbers[CountdownTimer / 10].DrawOutline(Position + new Vector2(-4, 0), Vector2.Zero, color);
                CountdownTimer %= 10;
            }
            numbers[CountdownTimer].DrawOutline(Position, Vector2.Zero, color);
        }
    }

    private static SpinnerColorIndex CycleHitboxColorIndex(Entity self, float offset, Vector2 CameraPosition) {
        if (TasHelperSettings.UsingNotInViewColor && !SpinnerCalculateHelper.InView(self, CameraPosition) && !SpinnerCalculateHelper.NoPeriodicCheckInViewBehavior(self)) {
            // NotInView Color is in some sense, not a cycle hitbox color, we make it independent
            // Dust needs InView to establish graphics, but that's almost instant (actually every frame can establish up to 25 dust graphics)
            // so InView seems meaningless for Dusts, especially if we only care about those 3f/15f periodic behaviors
            return SpinnerColorIndex.NotInView;
        }
        return CycleHitboxColorIndexNoInView(self, offset);
    }

    private static SpinnerColorIndex CycleHitboxColorIndexNoInView(Entity self, float offset) {
        if (!TasHelperSettings.ShowCycleHitboxColors) {
            return SpinnerColorIndex.Default;
        }
        if (SpinnerCalculateHelper.NoCycle(self)) {
            return SpinnerColorIndex.NoCycle;
        }
        if (TasHelperSettings.UsingFreezeColor && SpinnerCalculateHelper.TimeActive >= 524288f) {
            // we assume the normal state is TimeRate = 1, so we do not detect time freeze by TimeActive + DeltaTime == TimeActive, instead just check >= 524288f (actually works for TimeRate <= 1.8)
            // so freeze colors will not appear too early in some extreme case like slow down of collecting heart
            // we make the color reflects its state at TimeRate = 1, so it will not flash during slowdown like collecting heart
            // unfortunately it will flash if TimeRate > 1, hope this will never happen
            return SpinnerCalculateHelper.OnInterval(SpinnerCalculateHelper.TimeActive, 0.05f, offset, Engine.RawDeltaTime) ? SpinnerColorIndex.FreezeActivateEveryFrame : SpinnerColorIndex.NeverActivate;
        }
        int group = SpinnerCalculateHelper.CalculateSpinnerGroup(offset);
#pragma warning disable CS8509
        return group switch {
            0 => SpinnerColorIndex.Group1,
            1 => SpinnerColorIndex.Group2,
            2 => SpinnerColorIndex.Group3,
            > 2 => SpinnerColorIndex.MoreThan3
        };
#pragma warning restore CS8509
    }

    public static void DrawSpinnerCollider(Entity self, Color color) {
        if (OnGrid(self)) {
            DrawVanillaCollider(self.Position, color, self.Collidable);
        }
        else {
            DrawComplexSpinnerCollider(self, color);
        }
    }

    public static bool DrawVivCollider(Entity self, Color color) {
        if (self is VivEntities.CustomSpinner spinner) {
            if (OnGrid(self)) {
#pragma warning disable CS8600, CS8604
                string[] hitboxString = SpinnerCalculateHelper.VivHitboxStringGetter.GetValue(spinner) as string[];
                float scale = spinner.scale;
                string key = SpinnerColliderHelper.SpinnerColliderKey(hitboxString, scale);
#pragma warning restore CS8600, CS8604
                if (SpinnerColliderHelper.SpinnerColliderTextures.TryGetValue(key, out SpinnerColliderHelper.SpinnerColliderValue value)) {
                    value.DrawOutlineAndInside(self.Position, color, self.Collidable);
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
        if (spinner.Collider is not ColliderList clist) {
            return;
        }
        color *= TasHelperSettings.Ignore_TAS_UnCollidableAlpha || spinner.Collidable ? 1f : HitboxColor.UnCollidableAlpha;
        Collider[] list = clist.colliders;
        foreach (Collider collider in list) {
            if (collider is Hitbox hitbox) {
                Draw.HollowRect(hitbox, color);
            }
        }
        foreach (Collider collider in list) {
            if (collider is Circle circle) {
                Draw.Circle(circle.AbsolutePosition, circle.Radius, color, 4);
            }
        }
    }

    public static void DrawVanillaCollider(Vector2 Position, Color color, bool Collidable) {
        SpinnerColliderHelper.Vanilla.DrawOutlineAndInside(Position, color, Collidable);
    }

    public static void DrawOutlineAndInside(this SpinnerColliderHelper.SpinnerColliderValue value, Vector2 Position, Color color, bool Collidable) {
        float alpha = TasHelperSettings.Ignore_TAS_UnCollidableAlpha || Collidable ? 1f : HitboxColor.UnCollidableAlpha;
        float inner_mult = Collidable ? TasHelperSettings.SpinnerFillerAlpha_Collidable : TasHelperSettings.SpinnerFillerAlpha_Uncollidable;
        if (Collidable || !TasHelperSettings.SimplifiedSpinnerDashedBorder) {
            value.Outline.DrawCentered(Position, color.SetAlpha(alpha));
        }
        else {
            value.Outline_Dashed1.DrawCentered(Position, color.SetAlpha(alpha));
            value.Outline_Dashed2.DrawCentered(Position, color.SetAlpha(alpha) * 0.3f);
        }
        value.Inside.DrawCentered(Position, Color.Lerp(color, Color.Black, 0.6f).SetAlpha(alpha * inner_mult));
    }

}
