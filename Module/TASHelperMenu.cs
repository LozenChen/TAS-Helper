using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using static Celeste.Mod.TASHelper.Module.TASHelperSettings;

namespace Celeste.Mod.TASHelper.Module;

internal static class TASHelperMenu {
    // basically taken from Celeste TAS
    private static readonly MethodInfo CreateKeyboardConfigUi = typeof(EverestModule).GetMethod("CreateKeyboardConfigUI", BindingFlags.NonPublic);
    private static readonly MethodInfo CreateButtonConfigUI = typeof(EverestModule).GetMethod("CreateButtonConfigUI", BindingFlags.NonPublic);
    private static TextMenu.Item hotkeysSubMenu;
    internal static string ToDialogText(this string input) => Dialog.Clean("TAS_HELPER_" + input.ToUpper().Replace(" ", "_"));

    private static TextMenuExt.SubMenu CreateLoadRangeSubMenu(TextMenu menu) {
        return new TextMenuExt.SubMenu("Load Range".ToDialogText(), false).Apply(subMenu => {
            TextMenu.Item LoadRangeModeItem;
            subMenu.Add(LoadRangeModeItem = new TextMenuExt.EnumerableSlider<LoadRangeModes>("Load Range Mode".ToDialogText(), CreateLoadRangeOptions(),
                TasHelperSettings.LoadRangeMode).Change(value => TasHelperSettings.LoadRangeMode = value));
            subMenu.AddDescription(menu, LoadRangeModeItem, "Load Range Description".ToDialogText());
            TextMenu.Item InViewRangeWidthItem;
            subMenu.Add(InViewRangeWidthItem = new TextMenuExt.IntSlider("In View Range Width".ToDialogText(), 0, 32, TasHelperSettings.InViewRangeWidth).Change(value => TasHelperSettings.InViewRangeWidth = value));
            subMenu.AddDescription(menu, InViewRangeWidthItem, "In View Description".ToDialogText());
            subMenu.Add(new TextMenuExt.IntSlider("Near Player Range Width".ToDialogText(), 1, 16, TasHelperSettings.NearPlayerRangeWidth).Change(value => TasHelperSettings.NearPlayerRangeWidth = value));
            subMenu.Add(new TextMenuExt.IntSlider("Load Range Opacity".ToDialogText(), 0, 9, TasHelperSettings.LoadRangeOpacity).Change(value => TasHelperSettings.LoadRangeOpacity = value));
            TextMenu.Item CameraZoomItem;
            subMenu.Add(CameraZoomItem = new TextMenu.OnOff("Apply Camera Zoom".ToDialogText(), TasHelperSettings.ApplyCameraZoom).Change(value => TasHelperSettings.ApplyCameraZoom = value));
            subMenu.AddDescription(menu, CameraZoomItem, "Apply Camera Zoom Description".ToDialogText());
        });
    }

    private static TextMenuExt.SubMenu CreateSimplifiedSpinnerSubMenu(TextMenu menu) {
        return new TextMenuExt.SubMenu("Simplified Spinners".ToDialogText(), false).Apply(subMenu => {
            subMenu.Add(new TextMenu.OnOff("Enabled".ToDialogText(), TasHelperSettings.EnableSimplifiedSpinner).Change(value => TasHelperSettings.EnableSimplifiedSpinner = value));
            subMenu.Add(new TextMenuExt.EnumerableSlider<ClearSpritesMode>("Clear Spinner Sprites".ToDialogText(), CreateClearSpritesModeOptions(), TasHelperSettings.EnforceClearSprites).Change(value => TasHelperSettings.EnforceClearSprites = value));
            subMenu.Add(new TextMenuExt.IntSlider("Spinner Filler Opacity".ToDialogText(), 0, 9, TasHelperSettings.SpinnerFillerOpacity).Change(value => TasHelperSettings.SpinnerFillerOpacity = value));
        });
    }

    private static TextMenuExt.SubMenu CreateCameraTargetSubMenu(TextMenu menu) {
        return new TextMenuExt.SubMenu("Camera Target".ToDialogText(), false).Apply(subMenu => {
            subMenu.Add(new TextMenu.OnOff("Enabled".ToDialogText(), TasHelperSettings.UsingCameraTarget).Change(value => TasHelperSettings.UsingCameraTarget = value));
            subMenu.Add(new TextMenuExt.IntSlider("Camera Target Vector Opacity".ToDialogText(), 1, 9, TasHelperSettings.CameraTargetLinkOpacity).Change(value => TasHelperSettings.CameraTargetLinkOpacity = value));
        });
    }

    private static TextMenuExt.SubMenu CreatePixelGridSubMenu(TextMenu menu) {
        return new TextMenuExt.SubMenu("Pixel Grid".ToDialogText(), false).Apply(subMenu => {
            subMenu.Add(new TextMenu.OnOff("Enable Pixel Grid".ToDialogText(), TasHelperSettings.EnablePixelGrid).Change(value => TasHelperSettings.EnablePixelGrid = value));
            subMenu.Add(new TextMenuExt.IntSlider("Pixel Grid Width".ToDialogText(), 0, 50, TasHelperSettings.PixelGridWidth).Change(value => TasHelperSettings.PixelGridWidth = value));
            subMenu.Add(new TextMenuExt.IntSlider("Pixel Grid Opacity".ToDialogText(), 1, 10, TasHelperSettings.PixelGridOpacity).Change(value => TasHelperSettings.PixelGridOpacity = value));
        });
    }
    private static TextMenuExt.SubMenu CreateHotkeysSubMenu(EverestModule everestModule, TextMenu menu) {
        return new TextMenuExt.SubMenu("Hotkeys".ToDialogText(), false).Apply(subMenu => {
            subMenu.Add(new TextMenu.Button(Dialog.Clean("options_keyconfig")).Pressed(() => {
                subMenu.Focused = false;
                KeyboardConfigUI keyboardConfig;
                if (CreateKeyboardConfigUi != null) {
                    keyboardConfig = (KeyboardConfigUI)CreateKeyboardConfigUi.Invoke(everestModule, new object[] { menu });
                }
                else {
                    keyboardConfig = new ModuleSettingsKeyboardConfigUI(everestModule);
                }

                keyboardConfig.OnClose = () => { subMenu.Focused = true; };

                Engine.Scene.Add(keyboardConfig);
                Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
            }));

            subMenu.Add(new TextMenu.Button(Dialog.Clean("options_btnconfig")).Pressed(() => {
                subMenu.Focused = false;
                ButtonConfigUI buttonConfig;
                if (CreateButtonConfigUI != null) {
                    buttonConfig = (ButtonConfigUI)CreateButtonConfigUI.Invoke(everestModule, new object[] { menu });
                }
                else {
                    buttonConfig = new ModuleSettingsButtonConfigUI(everestModule);
                }

                buttonConfig.OnClose = () => { subMenu.Focused = true; };

                Engine.Scene.Add(buttonConfig);
                Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
            }));
        }).Apply(subMenu => hotkeysSubMenu = subMenu);
    }

    private static IEnumerable<KeyValuePair<SpinnerMainSwitchModes, string>> CreateSpinnerMainSwitchOptions() {
        // no longer use this
        return new List<KeyValuePair<SpinnerMainSwitchModes, string>> {
            new(SpinnerMainSwitchModes.Off, "Spinner Main Switch Mode Off".ToDialogText()),
            new(SpinnerMainSwitchModes.OnlyDefault, "Spinner Main Switch Mode Only Default".ToDialogText()),
            new(SpinnerMainSwitchModes.AllowAll, "Spinner Main Switch Mode Allow All".ToDialogText()),
        };
    }
    private static IEnumerable<KeyValuePair<CountdownModes, string>> CreateCountdownOptions() {
        return new List<KeyValuePair<CountdownModes, string>> {
            new(CountdownModes.Off, "Countdown Mode Off".ToDialogText()),
            new(CountdownModes._3fCycle, "Countdown Mode 3f Cycle".ToDialogText()),
            new(CountdownModes._15fCycle, "Countdown Mode 15f Cycle".ToDialogText()),
        };
    }
    private static IEnumerable<KeyValuePair<LoadRangeModes, string>> CreateLoadRangeOptions() {
        return new List<KeyValuePair<LoadRangeModes, string>> {
            new(LoadRangeModes.Neither, "Load Range Mode Neither".ToDialogText()),
            new(LoadRangeModes.InViewRange, "Load Range Mode In View Range".ToDialogText()),
            new(LoadRangeModes.NearPlayerRange, "Load Range Mode Near Player Range".ToDialogText()),
            new(LoadRangeModes.Both, "Load Range Mode Both".ToDialogText()),
        };
    }
    private static IEnumerable<KeyValuePair<ClearSpritesMode, string>> CreateClearSpritesModeOptions() {
        return new List<KeyValuePair<ClearSpritesMode, string>> {
            new(ClearSpritesMode.Off, "Clear Spinner Sprites Mode Off".ToDialogText()),
            new(ClearSpritesMode.WhenSimplifyGraphics, "Clear Spinner Sprites Mode When Simplified Graphics".ToDialogText()),
            new(ClearSpritesMode.Always, "Clear Spinner Sprites Mode Always".ToDialogText()),
        };
    }


    public static void AddDescription(this TextMenuExt.SubMenu subMenu, TextMenu containingMenu, TextMenu.Item subMenuItem, string description) {
        TextMenuExt.EaseInSubHeaderExt descriptionText = new(description, false, containingMenu) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        subMenu.Add(descriptionText);
        subMenuItem.OnEnter += () => descriptionText.FadeVisible = true;
        subMenuItem.OnLeave += () => descriptionText.FadeVisible = false;
    }

    public static void CreateMenu(EverestModule everestModule, TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.OnOff("Enabled".ToDialogText(), TasHelperSettings.Enabled).Change((value) => { TasHelperSettings.Enabled = value; }));
        TextMenu.Item SpinnerMainItem;
        menu.Add(SpinnerMainItem = new TextMenu.OnOff("Spinner Main Switch".ToDialogText(), TasHelperSettings.SpinnerEnabled).Change((value) => { TasHelperSettings.SpinnerEnabled = value; }));
        SpinnerMainItem.AddDescription(menu, "Spinner Main Switch Description".ToDialogText());
        menu.Add(new TextMenu.OnOff("Show Cycle Hitbox Colors".ToDialogText(), TasHelperSettings.ShowCycleHitboxColors).Change(value => TasHelperSettings.ShowCycleHitboxColors = value));
        TextMenu.Item CountdownModeItem;
        menu.Add(CountdownModeItem = new TextMenuExt.EnumerableSlider<CountdownModes>("Countdown Mode".ToDialogText(), CreateCountdownOptions(),
                TasHelperSettings.CountdownMode).Change(value => TasHelperSettings.CountdownMode = value));
        CountdownModeItem.AddDescription(menu, "Countdown Mode Description".ToDialogText());
        menu.Add(CreateLoadRangeSubMenu(menu));
        menu.Add(CreateSimplifiedSpinnerSubMenu(menu));
        menu.Add(CreatePixelGridSubMenu(menu));
        TextMenu.Item EntityActivatorReminderItem;
        menu.Add(EntityActivatorReminderItem = new TextMenu.OnOff("Entity Activator Reminder".ToDialogText(), TasHelperSettings.EntityActivatorReminder).Change((value) => TasHelperSettings.EntityActivatorReminder = value));
        EntityActivatorReminderItem.AddDescription(menu, "Entity Activator Reminder Description".ToDialogText());
        menu.Add(CreateCameraTargetSubMenu(menu));
        menu.Add(CreateHotkeysSubMenu(everestModule, menu));
        hotkeysSubMenu.AddDescription(menu, "Hotkey Description".ToDialogText());
    }

}
