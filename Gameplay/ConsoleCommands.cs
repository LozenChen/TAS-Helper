using Monocle;
namespace Celeste.Mod.TASHelper.Gameplay;

public static class ConsoleCommands {
    [Command("spinner_freeze", "Quick command to set Level.TimeActive 524288 (TAS Helper)")]
    public static void CmdSpinnerFreeze(bool on = true) {
        if (Engine.Scene is Level level) {
            level.TimeActive = on ? 524288f : 0f;
        }
    }
}