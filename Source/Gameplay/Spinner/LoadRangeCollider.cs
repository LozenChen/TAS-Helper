using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;
internal static class LoadRangeCollider {

    public static void Draw(Entity self) {
        if (LoadRangeColliderRenderer.Cached) {
            return;
        }

        if (TasHelperSettings.UsingLoadRangeCollider) {
            DrawLoadRangeCollider(self.Position, self.Width, self.Height, ActualPosition.CameraPosition, self.isLightning());
        }

    }

    public static void DrawLoadRangeCollider(Vector2 Position, float Width, float Height, Vector2 CameraPos, bool isLightning) {
        if (isLightning) {
            // only check in view for lightning
            if ((TasHelperSettings.UsingInViewRange || TasHelperSettings.loadRangeColliderMode == Module.TASHelperSettings.LoadRangeColliderModes.Always) && !SpinnerCalculateHelper.InView(Position, Width, Height, CameraPos, true)) {
                LoadRangeColliderRenderer.lightningDatas.Add(new LoadRangeColliderRenderer.LightningData(Position, Width + 1, Height + 1));
            }
        }
        else {
            // spinner use in view for visible, and near player for collidable
            // dust use in view for graphics establish, and near player for collidable
            // so we render the center when using load range
            LoadRangeColliderRenderer.starShapePositions.Add(Position);
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


internal static class LoadRangeColliderRenderer {

    public static bool Cached = false;
    private static Color SpinnerCenterColor => TasHelperSettings.LoadRangeColliderColor;

    private static MTexture starShape;

    [Load]
    public static void Load() {
        On.Monocle.EntityList.DebugRender += PatchEntityListDebugRender;
        On.Monocle.Scene.BeforeUpdate += OnSceneBeforeUpdate;
    }

    [Unload]
    public static void Unload() {
        On.Monocle.EntityList.DebugRender += PatchEntityListDebugRender;
        On.Monocle.Scene.BeforeUpdate -= OnSceneBeforeUpdate;
    }

    [Initialize]
    public static void Initialize() {
        starShape = GFX.Game["TASHelper/SpinnerCenter/spinner_center"];
    }


    public struct LightningData {
        public Vector2 Position;
        public float Width;
        public float Height;
        public LightningData(Vector2 position, float width, float height) {
            this.Position = position;
            this.Width = width;
            this.Height = height;
        }
    }
    public static readonly List<LightningData> lightningDatas = new List<LightningData>();

    public static readonly List<Vector2> starShapePositions = new List<Vector2>();
    private static void PatchEntityListDebugRender(On.Monocle.EntityList.orig_DebugRender orig, EntityList self, Camera camera) {
        orig(self, camera);
        // render it after entity list debug render, so they are rendered above those solids
        foreach (LightningData data in lightningDatas) {
            Draw.HollowRect(data.Position, data.Width, data.Height, SpinnerCenterColor);
        }
        foreach (Vector2 position in starShapePositions) {
            starShape.Draw(position, new Vector2(1f, 1f), SpinnerCenterColor);
        }
        Cached = true;
    }

    private static void OnSceneBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
        orig(self);
        lightningDatas.Clear();
        starShapePositions.Clear();
        Cached = false;
    }
}
