using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using Hotkey = Celeste.Mod.TASHelper.Module.Menu.Hotkeys_BASE.Hotkey_BASE;

namespace Celeste.Mod.TASHelper.Module.Menu;

public static class TH_Hotkeys {

    public static Hotkey MainSwitchHotkey { get; set; }

    public static Hotkey CountDownHotkey { get; set; }

    public static Hotkey LoadRangeHotkey { get; set; }

    public static Hotkey PixelGridWidthHotkey { get; set; }

    public static Hotkey PredictEnableHotkey { get; set; }

    public static Hotkey PredictFutureHotkey { get; set; }

    public static Hotkey OoO_Step_Hotkey { get; set; }

    public static Hotkey OoO_Fastforward_Hotkey { get; set; }

    public static Hotkey AutoWatchHotkey { get; set; }

    public static List<Hotkey> HotkeyList = new();

    [Load]
    public static void Load() {
        using (DetourContextHelper.ManagedHook(DetourContextHelper.HookIds.Hotkeys).AppliedAfter(DetourContextHelper.CelesteTAS).Use()) {
            // FrameStepBack hotkey invokes a load state, and we should be after CenterCamera.RestoreCamera
            On.Celeste.Level.Render += HotkeysPressed;
        }
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.Render -= HotkeysPressed;
    }


    [Initialize]
    public static void HotkeyInitialize() {
        MainSwitchHotkey = BindingToHotkey(TasHelperSettings.keyMainSwitch);
        CountDownHotkey = BindingToHotkey(TasHelperSettings.keyCountDown);
        LoadRangeHotkey = BindingToHotkey(TasHelperSettings.keyLoadRange);
        PixelGridWidthHotkey = BindingToHotkey(TasHelperSettings.keyPixelGridWidth);
        PredictEnableHotkey = BindingToHotkey(TasHelperSettings.keyPredictEnable);
        PredictFutureHotkey = BindingToHotkey(TasHelperSettings.keyPredictFuture);
        OoO_Step_Hotkey = BindingToHotkey(TasHelperSettings.keyOoO_Step);
        OoO_Fastforward_Hotkey = BindingToHotkey(TasHelperSettings.keyOoO_Fastforward);
        AutoWatchHotkey = BindingToHotkey(TasHelperSettings.keyAutoWatch);

        HotkeyList = new List<Hotkey> { MainSwitchHotkey, CountDownHotkey, LoadRangeHotkey, PixelGridWidthHotkey, PredictEnableHotkey, PredictFutureHotkey, OoO_Step_Hotkey, OoO_Fastforward_Hotkey, AutoWatchHotkey };
    }

    private static void HotkeysPressed(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);
        bool changed = TasHelperSettings.SettingsHotkeysPressed();
        global::Celeste.Mod.TASHelper.Entities.HotkeyWatcher.FlushPending();
        if (changed) {
            TASHelperModule.Instance.SaveSettings();
        }
    }

    public static void Update(bool updateKey, bool updateButton) {
        Hotkeys_BASE.UpdateMeta();
        foreach (Hotkey hotkey in HotkeyList) {
            hotkey.Update(updateKey, updateButton);
        }
    }



    internal static Hotkey BindingToHotkey(ButtonBinding binding, bool held = false) {
        return new(binding.Keys, binding.Buttons, true, held);
    }
}


// taken from CelesteTAS
// previously we are using the class from CelesteTAS; but now to reduce dependency on CelesteTAS, we copy it here
public static class Hotkeys_BASE {

    private static KeyboardState kbState;
    private static GamePadState padState;

    internal static void UpdateMeta() {
        kbState = Keyboard.GetState();
        padState = GetGamePadState();

    }
    private static GamePadState GetGamePadState() {
        GamePadState currentState = MInput.GamePads[0].CurrentState;
        for (int i = 0; i < 4; i++) {
            currentState = GamePad.GetState((PlayerIndex)i);
            if (currentState.IsConnected) {
                break;
            }
        }

        return currentState;
    }

    public class Hotkey_BASE {
        public readonly List<Buttons> Buttons;
        private readonly bool held;
        private readonly bool keyCombo;
        public readonly List<Keys> Keys;
        private DateTime lastPressedTime;
        public bool OverrideCheck;

        public Hotkey_BASE(List<Keys> keys, List<Buttons> buttons, bool keyCombo, bool held) {
            Keys = keys;
            Buttons = buttons;
            this.keyCombo = keyCombo;
            this.held = held;
        }

        public bool Check { get; private set; }
        public bool LastCheck { get; private set; }
        public bool Pressed => !LastCheck && Check;

        // note: dont check DoublePressed on render, since unstable DoublePressed response during frame drops
        public bool DoublePressed { get; private set; }
        public bool Released => LastCheck && !Check;

        public void Update(bool updateKey = true, bool updateButton = true) {
            LastCheck = Check;
            bool keyCheck;
            bool buttonCheck;

            if (OverrideCheck) {
                keyCheck = buttonCheck = true;
                if (!held) {
                    OverrideCheck = false;
                }
            }
            else {
                keyCheck = updateKey && IsKeyDown();
                buttonCheck = updateButton && IsButtonDown();
            }

            Check = keyCheck || buttonCheck;

            if (Pressed) {
                DateTime pressedTime = DateTime.Now;
                DoublePressed = pressedTime.Subtract(lastPressedTime).TotalMilliseconds < 200;
                lastPressedTime = DoublePressed ? default : pressedTime;
            }
            else {
                DoublePressed = false;
            }
        }

        private bool IsKeyDown() {
            List<Keys> keys = Keys;
            if (keys is null || keys.Count == 0 || kbState == default) {
                return false;
            }
            // manual loops instead of Keys.All/Any: method-group delegates would be allocated for every hotkey every frame
            if (keyCombo) {
                for (int i = 0; i < keys.Count; i++) {
                    if (!kbState.IsKeyDown(keys[i])) {
                        return false;
                    }
                }
                return true;
            }
            for (int i = 0; i < keys.Count; i++) {
                if (kbState.IsKeyDown(keys[i])) {
                    return true;
                }
            }
            return false;
        }

        private bool IsButtonDown() {
            List<Buttons> buttons = Buttons;
            if (buttons is null || buttons.Count == 0 || padState == default) {
                return false;
            }
            if (keyCombo) {
                for (int i = 0; i < buttons.Count; i++) {
                    if (!padState.IsButtonDown(buttons[i])) {
                        return false;
                    }
                }
                return true;
            }
            for (int i = 0; i < buttons.Count; i++) {
                if (padState.IsButtonDown(buttons[i])) {
                    return true;
                }
            }
            return false;
        }
    }
}