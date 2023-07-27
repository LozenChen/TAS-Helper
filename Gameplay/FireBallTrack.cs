using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Gameplay;

public static class FireBallTrack {

    public static void Load() {
        On.Monocle.EntityList.DebugRender += PatchEntityListDebugRender;
        On.Celeste.Level.LoadLevel += OnLoadLevel;
    }

    public static void Unload() {
        On.Monocle.EntityList.DebugRender -= PatchEntityListDebugRender;
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
    }

    public static void Initialize() {
        typeof(FireBall).GetMethod("Added").HookAfter<FireBall>((fireball) => {
            Vector2[] nodes = (Vector2[])FireBallNodesGetter.GetValue(fireball);
            if (!CachedNodes.Contains(nodes)) {
                CachedNodes.Add(nodes);
            }
        });
    }

    public static FieldInfo FireBallNodesGetter = typeof(FireBall).GetField("nodes", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static readonly List<Vector2[]> CachedNodes = new List<Vector2[]>();

    public static Color FireBallTrackColor = Color.Yellow * 0.5f;
    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        CachedNodes.Clear();
        orig(self, playerIntro, isFromLoader);
    }
    private static void PatchEntityListDebugRender(On.Monocle.EntityList.orig_DebugRender orig, EntityList self, Camera camera) {
        orig(self, camera);
        if (!TasHelperSettings.UsingFireBallTrack || self.Scene is not Level level) {
            return;
        }
        foreach (Vector2[] nodes in CachedNodes) {
            for (int i = 0; i < nodes.Length - 1; i++) {
                Draw.Line(nodes[i], nodes[i + 1], FireBallTrackColor);
            }
        }
    }

    /*
     * The KillBox is just those part under the bounce hitbox, unless FireBall happens to have Position.Y an integer
     * btw there is some OoO issue, so i decide not to render it
     * CelesteTAS
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
}