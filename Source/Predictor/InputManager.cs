using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System.Reflection;
using TAS;
using TAS.Input;

namespace Celeste.Mod.TASHelper.Predictor;
public static class InputManager {

    // mostly copied from tas mod


    public static float FreezeTimer = 0f;
    public static void Freeze(float time) {
        if (time > FreezeTimer) {
            FreezeTimer = time;
        }
    }

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

    private static GamePadState Store_gamePadData = new();
    private static MouseState Store_mouseData = new();
    private static KeyboardState Store_keyboardData = new();
    private static readonly Dictionary<VirtualInput, VirtualInputData> Dictionary = new Dictionary<VirtualInput, VirtualInputData>();
    private static MInput.GamePadData gamePadData => MInput.GamePads[Input.Gamepad];
    public class VirtualInputData {
        public float f1;
        public float f2;
        public bool b1;
        public bool b2;
        public int i1;
        public int i2;
        public Dictionary<object, VirtualInputData> children;
        public VirtualInputData() {
            children = new Dictionary<object, VirtualInputData>();
        }
    }
    public static void StoreInputState() {
        Store_gamePadData = gamePadData.CurrentState;
        Store_mouseData = MInput.Mouse.CurrentState;
        Store_keyboardData = MInput.Keyboard.CurrentState;
        FieldInfo virtualInputsField = typeof(MInput).GetFieldInfo("VirtualInputs");
        if ((List<VirtualInput>)virtualInputsField.GetValue(null) is not { } list) {
            return;
        }
        foreach (VirtualInput input in list) {
            if (input is VirtualButton button) {
                Dictionary.Add(input, new VirtualInputData {
                    b1 = button.GetFieldValue<bool>("consumed"),
                    f1 = button.GetFieldValue<float>("bufferCounter"),
                    f2 = button.GetFieldValue<float>("repeatCounter")
                });
                // consumed, bufferCounter, repeatCounter
            }
            else if (input is VirtualAxis axis) {
                if (axis.Nodes.Where(self => self is VirtualAxis.KeyboardKeys).Cast<VirtualAxis.KeyboardKeys>() is { } vaKeys && vaKeys.IsNotNullOrEmpty()) {
                    VirtualInputData data = new();
                    foreach (VirtualAxis.KeyboardKeys vaKey in vaKeys) {
                        data.children.Add(vaKey, new VirtualInputData {
                            b1 = vaKey.GetFieldValue<bool>("turned"),
                            f1 = vaKey.GetFieldValue<float>("value")
                        });
                    }
                    Dictionary.Add(input, data);
                }
                // each Node's value, which for most inputs, already stored in gamepadData
                // except KeyboardKeys : Node, we need to store its "turned" and "value"
            }
            else if (input is VirtualIntegerAxis intAxis) {
                VirtualInputData data = new();
                data.b1 = intAxis.GetFieldValue<bool>("turned");
                if (intAxis.Nodes.Where(self => self is VirtualAxis.KeyboardKeys).Cast<VirtualAxis.KeyboardKeys>() is { } vaKeys && vaKeys.IsNotNullOrEmpty()) {
                    foreach (VirtualAxis.KeyboardKeys vaKey in vaKeys) {
                        data.children.Add(vaKey, new VirtualInputData {
                            b1 = vaKey.GetFieldValue<bool>("turned"),
                            f1 = vaKey.GetFieldValue<float>("value")
                        });
                    }
                }
                Dictionary.Add(input, data);
                // its "turned", and KeyboardKeys:Node 's data
            }
            else if (input is VirtualJoystick joystick) {
                VirtualInputData data = new();
                data.b1 = joystick.GetFieldValue<bool>("hTurned");
                data.b2 = joystick.GetFieldValue<bool>("vTurned");
                Vector2 v = joystick.GetFieldValue<Vector2>("value");
                data.f1 = v.X;
                data.f2 = v.Y;
                if (joystick.Nodes.Where(self => self is VirtualJoystick.KeyboardKeys).Cast<VirtualJoystick.KeyboardKeys>() is { } vjKeys && vjKeys.IsNotNullOrEmpty()) {
                    foreach (VirtualJoystick.KeyboardKeys vjKey in vjKeys) {
                        Vector2 v2 = vjKey.GetFieldValue<Vector2>("value");
                        data.children.Add(vjKey, new VirtualInputData {
                            b1 = vjKey.GetFieldValue<bool>("turnedX"),
                            b2 = vjKey.GetFieldValue<bool>("turnedY"),
                            f1 = v2.X,
                            f2 = v2.Y
                        });
                    }
                }
                Dictionary.Add(input, data);
                // KeyboardKeys's turnedX, turnedY, value, and Joystick's hturned, vturned, value
            }
        }
    }

    public static void RestoreInputState() {
        gamePadData.CurrentState = Store_gamePadData;
        MInput.Mouse.CurrentState = Store_mouseData;
        MInput.Keyboard.CurrentState = Store_keyboardData;
        FieldInfo virtualInputsField = typeof(MInput).GetFieldInfo("VirtualInputs");
        if ((List<VirtualInput>)virtualInputsField.GetValue(null) is not { } list) {
            Dictionary.Clear();
            return;
        }
        foreach (VirtualInput input in list) {
            if (!Dictionary.TryGetValue(input, out VirtualInputData data)) {
                continue;
            }
            if (input is VirtualButton button) {
                button.SetFieldValue("consumed", data.b1);
                button.SetFieldValue("bufferCounter", data.f1);
                button.SetFieldValue("repeatCounter", data.f2);
            }
            else if (input is VirtualAxis axis) {
                if (axis.Nodes.Where(self => self is VirtualAxis.KeyboardKeys).Cast<VirtualAxis.KeyboardKeys>() is { } vaKeys && vaKeys.IsNotNullOrEmpty()) {
                    foreach (VirtualAxis.KeyboardKeys vaKey in vaKeys) {
                        vaKey.SetFieldValue("turned", data.children[vaKey].b1);
                        vaKey.SetFieldValue("value", data.children[vaKey].f1);
                    }
                }
            }
            else if (input is VirtualIntegerAxis intAxis) {
                intAxis.SetFieldValue("turned", data.b1);
                if (intAxis.Nodes.Where(self => self is VirtualAxis.KeyboardKeys).Cast<VirtualAxis.KeyboardKeys>() is { } vaKeys && vaKeys.IsNotNullOrEmpty()) {
                    foreach (VirtualAxis.KeyboardKeys vaKey in vaKeys) {
                        vaKey.SetFieldValue("turned", data.children[vaKey].b1);
                        vaKey.SetFieldValue("value", data.children[vaKey].f1);
                    }
                }
            }
            else if (input is VirtualJoystick joystick) {
                joystick.SetFieldValue("hTurned", data.b1);
                joystick.SetFieldValue("vTurned", data.b2);
                joystick.SetFieldValue("value", new Vector2(data.f1, data.f2));
                if (joystick.Nodes.Where(self => self is VirtualJoystick.KeyboardKeys).Cast<VirtualJoystick.KeyboardKeys>() is { } vjKeys && vjKeys.IsNotNullOrEmpty()) {
                    foreach (VirtualJoystick.KeyboardKeys vjKey in vjKeys) {
                        vjKey.SetFieldValue("turnedX", data.children[vjKey].b1);
                        vjKey.SetFieldValue("turnedY", data.children[vjKey].b2);
                        vjKey.SetFieldValue("value", new Vector2(data.children[vjKey].f1, data.children[vjKey].f2));
                    }
                }
            }
        }
        Dictionary.Clear();
    }
}

