using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System.Reflection;
using TAS;
using TAS.Input;

namespace Celeste.Mod.TASHelper.Predictor;
public static class InputManager {

    public static readonly List<InputFrame> P_Inputs = new List<InputFrame>();

    public static InputFrame EmptyInput;
    public static void ReadInputs(int frames) {
        P_Inputs.Clear();
        for (int i = 0; i < frames; i++) {
            P_Inputs.Add(Manager.Controller.Inputs.GetValueOrDefault(Manager.Controller.CurrentFrameInTas + i, EmptyInput));
        }
    }

    public static void Initialize() {
        InputFrame.TryParse("9999", 0, null, out InputFrame emptyInput);
        EmptyInput = emptyInput;
    }
}

