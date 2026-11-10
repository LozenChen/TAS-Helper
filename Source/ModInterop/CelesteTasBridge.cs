// CelesteTAS members that depend on the publicizer / are not stable API. Keeping them in one bridge makes it
// obvious where TASHelper reaches into CelesteTAS beyond the documented ModInterop surface.
namespace Celeste.Mod.TASHelper.ModInterop;

internal static class CelesteTasBridge {
    public static bool PlayerUpdated {
        get => TAS.Gameplay.Hitboxes.ActualCollideHitbox.playerUpdated;
        set => TAS.Gameplay.Hitboxes.ActualCollideHitbox.playerUpdated = value;
    }

    public static TAS.Input.InputFrame CreateEmptyInput() {
        // CelesteTAS v3.44.0: InputFrame.TryParse gained parameters, so keep the call in one place.
        TAS.Input.InputFrame.TryParse("9999", "", 0, 0, null, out TAS.Input.InputFrame emptyInput);
        return emptyInput;
    }

    public static bool DebugRendered => GlobalVariables.DebugRendered;
    public static bool FastForwarding => GlobalVariables.FastForwarding;
    public static bool FrameStep => GlobalVariables.FrameStep;
    public static bool StrictFrameStep => GlobalVariables.StrictFrameStep;
}
