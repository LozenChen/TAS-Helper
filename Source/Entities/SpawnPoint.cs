using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.TASHelper.Entities;

public class SpawnPoint : Entity {

    public static MTexture Maddy;

    [Initialize]
    private static void Initialize() {
        Maddy = GFX.Game["TASHelper/Spawn/sitDown00"];
    }

    [LoadLevel]
    private static void OnLoadLevel(Level self) {
        self.Add(new SpawnPoint(self));
    }

    public List<Vector2> spawns;

    public Level level;

    // TAS.EverestInterop.Hitboxes.UnloadedRoomHitbox will render a red point if CelesteTAS is Debug ver. instead of Release ver.
    // so it's not really an issue
    public SpawnPoint(Level level) {
        Depth = 1;
        this.level = level;
        spawns = level.Session.LevelData.Spawns;
        Collider = new Hitbox(8f, 11f, -4f, -11f);
    }
    public override void Render() {
        if (DebugRendered && TasHelperSettings.UsingSpawnPoint) {
            // we show the actual respawn point, instead of closest spawn point
            // respawn point are set by level transition in general, and sometimes by triggers

            if (level.Session.RespawnPoint is null) {
                return;
            }
            Vector2 RespawnPoint = level.Session.RespawnPoint.Value;
            foreach (Vector2 spawn in spawns) {
                Facings Facing = Facings.Right;
                SpawnFacingTrigger spawnFacingTrigger = CollideFirst<SpawnFacingTrigger>(new Vector2((int)spawn.X, (int)spawn.Y));
                if (spawnFacingTrigger != null) {
                    Facing = spawnFacingTrigger.Facing;
                }
                else if ((int)spawn.X > (float)level.Bounds.Center.X) {
                    Facing = Facings.Left;
                }
                // we assume the intro type is respawn or transition
                Maddy.Draw(spawn - new Vector2(16f, 32f), Vector2.Zero, Color.White * (0.1f * (spawn == RespawnPoint ? TasHelperSettings.CurrentSpawnPointOpacity : TasHelperSettings.OtherSpawnPointOpacity)), 1f, 0f, Facing == Facings.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            }
        }
    }

    // the first room in 6A is a bit tricky, it seems the vanilla somehow make maddy respawn at a point which is not a spawn point?
    // CelesteTAS has a patch FixCh6FirstRoomLoad about this to fix console load command
    // .... ok let's just ignore it

    public override void DebugRender(Camera camera) {
        // do nothing, never render its hitbox
    }
}
