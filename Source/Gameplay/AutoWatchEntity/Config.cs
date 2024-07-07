using Mode = Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity.RenderMode;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class Config{
    public static bool MainEnabled = true;

    public static Mode SwapBlock = Mode.WhenWatched;

    public static Mode Refill = Mode.Always;

    public static Mode FallingBlock = Mode.Always;

    public static Mode Booster = Mode.Always;

    public static Mode ZipMover = Mode.Always;
}

internal static class Format {


    public static bool Speed_UsePixelPerSecond = true; // instead of PixelPerFrame
}