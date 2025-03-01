// these may depend on publicizer / is not PublicAPI / should change later
// only part of them

namespace Celeste.Mod.TASHelper.ModInterop;
internal class UnstableCelesteTasUsings {
    public static bool TasRecorderIsRecording => TAS.ModInterop.TASRecorderInterop.IsRecording;

    public static bool playerUpdated {
        get => TAS.Gameplay.Hitboxes.ActualCollideHitbox.playerUpdated;
        set => TAS.Gameplay.Hitboxes.ActualCollideHitbox.playerUpdated = value;
    }
}
