using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Gameplay;

public static class MovingEntityTrack {


    [Initialize]

    public static void Initialize() {
        typeof(FireBall).GetMethod("Added").HookAfter<FireBall>((fireball) => {
            // Vector2[] nodes = (Vector2[])FireBallNodesGetter.GetValue(fireball);
            // thanks to Krafs.Publicizer, we can directly access vanilla stuff now
            Vector2[] nodes = fireball.nodes;
            if (!CachedNodes.Contains(nodes)) {
                CachedNodes.Add(nodes);
            }
        });

        typeof(TrackSpinner).GetConstructorInfo(typeof(EntityData), typeof(Vector2)).HookAfter<TrackSpinner>((track) => {
            CachedStartEnd.Add(new StartEnd(track.Start, track.End));
        });

        typeof(RotateSpinner).GetConstructorInfo(typeof(EntityData), typeof(Vector2)).HookAfter<RotateSpinner>((spinner) => {
            RotateData data = new RotateData(spinner.center, spinner.length);
            if (!CachedCircle.ContainsKey(data)) {
                CachedCircle[data] = 1;
            }
            else {
                CachedCircle[data]++;
            }
            foreach (Component component in spinner.Components) {
                if (component is StaticMover sm) {
                    Action<Vector2> orig_OnMove = sm.OnMove;
                    sm.OnMove = v => {
                        RotateData oldData = new RotateData(spinner.center, spinner.length);
                        orig_OnMove(v);
                        RotateData newData = new RotateData(spinner.center, spinner.length);
                        if (CachedCircle.TryGetValue(oldData, out int count)) {
                            if (count > 1) {
                                CachedCircle[oldData]--;
                            }
                            else {
                                CachedCircle.Remove(oldData);
                            }
                        }
                        if (!CachedCircle.ContainsKey(newData)) {
                            CachedCircle[newData] = 1;
                        }
                        else {
                            CachedCircle[newData]++;
                        }
                    };
                    break;
                }
            }
        });
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

    public static FieldInfo FireBallNodesGetter = typeof(FireBall).GetField("nodes", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static List<Vector2[]> CachedNodes = new List<Vector2[]>();

    internal static HashSet<StartEnd> CachedStartEnd = new();

    internal static Dictionary<RotateData, int> CachedCircle = new();

    public static Color TrackColor = Color.Yellow * 0.5f;

    [LoadLevel(true)]
    private static void OnLoadLevel() {
        CachedNodes.Clear();
        CachedStartEnd.Clear();
        CachedCircle.Clear();
    }

    [AddDebugRender]
    private static void PatchEntityListDebugRender(EntityList self) {
        if (self.Scene is not Level) {
            return;
        }

        if (TasHelperSettings.UsingFireBallTrack) {
            foreach (Vector2[] nodes in CachedNodes) {
                for (int i = 0; i < nodes.Length - 1; i++) {
                    Draw.Line(nodes[i], nodes[i + 1], TrackColor, 1f);
                    // use Draw.Line(start, end, color, thickness) will add an extra offset, making diagnoal lines really thickness = 1, comparing with Draw.Line(start, end, color, thickness)
                    // however, there is a bit offset away from Draw.Point(...)
                }
            }
        }

        if (TasHelperSettings.UsingRotateSpinnerTrack) {
            foreach (RotateData circle in CachedCircle.Keys) {
                Draw.Circle(circle.center, circle.length, TrackColor, 4);
            }
        }

        if (TasHelperSettings.UsingTrackSpinnerTrack) {
            foreach (StartEnd startEnd in CachedStartEnd) {
                Draw.Line(startEnd.Start, startEnd.End, TrackColor, 1f);
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


