using Mode = Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity.RenderMode;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class Config{
    public static bool MainEnabled = true;

    public static Mode SwapBlock = Mode.WhenWatched;

    public static Mode MoveBlock = Mode.Always;

    public static Mode Refill = Mode.Always;

    public static Mode FallingBlock = Mode.Always;

    public static Mode Booster = Mode.Always;

    public static Mode ZipMover = Mode.Always;

    public static Mode FloatySpaceBlock = Mode.Always;

    public static Mode Glider = Mode.Always;

    public static Mode Cloud = Mode.Always;
}

internal static class Format {


    public static bool Speed_UsePixelPerSecond = true; // instead of PixelPerFrame
}