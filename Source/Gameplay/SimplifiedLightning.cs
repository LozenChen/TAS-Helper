using Celeste.Mod.TASHelper.Gameplay.Spinner;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using TAS.EverestInterop;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Gameplay;
internal static class SimplifiedLightning {


    [Initialize]
    public static void Initialize() {
        typeof(SimplifiedGraphicsFeature).GetMethod("IsSimplifiedLightning", BindingFlags.NonPublic | BindingFlags.Static).OnHook(SimplifiedLightningEnhance);
    }

    private static bool SimplifiedLightningEnhance(Func<bool, Lightning, bool> orig, bool visible, Lightning item) {
        if (TasHelperSettings.EnableSimplifiedLightning) {
            Rectangle rectangle = new((int)item.X + 1, (int)item.Y + 1, (int)item.Width, (int)item.Height);

            float alpha = item.Collidable ? 0.5f : 0.5f * HitboxColor.UnCollidableAlpha;
            bool inView = SpinnerCalculateHelper.InView(item, ActualPosition.CameraPosition);
            SpinnerRenderHelper.SpinnerColorIndex index = SpinnerRenderHelper.GetSpinnerColorIndex(item, false);
            Color color;
            if (index == SpinnerRenderHelper.SpinnerColorIndex.Default) {
                color = item.Collidable ? Color.Yellow * 0.5f : Color.White * alpha;
            }else if (index is SpinnerRenderHelper.SpinnerColorIndex.Group1 or SpinnerRenderHelper.SpinnerColorIndex.Group2 or SpinnerRenderHelper.SpinnerColorIndex.Group3 or SpinnerRenderHelper.SpinnerColorIndex.MoreThan3) {
                if (TasHelperSettings.HighlightLoadUnload && !item.Collidable && inView) {
                    color = Color.White * 0.2f;
                } 
                else if (TasHelperSettings.HighlightLoadUnload && item.Collidable && !inView) {
                    color = Color.Black * 0.9f;
                }
                else if (!item.Collidable) {
                    color = Color.Black * 0.2f;
                }
                else {
                    if (TasHelperSettings.UsingNotInViewColor && !inView && !SpinnerCalculateHelper.NoPeriodicCheckInViewBehavior(item)) {
                        index = SpinnerRenderHelper.SpinnerColorIndex.NotInView;
                    }
                    color = SpinnerRenderHelper.GetSpinnerColor(index) * alpha;
                }
            } else {
                color = SpinnerRenderHelper.GetSpinnerColor(index) * alpha;
            }

            Monocle.Draw.Rect(rectangle, color);
            //Draw.SpriteBatch.Draw(GameplayBuffers.Lightning, item.Position + Vector2.One, rectangle, Color.Yellow);

            if (visible) {
                Draw.HollowRect(rectangle, Color.LightGoldenrodYellow);
            }

            return false;
        }
        return orig(visible, item);
    }
}