using Celeste.Mod.TASHelper.Utils;
using TAS;
using TAS.Input;

namespace Celeste.Mod.TASHelper.Predictor;
public static class InputManager {

    public static readonly List<InputFrame> Inputs = new List<InputFrame>();

    public static InputFrame EmptyInput;
    public static void ReadInputs(int frames) {
        Inputs.Clear();
        for (int i = 0; i < frames; i++) {
            Inputs.Add(Manager.Controller.Inputs.GetValueOrDefault(Manager.Controller.CurrentFrameInTas + i, EmptyInput));
        }
    }

    [Initialize]
    public static void Initialize() {
        InputFrame.TryParse("9999", 0, null, out InputFrame emptyInput);
        EmptyInput = emptyInput;
    }
}

