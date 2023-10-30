using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using TAS.EverestInterop;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;
internal static class SimplifiedLightning {

    [Initialize]
    public static void Initialize() {
        typeof(SimplifiedGraphicsFeature).GetMethod("IsSimplifiedLightning", BindingFlags.NonPublic | BindingFlags.Static).OnHook(DrawInner);
    }

    private static bool DrawInner(Func<bool, Lightning, bool> orig, bool visible, Lightning item) {
        if (TasHelperSettings.EnableSimplifiedLightning) {
            
            bool collidable = SpinnerCalculateHelper.GetCollidable(item);
            bool inView = SpinnerCalculateHelper.InView(item, ActualPosition.CameraPosition);

            ActualCollideHitboxDelegatee.DrawLastFrameHitbox(!TasHelperSettings.ApplyActualCollideHitboxForLightning, item, null, Color.White, collidable , DrawInnerWrapper);
            
            //Draw.SpriteBatch.Draw(GameplayBuffers.Lightning, item.Position + Vector2.One, rectangle, Color.Yellow);

            if (!DebugRendered && visible) {
                Rectangle rectangle = new((int)item.X + 1, (int)item.Y + 1, (int)item.Width, (int)item.Height);
                Draw.HollowRect(rectangle, Color.LightGoldenrodYellow);
            }

            return false;
        }
        return orig(visible, item);
    }

    private static void DrawInnerWrapper(Entity item, Camera _, Color __, bool collidable, bool isNow) {
        bool inView;
        if (isNow) {
            inView = SpinnerCalculateHelper.InView(item, ActualPosition.CameraPosition);
        }
        else {
            inView = SpinnerCalculateHelper.InView(item, ActualPosition.PreviousCameraPos);
        }
        DrawInnerCore(item, collidable, inView);
    }

    private static void DrawInnerCore(Entity item, bool collidable, bool inView) {
        float alpha = collidable ? 0.5f : 0.5f * HitboxColor.UnCollidableAlpha;
        SpinnerRenderHelper.SpinnerColorIndex index = SpinnerRenderHelper.GetSpinnerColorIndex(item, false);
        Color color;
        if (index == SpinnerRenderHelper.SpinnerColorIndex.Default) {
            color = collidable ? Color.Yellow * 0.5f : Color.White * alpha;
        }
        else if (index is SpinnerRenderHelper.SpinnerColorIndex.Group1 or SpinnerRenderHelper.SpinnerColorIndex.Group2 or SpinnerRenderHelper.SpinnerColorIndex.Group3 or SpinnerRenderHelper.SpinnerColorIndex.MoreThan3) {
            if (TasHelperSettings.HighlightLoadUnload && !collidable && inView) {
                color = Color.White * 0.2f;
            }
            else if (TasHelperSettings.HighlightLoadUnload && collidable && !inView) {
                color = Color.Black * 0.9f;
            }
            else if (!collidable) {
                color = Color.Black * 0.2f;
            }
            else {
                if (TasHelperSettings.UsingNotInViewColor && !inView && !SpinnerCalculateHelper.NoPeriodicCheckInViewBehavior(item)) {
                    index = SpinnerRenderHelper.SpinnerColorIndex.NotInView;
                }
                color = SpinnerRenderHelper.GetSpinnerColor(index) * alpha;
            }
        }
        else {
            color = SpinnerRenderHelper.GetSpinnerColor(index) * alpha;
        }

        Rectangle rectangle = new((int)item.X + 1, (int)item.Y + 1, (int)item.Width, (int)item.Height);
        Monocle.Draw.Rect(rectangle, color);
    }

    public static void DrawOutline(Entity self, Camera camera, Color color, bool collidable) {
        if (!collidable) {
            DashedLine.DrawRect(self.Position + Vector2.One, self.Width, self.Height, color * 0.8f);
        }
        else {
            self.Collider.Render(camera, color * (collidable ? 1f : HitboxColor.UnCollidableAlpha));
        }
    }

}