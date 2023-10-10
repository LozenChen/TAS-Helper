using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using TAS.EverestInterop;
using Hotkey = TAS.EverestInterop.Hotkeys.Hotkey;

namespace Celeste.Mod.TASHelper.Module.Menu;

public static class TH_Hotkeys {

    public static Hotkey MainSwitchHotkey { get; set; }

    public static Hotkey CountDownHotkey { get; set; }

    public static Hotkey LoadRangeHotkey { get; set; }

    public static Hotkey PixelGridWidthHotkey { get; set; }

    public static Hotkey PredictEnableHotkey { get; set; }

    public static Hotkey PredictFutureHotkey { get; set; }

    public static Hotkey OOPHotkey { get; set; }

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
        OOPHotkey = BindingToHotkey(TasHelperSettings.keyOOP);
    }

    private static void HotkeysPressed(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);
        if (TasHelperSettings.SettingsHotkeysPressed()) {
            TASHelperModule.Instance.SaveSettings();
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