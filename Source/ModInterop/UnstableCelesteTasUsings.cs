// these may depend on publicizer / are not PublicAPI / will change later / have changed sometime ago
// only part of them

namespace Celeste.Mod.TASHelper.ModInterop;
internal class UnstableCelesteTasUsings {
    public static bool TasRecorderIsRecording => TAS.ModInterop.TASRecorderInterop.IsRecording;

    public static bool playerUpdated {
        get => TAS.Gameplay.Hitboxes.ActualCollideHitbox.playerUpdated;
        set => TAS.Gameplay.Hitboxes.ActualCollideHitbox.playerUpdated = value;
    }

    public static TAS.Input.InputFrame CreateEmptyInput() {
        // Celeste v3.43.9
        // public static bool TryParse(string line,
        //      int studioLine, InputFrame? prevInputFrame, [NotNullWhen(true)] out InputFrame? inputFrame,
        //      int repeatIndex = 0, int repeatCount = 0, int frameOffset = 0, Command? parentCommand = null)
        // adds an additional "Command? parentCommand" parameter than previous versions
        // which makes Function Signature changes
        TAS.Input.InputFrame.TryParse("9999", 0, null, out TAS.Input.InputFrame emptyInput);
        return emptyInput;
    }
}
