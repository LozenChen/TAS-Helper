using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Entities;

public class SpawnPoint : Entity {

    public static MTexture Maddy;
    public static void Load() {
        On.Celeste.Level.LoadLevel += OnLoadLevel;
    }
    public static void Unload() {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
    }
    public static void Initialize() {
        Maddy = GFX.Game["TASHelper/Spawn/sitDown00"];
    }
    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        orig(self, playerIntro, isFromLoader);
        self.Add(new SpawnPoint(self));
    }

    public List<Vector2> spawns;

    public Session session;
    public SpawnPoint(Level level) {
        Depth = 1;
        session = level.Session;
        spawns = session.LevelData.Spawns;
    }
    public override void Render() {
        if (DebugRendered && TasHelperSettings.UsingSpawnPoint) {
            // we show the actual respawn point, instead of closest spawn point
            // respawn point are set by level transition in general, and sometimes by triggers
            Vector2 RespawnPoint = session.RespawnPoint.Value;
            foreach (Vector2 spawn in spawns) {
                Maddy.Draw(spawn - new Vector2(16f,32f), Vector2.Zero,  Color.White * (0.1f * (spawn == RespawnPoint ? TasHelperSettings.CurrentSpawnPointOpacity : TasHelperSettings.OtherSpawnPointOpacity)));
            }
        }
    }

    // the first room in 6A is a bit tricky, it seems the vanilla somehow make maddy respawn at a point which is not a spawn point?
    // CelesteTAS has a patch FixCh6FirstRoomLoad about this to fix console load command
    // .... ok let's just ignore it
}
