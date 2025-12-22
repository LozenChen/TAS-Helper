using Celeste.Mod.TASHelper.Gameplay.Spinner.Info;
using Celeste.Mod.TASHelper.Gameplay.Spinner.Render;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;

internal static class Countdown {

    public static bool LastNotCountdownBoost = !NotCountdownBoost;
    public static bool NotCountdownBoost => !TasHelperSettings.CountdownBoost || FrameStep || Engine.Scene.Paused && !Manager_Running;

    public static bool Cached = false;

    public static void Draw(Entity self, SpinnerRenderHelper.SpinnerColorIndex index, bool collidable) {
        if (Cached) {
            return;
        }
        if (TasHelperSettings.UsingCountDown && NotCountdownBoost) {
            float offset = OffsetHelper.GetOffset(self)!.Value;
            Vector2 CountdownPos;
            if (HazardTypeHelper.IsLightning(self)) {
                CountdownPos = PositionHelper.GetInviewCheckCenter(self) + pos_offset;
            }
            else {
                CountdownPos = PositionHelper.GetInviewCheckPosition(self) + (TasHelperSettings.UsingLoadRange ? pos_offset_down : pos_offset);
            }
            SpinnerRenderHelper.DrawCountdown(
                CountdownPos,
                TimeActiveHelper.PredictCountdown(offset, HazardTypeHelper.IsDust(self), TasHelperSettings.SpinnerCountdownLoad),
                index,
                collidable);
        }
    }

    [LevelUpdate(before: true)]
    public static void ClearCache() => CountdownRenderer.ClearCache();

    private static Vector2 pos_offset = new(-1f, -2f);

    private static Vector2 pos_offset_down = new(-1f, 3f);
}