using Microsoft.Xna.Framework;
using Mode = Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity.RenderMode;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class Config {
    public static bool MainEnabled => TasHelperSettings.AutoWatchEnable;

    public static Vector2 HiresFontSize = new Vector2(0.8f); // settings.HiresFontSize / 10f

    public static int HiresFontStroke = 5; // settigns.HiresFontStroke

    public static Mode SwapBlock => TasHelperSettings.SwapBlock;

    public static Mode MoveBlock => TasHelperSettings.MoveBlock;

    public static Mode Refill => TasHelperSettings.Refill;

    public static Mode FallingBlock => TasHelperSettings.FallingBlock;

    public static Mode Booster => TasHelperSettings.Booster;

    public static Mode ZipMover => TasHelperSettings.ZipMover;

    public static Mode FloatySpaceBlock => TasHelperSettings.FloatySpaceBlock;

    public static Mode Glider => TasHelperSettings.Glider;

    public static Mode Cloud => TasHelperSettings.Cloud;

    public static Mode TheoCrystal => TasHelperSettings.TheoCrystal;
}

internal static class Format {

    public static bool Speed_UsePixelPerSecond = true; // instead of PixelPerFrame
}

internal static class TODO {
    public static Mode CrumblePlatform = Mode.Always;

    public static Mode Lookout = Mode.Always;

    public static Mode CornerBoostBlock = Mode.Always;

    public static Mode SwitchGate = Mode.Always; // 0 / n

    public static Mode LockBlock = Mode.Always;

    public static Mode DustTrackSpinner = Mode.Always;

    public static Mode PlayerIntro = Mode.Always;

    public static Mode PlayerWallBoost = Mode.Always;

    public static Mode PlayerStarFlyDuration = Mode.Always;

    public static Mode OshiroBoss = Mode.Always; // engine timerate shown on him

    public static Mode DashBlock = Mode.Always; // change its render / debug render, make it half-transparent? // differs if it's not dashable by player

    public static Mode MoveingPlatform = Mode.Always;

    public static Mode SnowBall = Mode.Always;

    public static Mode TempleGate = Mode.Always; // 0 / n

    public static Mode CrushBlock = Mode.Always; // aka Kevin // btw the 6a tricks looks cool but loses some height when kevin move upwards? is that worth? // differs if it's one-way

    public static Mode Bumper = Mode.Always;

    public static Mode FinalBoss = Mode.Always; // 6a boss, timerate?

    public static Mode FinalBossMovingBlock = Mode.Always;

    public static Mode BounceBlock = Mode.Always;

    public static Mode Puffer = Mode.Always;

    public static Mode LightningBreakerBox = Mode.Always;

    public static Mode NPC = Mode.Always;

    public static Mode Cutscene = Mode.Always;

    public static Mode Triggers = Mode.Always;

}
