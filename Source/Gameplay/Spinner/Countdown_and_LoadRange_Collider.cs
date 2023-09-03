using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;
internal static class Countdown_and_LoadRange_Collider {
    internal static Color SpinnerCenterColor => TasHelperSettings.LoadRangeColliderColor;

    public static bool NotCountdownBoost => !TasHelperSettings.CountdownBoost || FrameStep || Engine.Scene.Paused;

    private static MTexture starShape;

    [Initialize]
    public static void Initialize() {
        starShape = GFX.Game["TASHelper/SpinnerCenter/spinner_center"];
    }
    public static void Draw(Entity self, SpinnerRenderHelper.SpinnerColorIndex index) {
        if (TasHelperSettings.DoNotRenderWhenFarFromView && SpinnerCalculateHelper.FarFromRange(self, ActualPosition.PlayerPosition, ActualPosition.CameraPosition, 0.25f)) {
            return;
        }
        if (TasHelperSettings.UsingLoadRange) {
            DrawLoadRangeCollider(self.Position, self.Width, self.Height, ActualPosition.CameraPosition, self.isLightning());
        }
        if (TasHelperSettings.UsingCountDown && NotCountdownBoost) {
#pragma warning disable CS8629
            float offset = SpinnerCalculateHelper.GetOffset(self).Value;
#pragma warning restore CS8629
            Vector2 CountdownPos;
            if (self.isLightning()) {
                CountdownPos = self.Center + new Vector2(-1f, -2f);
            }
            else {
                CountdownPos = self.Position + (TasHelperSettings.UsingLoadRange ? new Vector2(-1f, 3f) : new Vector2(-1f, -2f));
            }
            SpinnerRenderHelper.DrawCountdown(CountdownPos, SpinnerCalculateHelper.PredictCountdown(offset, self.isDust()), index);
        }
    }

    public static void DrawLoadRangeCollider(Vector2 Position, float Width, float Height, Vector2 CameraPos, bool isLightning) {
        if (isLightning) {
            // only check in view for lightning
            if (TasHelperSettings.UsingInViewRange && !SpinnerCalculateHelper.InView(Position, Width, Height, CameraPos, true)) {
                Monocle.Draw.HollowRect(Position, Width + 1, Height + 1, SpinnerCenterColor);
            }
        }
        else {
            // spinner use in view for visible, and near player for collidable
            // dust use in view for graphics establish, and near player for collidable
            // so we render the center when using load range
            starShape.Draw(Position, new Vector2(1f, 1f), SpinnerCenterColor);
            /*
            Monocle.Draw.Point(Position, SpinnerCenterColor);
            Monocle.Draw.Point(Position + new Vector2(-1f, -1f), SpinnerCenterColor);
            Monocle.Draw.Point(Position + new Vector2(-1f, 1f), SpinnerCenterColor);
            Monocle.Draw.Point(Position + new Vector2(1f, -1f), SpinnerCenterColor);
            Monocle.Draw.Point(Position + new Vector2(1f, 1f), SpinnerCenterColor);
            */
        }
    }
}