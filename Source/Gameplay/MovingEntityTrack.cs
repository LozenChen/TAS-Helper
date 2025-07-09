using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay;

public static class MovingEntityTrack {

    [Initialize]

    public static void Initialize() {
        typeof(FireBall).GetMethod("Added")!.HookAfter<FireBall>((fireball) => {
            Vector2[] nodes = fireball.nodes;
            if (!CachedNodes.Contains(nodes)) {
                CachedNodes.Add(nodes);
            }
        });

        typeof(TrackSpinner).GetConstructorInfo(typeof(EntityData), typeof(Vector2)).HookAfter<TrackSpinner>((track) => {
            CachedStartEnd.Add(new StartEnd(track.Start, track.End));
        });

        typeof(RotateSpinner).GetMethod("Update")!.HookAfter<RotateSpinner>(spinner => {
            CachedCircle.Add(new RotateData(spinner.center, spinner.length));
        });
    }

    [SceneOnUpdate]
    private static void ClearCircles(Scene scene) {
        if (scene is Level level && !level.Paused) {
            CachedCircle.Clear();
        }
    }

    internal struct StartEnd {
        public Vector2 Start;
        public Vector2 End;
        public StartEnd(Vector2 Start, Vector2 End) {
            this.Start = Start;
            this.End = End;
        }
    }

    internal struct RotateData {
        public Vector2 center;
        public float length;
        public RotateData(Vector2 center, float length) {
            this.center = center;
            this.length = length;
        }
    }

    internal static List<Vector2[]> CachedNodes = new List<Vector2[]>();

    internal static HashSet<StartEnd> CachedStartEnd = new();

    internal static HashSet<RotateData> CachedCircle = new();

    public static Color TrackColor = Color.Yellow * 0.5f;

    [LoadLevel(true)]
    private static void OnLoadLevel() {
        CachedNodes.Clear();
        CachedStartEnd.Clear();
        CachedCircle.Clear();
        if (HiresLevelRenderer.GetRenderers<MovingEntityTrackRenderer>().IsNullOrEmpty()) {
            HiresLevelRenderer.Add(new MovingEntityTrackRenderer());
        }
    }

    private class MovingEntityTrackRenderer : THRenderer {

        private static readonly Vector2 offset = new Vector2(3f, 3f);

        private const float thickness = 3f;

        private const int circleResolution = 16;
        public override void Render() {
            if (!DebugRendered) {
                return;
            }
            if (TasHelperSettings.UsingFireBallTrack) {
                foreach (Vector2[] nodes in CachedNodes) {
                    for (int i = 0; i < nodes.Length - 1; i++) {
                        Draw.Line(nodes[i] * 6f + offset, nodes[i + 1] * 6f + offset, TrackColor, thickness);
                    }
                }
            }

            if (TasHelperSettings.UsingRotateSpinnerTrack) {
                foreach (RotateData circle in CachedCircle) {
                    Draw.Circle(circle.center * 6f + offset, circle.length * 6f, TrackColor, thickness, circleResolution);
                }
            }

            if (TasHelperSettings.UsingTrackSpinnerTrack) {
                foreach (StartEnd startEnd in CachedStartEnd) {
                    Draw.Line(startEnd.Start * 6f + offset, startEnd.End * 6f + offset, TrackColor, thickness);
                }
            }
        }
    }
}

/*
 * btw when it's in ice mode
 * The KillBox is just those part under the bounce hitbox, unless FireBall happens to have Position.Y an integer
 * btw there is some OoO issue, so i decide not to render it
private static void PatchFireBallDebugRender(Entity entity) {
    if (entity is not FireBall self || !(bool)IceModeGetter.GetValue(self)) {
        return;
    }
    float y = self.Y + 4f - 1f;
    float z = (float)Math.Ceiling(y);
    if (z <= y) {
        z += 1f;
    }
    float top = Math.Max(self.Collider.AbsoluteTop, z);
    Draw.Rect(self.X - 4f, top, 9f, 1f, self.Collidable ? Color.WhiteSmoke : Color.WhiteSmoke * HitboxColor.UnCollidableAlpha);
}
*/


