using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;
internal static class SimplifiedLightning {

    [Initialize]
    public static void Initialize() {
        using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop" }, ID = "TAS Helper SimplifiedLightning" }) {
            IL.Celeste.LightningRenderer.Render += LightningRenderer_Render;
        }
    }

    [Unload]
    public static void Unload() {
        IL.Celeste.LightningRenderer.Render -= LightningRenderer_Render;
    }


    private static void LightningRenderer_Render(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        bool success = false;
        if (cursor.TryGotoNext(MoveType.AfterLabel, ins => ins.OpCode == OpCodes.Ldloc_2, ins => ins.MatchLdfld<Entity>(nameof(Entity.Visible)))) {
            int index = cursor.Index;
            // CelesteTAS simplified graphics inserts some codes here
            if (cursor.TryGotoNext(MoveType.Before, ins => ins.OpCode == OpCodes.Brfalse_S)) {
                ILLabel label = (ILLabel)cursor.Next.Operand;
                cursor.Goto(index, MoveType.AfterLabel);
                cursor.Emit(OpCodes.Ldloc_2);
                cursor.EmitDelegate(DrawInner);
                cursor.Emit(OpCodes.Brfalse, label.Target);
                success = true;
            }
        }

        if (!success) {
            throw new Exception($"[TASHelper] {nameof(SimplifiedLightning)} fail to initialize.");
        }


        // should be
        // foreach (Lightning item in list)
        // TASHelper: ldloc2
        // TASHelper: drawinner
        // TASHelper: brfalse, jump to the start of foreach
        // Vanilla: ldloc2
        // Vanilla: ldfld Entity.Visible
        // CelesteTAS: ldloc2
        // CelesteTAS: emit delegate SimplifyLightning, which is a Func<bool, Lightning, bool>
        // Vanilla: brfalse, jump to the start of foreach
    }

    private static bool DrawInner(Lightning item) {
        if (TasHelperSettings.EnableSimplifiedLightning && item.IsHazard()) {
            // why we still check hazard type here: in case in future we decide that some perticular subclass of lightning is not a hazard
            // note in the following codes we need to assume it's indeed a hazard (which is the bugfix of v1.8.15)

            bool collidable = SpinnerCalculateHelper.GetCollidable(item);
            bool inView = SpinnerCalculateHelper.InView(item, ActualPosition.CameraPosition);
#pragma warning disable CS8625
            ActualCollideHitboxDelegatee.DrawLastFrameHitbox(!TasHelperSettings.ApplyActualCollideHitboxForLightning, item, null, Color.White, collidable, DrawInnerWrapper);
#pragma warning restore CS8625
            //Draw.SpriteBatch.Draw(GameplayBuffers.Lightning, item.Position + Vector2.One, rectangle, Color.Yellow);

            if (!DebugRendered && item.Visible) {
                Rectangle rectangle = new((int)item.X + 1, (int)item.Y + 1, (int)item.Width, (int)item.Height);
                Draw.HollowRect(rectangle, Color.LightGoldenrodYellow);
            }

            return false;
        }
        return true;
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