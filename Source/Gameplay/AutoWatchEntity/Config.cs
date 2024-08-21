﻿using Microsoft.Xna.Framework;
using Mode = Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity.RenderMode;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class Config {
    public static bool MainEnabled => TasHelperSettings.AutoWatchEnable;

    public static Vector2 HiresFontSize = new Vector2(0.8f); // settings.HiresFontSize / 10f

    public static float HiresFontStroke = 5 * 0.4f; // settigns.HiresFontStroke

    public static Mode SwapBlock => TasHelperSettings.AutoWatch_SwapBlock;

    public static Mode MoveBlock => TasHelperSettings.AutoWatch_MoveBlock;

    public static Mode Refill => TasHelperSettings.AutoWatch_Refill;

    public static Mode FallingBlock => TasHelperSettings.AutoWatch_FallingBlock;

    public static Mode Booster => TasHelperSettings.AutoWatch_Booster;

    public static Mode ZipMover => TasHelperSettings.AutoWatch_ZipMover;

    public static Mode FloatySpaceBlock => TasHelperSettings.AutoWatch_MoonBlock;

    public static Mode Glider => TasHelperSettings.AutoWatch_Jelly;

    public static Mode Cloud => TasHelperSettings.AutoWatch_Cloud;

    public static Mode TheoCrystal => TasHelperSettings.AutoWatch_TheoCrystal;

    public static Mode Player => TasHelperSettings.AutoWatch_Player;

    public static bool ExcludePlayerDashState = false;

    public static bool ShowWallBoostTimer = true;

    public static bool ShowDreamDashCanEndTimer = true;

    public static bool ShowPlayerGliderBoostTimer = true;

    public static bool ShowDashAttackTimer = true;

    public static Mode CrushBlock => TasHelperSettings.AutoWatch_Kevin;

    public static Mode CutsceneEntity = Mode.Always;
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

    public static Mode CrushBlock = Mode.Always; // aka Kevin // differs if it's one-way

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
