using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using static Celeste.Mod.TASHelper.Module.TASHelperSettings;

namespace Celeste.Mod.TASHelper.Module;

internal static class TASHelperMenu {
    // basically taken from Celeste TAS
    private static readonly MethodInfo CreateKeyboardConfigUi = typeof(EverestModule).GetMethod("CreateKeyboardConfigUI", BindingFlags.NonPublic);
    private static readonly MethodInfo CreateButtonConfigUI = typeof(EverestModule).GetMethod("CreateButtonConfigUI", BindingFlags.NonPublic);
    internal static string ToDialogText(this string input) => Dialog.Clean("TAS_HELPER_" + input.ToUpper().Replace(" ", "_"));

    private static OptionSubMenuCountExt CreateColorCustomizationSubMenu(TextMenu menu, bool inGame) {
        OptionSubMenuCountExt ColorCustomizationItem = new OptionSubMenuCountExt("Color Customization".ToDialogText());
        ColorCustomizationItem.OnLeave += () => ColorCustomizationItem.MenuIndex = 0;
        ColorCustomizationItem.Add("Color Customization Finished".ToDialogText(), new List<TextMenu.Item>());
        ColorCustomizationItem.Add("Color Customization OnOff".ToDialogText(), CustomColors.CreateColorCustomization_PageOnOff(menu, inGame));
        ColorCustomizationItem.Add("Color Customization Spinner Color".ToDialogText(), CustomColors.CreateColorCustomization_PageSpinnerColor(menu, inGame));
        ColorCustomizationItem.Add("Color Customization Other".ToDialogText(), CustomColors.CreateColorCustomization_PageOther(menu, inGame));
        return ColorCustomizationItem;
    }


    private static TextMenuExt.SubMenu CreateCountdownSubMenu(TextMenu menu) {
        return new TextMenuExt.SubMenu("Countdown".ToDialogText(), false).Apply(subMenu => {
            TextMenu.Item CountdownModeItem;
            subMenu.Add(CountdownModeItem = new TextMenuExt.EnumerableSlider<CountdownModes>("Countdown Mode".ToDialogText(), CreateCountdownOptions(),
                    TasHelperSettings.CountdownMode).Change(value => TasHelperSettings.CountdownMode = value));
            subMenu.AddDescription(menu, CountdownModeItem, "Countdown Mode Description".ToDialogText());
            subMenu.Add(new TextMenuExt.EnumerableSlider<CountdownFonts>("Font".ToDialogText(), CreateCountdownFontOptions(),
                TasHelperSettings.CountdownFont).Change(value => TasHelperSettings.CountdownFont = value));
            subMenu.Add(new TextMenuExt.IntSlider("Hires Font Size".ToDialogText(), 1, 20, TasHelperSettings.HiresFontSize).Change(value => TasHelperSettings.HiresFontSize = value));
            subMenu.Add(new TextMenuExt.IntSlider("Hires Font Stroke".ToDialogText(), 0, 20, TasHelperSettings.HiresFontStroke).Change(value => TasHelperSettings.HiresFontStroke = value));
            TextMenu.Item OptimizationItem;
            subMenu.Add(OptimizationItem = new TextMenu.OnOff("Performance Optimization".ToDialogText(), TasHelperSettings.DoNotRenderWhenFarFromView).Change(value => TasHelperSettings.DoNotRenderWhenFarFromView = value));
            subMenu.AddDescription(menu, OptimizationItem, "Performance Optimization Description".ToDialogText());
        });
    }
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
        });
    }

    private static TextMenuExt.SubMenu CreateMoreOptionsSubMenu(TextMenu menu) {
        return new TextMenuExt.SubMenu("More Options".ToDialogText(), false).Apply(subMenu => {
            subMenu.Add(new TextMenu.OnOff("Spawn Point".ToDialogText(), TasHelperSettings.UsingSpawnPoint).Change((value) => TasHelperSettings.UsingSpawnPoint = value));
            subMenu.Add(new TextMenuExt.IntSlider("Current Spawn Point Opacity".ToDialogText(), 1, 9, TasHelperSettings.CurrentSpawnPointOpacity).Change((value) => TasHelperSettings.CurrentSpawnPointOpacity = value));
            subMenu.Add(new TextMenuExt.IntSlider("Other Spawn Point Opacity".ToDialogText(), 0, 9, TasHelperSettings.OtherSpawnPointOpacity).Change((value) => TasHelperSettings.OtherSpawnPointOpacity = value));
            TextMenu.Item EntityActivatorReminderItem;
            subMenu.Add(EntityActivatorReminderItem = new TextMenu.OnOff("Entity Activator Reminder".ToDialogText(), TasHelperSettings.EntityActivatorReminder).Change((value) => TasHelperSettings.EntityActivatorReminder = value));
            subMenu.AddDescription(menu, EntityActivatorReminderItem, "Entity Activator Reminder Description".ToDialogText());
            subMenu.Add(new TextMenu.OnOff("Enable Pixel Grid".ToDialogText(), TasHelperSettings.EnablePixelGrid).Change(value => TasHelperSettings.EnablePixelGrid = value));
            subMenu.Add(new TextMenuExt.IntSlider("Pixel Grid Width".ToDialogText(), 0, 50, TasHelperSettings.PixelGridWidth).Change(value => TasHelperSettings.PixelGridWidth = value));
            subMenu.Add(new TextMenuExt.IntSlider("Pixel Grid Opacity".ToDialogText(), 1, 10, TasHelperSettings.PixelGridOpacity).Change(value => TasHelperSettings.PixelGridOpacity = value));
            subMenu.Add(new TextMenu.OnOff("Camera Target".ToDialogText(), TasHelperSettings.UsingCameraTarget).Change(value => TasHelperSettings.UsingCameraTarget = value));
            subMenu.Add(new TextMenuExt.IntSlider("Camera Target Vector Opacity".ToDialogText(), 1, 9, TasHelperSettings.CameraTargetLinkOpacity).Change(value => TasHelperSettings.CameraTargetLinkOpacity = value));
        });
    }


    /*
    private static IEnumerable<KeyValuePair<SpinnerMainSwitchModes, string>> CreateSpinnerMainSwitchOptions() {
        // no longer use this, now we use {on, off} two state in menu (SpinnerEnabled), but call it SpinnerMainSwitch as well
        // and use {Off, OnlyDefault, AllowAll} three states for hotkey (the real SpinnerMainSwitch)
        return new List<KeyValuePair<SpinnerMainSwitchModes, string>> {
            new(SpinnerMainSwitchModes.Off, "Spinner Main Switch Mode Off".ToDialogText()),
            new(SpinnerMainSwitchModes.OnlyDefault, "Spinner Main Switch Mode Only Default".ToDialogText()),
            new(SpinnerMainSwitchModes.AllowAll, "Spinner Main Switch Mode Allow All".ToDialogText()),
        };
    }
    */
    internal static IEnumerable<KeyValuePair<UsingNotInViewColorModes, string>> CreateUsingNotInViewColorOptions() {
        return new List<KeyValuePair<UsingNotInViewColorModes, string>> {
            new(UsingNotInViewColorModes.Off, "NotInView Color Modes Off".ToDialogText()),
            new(UsingNotInViewColorModes.WhenUsingInViewRange, "NotInView Color Modes When".ToDialogText()),
            new(UsingNotInViewColorModes.Always, "NotInView Color Modes Always".ToDialogText()),
        };
    }
    private static IEnumerable<KeyValuePair<CountdownModes, string>> CreateCountdownOptions() {
        return new List<KeyValuePair<CountdownModes, string>> {
            new(CountdownModes.Off, "Countdown Mode Off".ToDialogText()),
            new(CountdownModes._3fCycle, "Countdown Mode 3f Cycle".ToDialogText()),
            new(CountdownModes._15fCycle, "Countdown Mode 15f Cycle".ToDialogText()),
        };
    }
    private static IEnumerable<KeyValuePair<CountdownFonts, string>> CreateCountdownFontOptions() {
        return new List<KeyValuePair<CountdownFonts, string>> {
            new(CountdownFonts.PixelFont, "Pixel Font".ToDialogText()),
            new(CountdownFonts.HiresFont, "Hires Font".ToDialogText()),
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

    private static TextMenu.Item mainItem;

    private static List<TextMenu.Item> disabledItems = new List<TextMenu.Item>();

    public static void CreateMenu(EverestModule everestModule, TextMenu menu, bool inGame) {
        menu.Add(mainItem = new TextMenu.OnOff("Enabled".ToDialogText(), TasHelperSettings.Enabled).Change((value) => { TasHelperSettings.Enabled = value; UpdateEnableItems(value,everestModule,menu,inGame); }));
        UpdateEnableItems(TasHelperSettings.Enabled, everestModule, menu, inGame);
    }
    private static void UpdateEnableItems(bool enable, EverestModule everestModule, TextMenu menu, bool inGame) {
        if (enable) {
            // we create all other menus on value change
            // so the values in these submenus will be correct
            TasHelperSettings.Awake(true);
            // prevent fake die by Ctrl+E
            TextMenu.Item colorItem = CreateColorCustomizationSubMenu(menu, inGame);
            TextMenu.Item countdownItem = CreateCountdownSubMenu(menu);
            TextMenu.Item loadrangeItem = CreateLoadRangeSubMenu(menu);
            TextMenu.Item simpspinnerItem = CreateSimplifiedSpinnerSubMenu(menu);
            TextMenu.Item hotkeysItem = CreateHotkeysSubMenu(everestModule, menu);
            TextMenu.Item moreoptionItem = CreateMoreOptionsSubMenu(menu);
            int N = menu.IndexOf(mainItem);
            menu.Insert(N+1, colorItem);
            menu.Insert(N+2, countdownItem);
            menu.Insert(N+3, loadrangeItem);
            menu.Insert(N+4, simpspinnerItem);
            menu.Insert(N+5, hotkeysItem);
            hotkeysItem.AddDescription(menu, "Hotkey Description".ToDialogText());
            menu.Insert(N+7,moreoptionItem);
            disabledItems = new List<TextMenu.Item>() { colorItem, countdownItem, loadrangeItem, simpspinnerItem, hotkeysItem, moreoptionItem };
        }
        else {
            // we remove items, to prevent unexcepted setting value changes
            foreach (TextMenu.Item item in disabledItems) {
                menu.Remove(item);
            }
            disabledItems.Clear();
        }
    }

}

