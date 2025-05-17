using Celeste.Mod.TASHelper.Utils;
using TAS;
using TAS.Input;
using TAS.Input.Commands;

namespace Celeste.Mod.TASHelper.Predictor;
public static class InputManager {

    public static readonly List<InputFrame> Inputs = new List<InputFrame>();

    public static InputFrame EmptyInput;
    public static void ReadInputs(int frames) {
        Inputs.Clear();
        int startingFrame = Manager.Controller.CurrentFrameInTas;
        int endingFrame = startingFrame + frames;
        for (int i = startingFrame; i < endingFrame; i++) {
            Inputs.Add(Manager.Controller.Inputs.GetValueOrDefault(i, EmptyInput));
        }
    }

    public static void ExecuteCommands(int frame) {
        if (Manager.Controller.Commands.GetValueOrDefault(Manager.Controller.CurrentFrameInTas + frame) is List<Command> CurrentCommands) {
            foreach (Command command in CurrentCommands) {
                if (SupportedRuntimeCommands.Contains(command.Attribute.Name) &&
                    (!EnforceLegalCommand.EnabledWhenRunning || command.Attribute.LegalInFullGame)) {
                    command.Invoke();
                }
            }
        }
    }

    private static readonly List<string> _supportedRuntimeCommands = new() { "Set", "Invoke", "Console", "Mouse", "Press", "Gun", "EvalLua" };

    public static readonly HashSet<string> SupportedRuntimeCommands = new();

    [Initialize]
    public static void Initialize() {
        InputFrame.TryParse("9999", "", 0, 0, null, out InputFrame emptyInput);
        EmptyInput = emptyInput;
        SupportedRuntimeCommands.Clear();
        foreach (string str in _supportedRuntimeCommands) {
            if (Command.Commands.Where(x => x.Name == str) is { } commands && commands.IsNotNullOrEmpty() && commands.First().ExecuteTiming.Has(ExecuteTiming.Runtime)) {
                SupportedRuntimeCommands.Add(str);
            }
        }
    }
}

