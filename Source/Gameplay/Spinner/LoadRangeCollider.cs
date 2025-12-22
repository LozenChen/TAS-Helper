using Celeste.Mod.TASHelper.Gameplay.Spinner.Info;
using Celeste.Mod.TASHelper.Module.Menu;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;

internal static class LoadRangeCollider {

    public static bool Cached = false;
    internal static Color SpinnerCenterColor => CustomColors.LoadRangeColliderColor;

    internal static MTexture starShape;

    [Initialize]
    public static void Initialize() {
        starShape = GFX.Game["TASHelper/SpinnerCenter/spinner_center"];

        // there's bug report that starShape doesn't get initialized properly, why?
    }

    public struct LightningData(Vector2 position, float width, float height) {
        public Vector2 Position = position;
        public float Width = width;
        public float Height = height;
    }

    public static readonly List<LightningData> lightningDatas = new List<LightningData>();

    public static readonly List<Vector2> starShapePositions = new List<Vector2>();

    [AddDebugRender]
    private static void PatchEntityListDebugRender() {
        Setup();

        // render it after entity list debug render, so they are rendered above those solids
        // and this also ensures it's excuted after LoadRangeCollider.Draw
        foreach (LightningData data in lightningDatas) {
            Draw.HollowRect(data.Position, data.Width, data.Height, SpinnerCenterColor);
        }
        foreach (Vector2 position in starShapePositions) {
            starShape.Draw(position, new Vector2(1f, 1f), SpinnerCenterColor);
        }
        Cached = true;
    }

    private static void Setup() {
        if (Cached || !TasHelperSettings.UsingLoadRangeCollider || Engine.Scene is not Level level) {
            return;
        }
        // only check in view for lightning
        if (TasHelperSettings.UsingInViewRange || TasHelperSettings.LoadRangeColliderMode == Module.TASHelperSettings.LoadRangeColliderModes.Always) {
            lightningDatas.AddRange(level.GetNotInViewLightnings().Select(CreateLightningData));
        }
        // spinner use in view for visible, and near player for collidable
        // dust use in view for graphics establish, and near player for collidable
        // so we render the center when using load range
        starShapePositions.AddRange(level.GetSpinners().Select(PositionHelper.GetInviewCheckPosition));
        starShapePositions.AddRange(level.GetDusts().Select(PositionHelper.GetInviewCheckPosition));

        static LightningData CreateLightningData(Entity self) {
            return new LightningData(PositionHelper.GetInviewCheckPosition(self), self.Width + 1, self.Height + 1);
        }
    }

    [LevelUpdate(before: true)]
    public static void ClearCache() {
        lightningDatas.Clear();
        starShapePositions.Clear();
        Cached = false;
    }
}
