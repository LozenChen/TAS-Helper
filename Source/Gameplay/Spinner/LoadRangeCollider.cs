using Celeste.Mod.TASHelper.Module.Menu;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;
internal static class LoadRangeCollider {
    public static void Draw(Entity self) {
        if (LoadRangeColliderRenderer.Cached) {
            return;
        }

        if (TasHelperSettings.UsingLoadRangeCollider) {
            if (self.isLightning()) {
                // only check in view for lightning
                if ((TasHelperSettings.UsingInViewRange || TasHelperSettings.LoadRangeColliderMode == Module.TASHelperSettings.LoadRangeColliderModes.Always) && !SpinnerCalculateHelper.LightningInView(self, ActualPosition.CameraPosition)) {
                    LoadRangeColliderRenderer.lightningDatas.Add(new LoadRangeColliderRenderer.LightningData(self.Position, self.Width + 1, self.Height + 1));
                }
            }
            else {
                // spinner use in view for visible, and near player for collidable
                // dust use in view for graphics establish, and near player for collidable
                // so we render the center when using load range
                LoadRangeColliderRenderer.starShapePositions.Add(self.Position);
            }
        }
    }
}


internal static class LoadRangeColliderRenderer {

    public static bool Cached = false;
    internal static Color SpinnerCenterColor => CustomColors.LoadRangeColliderColor;

    internal static MTexture starShape;

    [Load]
    public static void Load() {
        On.Monocle.EntityList.DebugRender += PatchEntityListDebugRender;
        On.Monocle.Scene.BeforeUpdate += OnSceneBeforeUpdate;
    }

    [Unload]
    public static void Unload() {
        On.Monocle.EntityList.DebugRender -= PatchEntityListDebugRender;
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
        ClearCache();
    }

    public static void ClearCache() {
        lightningDatas.Clear();
        starShapePositions.Clear();
        Cached = false;
    }
}
