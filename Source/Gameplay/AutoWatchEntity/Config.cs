using Microsoft.Xna.Framework;
using Mode = Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity.RenderMode;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class Config {
    public static bool MainEnabled => TasHelperSettings.AutoWatchEnable;

    public static Vector2 HiresFontSize = new Vector2(0.8f); // settings.HiresFontSize / 10f

    public static float HiresFontStroke = 5 * 0.4f; // settigns.HiresFontStroke

    public static Mode BadelineBoost => TasHelperSettings.AutoWatch_BadelineOrb;

    public static Mode Booster => TasHelperSettings.AutoWatch_Booster;

    public static Mode Bumper => TasHelperSettings.AutoWatch_Bumper;

    public static ShakeRenderMode Bumper_ChooseOffsetVelocity => TasHelperSettings.AutoWatch_Bumper_NoneOrVelocityOrOffset;

    public static Mode Cloud => TasHelperSettings.AutoWatch_Cloud;

    public static Mode CrushBlock => TasHelperSettings.AutoWatch_Kevin;

    public static Mode CutsceneEntity => TasHelperSettings.AutoWatch_Cutscene;

    public static Mode FallingBlock => TasHelperSettings.AutoWatch_FallingBlock;

    public static Mode FlingBird => TasHelperSettings.AutoWatch_FlingBird;

    public static Mode FloatySpaceBlock => TasHelperSettings.AutoWatch_MoonBlock;

    public static bool FloatySpaceBlock_UseOffsetInsteadOfVelocity => TasHelperSettings.AutoWatch_MoonBlock_VelocityOrOffset == ShakeRenderMode.Offset;

    public static Mode Glider => TasHelperSettings.AutoWatch_Jelly;

    public static Mode MoveBlock => TasHelperSettings.AutoWatch_MoveBlock;

    public static Mode Player => TasHelperSettings.AutoWatch_Player;

    public static bool ShowDashTimer => TasHelperSettings.AutoWatch_ShowDashTimer;

    public static bool ShowWallBoostTimer => TasHelperSettings.AutoWatch_ShowWallBoostTimer;

    public static bool ShowStLaunchSpeed => TasHelperSettings.AutoWatch_ShowStLaunchSpeed;

    public static bool ShowDreamDashCanEndTimer => TasHelperSettings.AutoWatch_ShowDreamDashCanEndTimer;

    public static bool ShowPlayerGliderBoostTimer => TasHelperSettings.AutoWatch_ShowPlayerGliderBoostTimer;

    public static bool ShowDashAttackTimer => TasHelperSettings.AutoWatch_ShowDashAttackTimer;

    public static Mode Puffer => TasHelperSettings.AutoWatch_Puffer;

    public static ShakeRenderMode Puffer_ChooseOffsetVelocity => TasHelperSettings.AutoWatch_Puffer_NoneOrVelocityOrOffset;
    public static Mode Refill => TasHelperSettings.AutoWatch_Refill;

    public static Mode Seeker => TasHelperSettings.AutoWatch_Seeker;
    public static Mode SwapBlock => TasHelperSettings.AutoWatch_SwapBlock;

    public static Mode TheoCrystal => TasHelperSettings.AutoWatch_TheoCrystal;

    public static Mode Trigger = Mode.Never;

    public static Mode ZipMover => TasHelperSettings.AutoWatch_ZipMover;

}
public enum ShakeRenderMode { None, Offset, Velocity } // 我在近义词里面纠结了很久, 最终将其定为 中文名定为 摇摆, 英文名定为 Shake
internal static class Format {

    public static bool Speed_UsePixelPerSecond = true; // instead of PixelPerFrame
}

internal static class TODO {

    public static Mode Lookout = Mode.Always;

    public static Mode Triggers = Mode.Always; // take care of compatibility with simplified triggers , camera trigger in particular

    public static Mode CrumblePlatform = Mode.Always; // disappear and respawn

    public static Mode Seeker = Mode.Always; // more info

    public static Mode EntityCollisionLine = Mode.Always; // show a line when two entities collide and pass momentum, e.g. Seeker hit Theo, e.g. ExplodeLaunch

    public static Mode HeartGem = Mode.Always; // the collect routine

    public static Mode BounceBlock = Mode.Always;

    public static Mode CornerBoostBlock = Mode.Always;

    public static Mode SwitchGate = Mode.Always; // 0 / n

    public static Mode LockBlock = Mode.Always;

    public static Mode DustTrackSpinner = Mode.Always;

    public static Mode OshiroBoss = Mode.Always; // engine timerate shown on him

    public static Mode DashBlock = Mode.Always; // change its render / debug render, make it half-transparent? // differs if it's not dashable by player

    public static Mode MoveingPlatform = Mode.Always;

    public static Mode SnowBall = Mode.Always;

    public static Mode TempleGate = Mode.Always; // 0 / n

    public static Mode CrushBlock = Mode.Always; // aka Kevin // differs if it's one-way

    public static Mode FinalBoss = Mode.Always; // 6a boss, timerate?

    public static Mode FinalBossMovingBlock = Mode.Always;

    public static Mode Puffer = Mode.Always;

    public static Mode LightningBreakerBox = Mode.Always;

    public static Mode NPC = Mode.Always;

    public static Mode Camera = Mode.Always;

}
