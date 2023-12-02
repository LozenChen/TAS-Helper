using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using TAS.EverestInterop;
using CMCore = Celeste.Mod.Core;
using Hotkey = TAS.EverestInterop.Hotkeys.Hotkey;

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

    public static Hotkey OpenConsole { get; set; }

    public static List<Hotkey> Hotkeys = new();

    [Load]
    public static void Load() {
        On.Celeste.Level.Render += HotkeysPressed;
        IL.Celeste.Mod.ModuleSettingsKeyboardConfigUI.Reset += ModReload;
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.Render -= HotkeysPressed;
        IL.Celeste.Mod.ModuleSettingsKeyboardConfigUI.Reset -= ModReload;
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
        if (typeof(CMCore.CoreModuleSettings).GetProperty("DebugConsole") is { } getDebugConsole) {
            // there's a period of time when DebugConsole get renamed
            // and before that commit, ToggleDebugConsole doesn't exist
            // several commits later, ToggleDebugConsole gets introduced, and DebugConsole gets renamed back 
            // https://github.com/EverestAPI/Everest/commit/4efe4d1adc95e07e242eb597e390727d3ce90593
            List<Keys> keys;
            List<Buttons> buttons;
            ButtonBinding debugConsole = (ButtonBinding)getDebugConsole.GetValue(CMCore.CoreModule.Settings);
            if (typeof(CMCore.CoreModuleSettings).GetProperty("ToggleDebugConsole") is { } getToggleDebugConsole) { // Everest >= 4351
                ButtonBinding toggleDebugConsole = (ButtonBinding)getToggleDebugConsole.GetValue(CMCore.CoreModule.Settings);
                keys = debugConsole.Keys.Union(toggleDebugConsole.Keys).ToList();
                buttons = debugConsole.Buttons.Union(toggleDebugConsole.Buttons).ToList();
            }
            else {
                keys = debugConsole.Keys.Union(new Keys[] { Keys.OemTilde, Keys.Oem8 }).ToList();
                buttons = debugConsole.Buttons;
            }
            OpenConsole = new Hotkey(keys, buttons, false, false);
        }
        else {
            OpenConsole = new Hotkey(null, null, false, false);
        }

        Hotkeys = new List<Hotkey> { MainSwitchHotkey, CountDownHotkey, LoadRangeHotkey, PixelGridWidthHotkey, PredictEnableHotkey, PredictFutureHotkey, OoO_Step_Hotkey, OoO_Fastforward_Hotkey, OpenConsole };
    }

    private static void HotkeysPressed(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);
        if (TasHelperSettings.SettingsHotkeysPressed()) {
            TASHelperModule.Instance.SaveSettings();
        }
    }

    public static void Update(bool updateKey, bool updateButton) {
        foreach (Hotkey hotkey in Hotkeys) {
            hotkey.Update(updateKey, updateButton);
        }
    }

    private static Hotkey BindingToHotkey(ButtonBinding binding, bool held = false) {
        return new(binding.Keys, binding.Buttons, true, held);
    }

    private static IEnumerable<PropertyInfo> bindingProperties;

    private static FieldInfo bindingFieldInfo;

    private static void ModReload(ILContext il) {
        bindingProperties = typeof(TASHelperSettings)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(info => info.PropertyType == typeof(ButtonBinding) &&
                           info.GetCustomAttribute<DefaultButtonBinding2Attribute>() is { } extraDefaultKeyAttribute &&
                           extraDefaultKeyAttribute.ExtraKey != Keys.None);

        ILCursor ilCursor = new(il);
        if (ilCursor.TryGotoNext(
                MoveType.After,
                ins => ins.OpCode == OpCodes.Callvirt && ins.Operand.ToString().Contains("<Microsoft.Xna.Framework.Input.Keys>::Add(T)")
            )) {
            ilCursor.Emit(OpCodes.Ldloc_1).EmitDelegate(AddExtraDefaultKey);
        }
    }

    private static void AddExtraDefaultKey(object bindingEntry) {
        if (bindingFieldInfo == null) {
            bindingFieldInfo = bindingEntry.GetType().GetFieldInfo("Binding");
        }

        if (bindingFieldInfo?.GetValue(bindingEntry) is not ButtonBinding binding) {
            return;
        }

        if (bindingProperties.FirstOrDefault(info => info.GetValue(TasHelperSettings) == binding) is { } propertyInfo) {
            binding.Keys.Add(propertyInfo.GetCustomAttribute<DefaultButtonBinding2Attribute>().ExtraKey);
        }
    }


}