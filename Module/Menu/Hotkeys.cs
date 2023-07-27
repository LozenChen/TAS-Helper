using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using TAS.EverestInterop;

namespace Celeste.Mod.TASHelper.Module.Menu;

internal static class TH_Hotkeys {

    private static IEnumerable<PropertyInfo> bindingProperties;

    private static FieldInfo bindingFieldInfo;
    public static void Load() {
        IL.Celeste.Mod.ModuleSettingsKeyboardConfigUI.Reset += ModReload;
    }

    public static void Unload() {
        IL.Celeste.Mod.ModuleSettingsKeyboardConfigUI.Reset -= ModReload;
    }

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