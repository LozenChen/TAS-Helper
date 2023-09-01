using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using static Celeste.Mod.TASHelper.Module.TASHelperSettings;

namespace Celeste.Mod.TASHelper.Module.Menu;

internal static class TASHelperMenu {
    internal static string ToDialogText(this string input) => Dialog.Clean("TAS_HELPER_" + input.ToUpper().Replace(" ", "_"));

    private static EaseInOptionSubMenuCountExt CreateColorCustomizationSubMenu(TextMenu menu, bool inGame) {
        EaseInOptionSubMenuCountExt ColorCustomizationItem = new EaseInOptionSubMenuCountExt("Color Customization".ToDialogText());
        ColorCustomizationItem.OnLeave += () => ColorCustomizationItem.MenuIndex = 0;
        ColorCustomizationItem.Add("Color Customization Finished".ToDialogText(), new List<TextMenu.Item>());
        ColorCustomizationItem.Add("Color Customization OnOff".ToDialogText(), CustomColors.CreateColorCustomization_PageOnOff(menu, inGame));
        ColorCustomizationItem.Add("Color Customization Spinner Color".ToDialogText(), CustomColors.CreateColorCustomization_PageSpinnerColor(menu, inGame));
        ColorCustomizationItem.Add("Color Customization Other".ToDialogText(), CustomColors.CreateColorCustomization_PageOther(menu, inGame));
        return ColorCustomizationItem.Apply(item => item.IncludeWidthInMeasurement = false);
    }

    private static EaseInSubMenu CreatePredictFutureSubMenu(TextMenu menu) {
        return new EaseInSubMenu("Predictor".ToDialogText(), false).Apply(subMenu => {
            TextMenu.Item PredictItem;
            subMenu.Add(PredictItem = new TextMenu.OnOff("Predict Future Main Button".ToDialogText(), TasHelperSettings.PredictFuture).Change((value) => TasHelperSettings.PredictFuture = value));
            subMenu.AddDescription(menu, PredictItem, "Predict Future Description".ToDialogText());
            subMenu.Add(new TextMenuExt.IntSlider("Future Length".ToDialogText(), 1, 999, TasHelperSettings.FutureLength).Change((value) => TasHelperSettings.FutureLength = value));

            subMenu.Add(new TextMenu.OnOff("Predict On File Change".ToDialogText(), TasHelperSettings.PredictOnFileChange).Change(value => TasHelperSettings.PredictOnFileChange = value));
            subMenu.Add(new TextMenu.OnOff("Predict On Hotkey Pressed".ToDialogText(), TasHelperSettings.PredictOnHotkeyPressed).Change(value => TasHelperSettings.PredictOnHotkeyPressed = value));
            subMenu.Add(new TextMenu.OnOff("Predict On Frame Step".ToDialogText(), TasHelperSettings.PredictOnFrameStep).Change(value => TasHelperSettings.PredictOnFrameStep = value));
        });
    }


    private static EaseInSubMenu CreateCountdownSubMenu(TextMenu menu) {
        return new EaseInSubMenu("Countdown".ToDialogText(), false).Apply(subMenu => {
            TextMenu.Item CountdownModeItem;
            subMenu.Add(CountdownModeItem = new TextMenuExt.EnumerableSlider<CountdownModes>("Countdown Mode".ToDialogText(), CreateCountdownOptions(),
                    TasHelperSettings.CountdownMode).Change(value => TasHelperSettings.CountdownMode = value));
            subMenu.AddDescription(menu, CountdownModeItem, "Countdown Mode Description".ToDialogText());
            TextMenu.Item CountdownBoostItem;
            subMenu.Add(CountdownBoostItem = new TextMenu.OnOff("Countdown Boost".ToDialogText(), TasHelperSettings.CountdownBoost).Change(value => TasHelperSettings.CountdownBoost = value));
            subMenu.AddDescription(menu, CountdownBoostItem, "Countdown Boost Description".ToDialogText());
            subMenu.Add(new TextMenuExt.EnumerableSlider<CountdownFonts>("Font".ToDialogText(), CreateCountdownFontOptions(),
                TasHelperSettings.CountdownFont).Change(value => TasHelperSettings.CountdownFont = value));
            subMenu.Add(new TextMenuExt.IntSlider("Hires Font Size".ToDialogText(), 1, 20, TasHelperSettings.HiresFontSize).Change(value => TasHelperSettings.HiresFontSize = value));
            subMenu.Add(new TextMenuExt.IntSlider("Hires Font Stroke".ToDialogText(), 0, 20, TasHelperSettings.HiresFontStroke).Change(value => TasHelperSettings.HiresFontStroke = value));
            TextMenu.Item OptimizationItem;
            subMenu.Add(OptimizationItem = new TextMenu.OnOff("Performance Optimization".ToDialogText(), TasHelperSettings.DoNotRenderWhenFarFromView).Change(value => TasHelperSettings.DoNotRenderWhenFarFromView = value));
            subMenu.AddDescription(menu, OptimizationItem, "Performance Optimization Description".ToDialogText());
        });
    }
    private static EaseInSubMenu CreateLoadRangeSubMenu(TextMenu menu) {
        return new EaseInSubMenu("Load Range".ToDialogText(), false).Apply(subMenu => {
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

    private static EaseInSubMenu CreateSimplifiedSpinnerSubMenu(TextMenu menu) {
        return new EaseInSubMenu("Simplified Spinners".ToDialogText(), false).Apply(subMenu => {
            subMenu.Add(new TextMenu.OnOff("Enabled".ToDialogText(), TasHelperSettings.EnableSimplifiedSpinner).Change(value => TasHelperSettings.EnableSimplifiedSpinner = value));
            subMenu.Add(new TextMenuExt.EnumerableSlider<ClearSpritesMode>("Clear Spinner Sprites".ToDialogText(), CreateClearSpritesModeOptions(), TasHelperSettings.EnforceClearSprites).Change(value => TasHelperSettings.EnforceClearSprites = value));
            subMenu.Add(new TextMenuExt.IntSlider("Spinner Filler Opacity".ToDialogText(), 0, 9, TasHelperSettings.SpinnerFillerOpacity_Collidable).Change(value => TasHelperSettings.SpinnerFillerOpacity_Collidable = value));
            subMenu.Add(new TextMenuExt.IntSlider("Spinner Filler Opacity Extra".ToDialogText(), 0, 9, TasHelperSettings.SpinnerFillerOpacity_Uncollidable).Change(value => TasHelperSettings.SpinnerFillerOpacity_Uncollidable = value));
            subMenu.Add(new TextMenu.OnOff("Spinner_Ignore_TAS_UncollidableAlpha".ToDialogText(), TasHelperSettings.Ignore_TAS_UnCollidableAlpha).Change(value => TasHelperSettings.Ignore_TAS_UnCollidableAlpha = value));
        });
    }

    private static EaseInSubMenu CreateHotkeysSubMenu(EverestModule everestModule, TextMenu menu) {
        return new EaseInSubMenu("Hotkeys".ToDialogText(), false).Apply(subMenu => {
            TextMenu.Item MainSwitchStateItem;
            EaseInSubHeaderExtPub StateDescription = new EaseInSubHeaderExtPub("Configure At State All".ToDialogText(), false, menu) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            subMenu.Add(MainSwitchStateItem = new TextMenuExt.EnumerableSlider<bool>("Main Switch State".ToDialogText(), CreateMainSwitchStatesOptions(), TasHelperSettings.MainSwitchThreeStates).Change(value => { TasHelperSettings.MainSwitchThreeStates = value; StateDescription.FadeVisible = value; StateDescription.uneasedAlpha = StateDescription.Alpha; }));
            subMenu.Add(StateDescription);
            MainSwitchStateItem.OnEnter += () => StateDescription.FadeVisible = TasHelperSettings.MainSwitchThreeStates && true;
            MainSwitchStateItem.OnLeave += () => StateDescription.FadeVisible = false;
            subMenu.Add(new TextMenu.OnOff("Main Switch Visualize".ToDialogText(), TasHelperSettings.MainSwitchStateVisualize).Change(value => TasHelperSettings.MainSwitchStateVisualize = value));
            subMenu.Add(new TextMenu.OnOff("Main Switch Prevent".ToDialogText(), TasHelperSettings.AllowEnableModWithMainSwitch).Change(value => TasHelperSettings.AllowEnableModWithMainSwitch = value));

            subMenu.Add(new TextMenu.Button(Dialog.Clean("options_keyconfig")).Pressed(() => {
                subMenu.Focused = false;
                KeyboardConfigUI keyboardConfig = new ModuleSettingsKeyboardConfigUIExt(everestModule) {
                    OnClose = () => { subMenu.Focused = true; TH_Hotkeys.HotkeyInitialize(); }
                };

                Engine.Scene.Add(keyboardConfig);
                Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
            }));

            subMenu.Add(new TextMenu.Button(Dialog.Clean("options_btnconfig")).Pressed(() => {
                subMenu.Focused = false;
                ButtonConfigUI buttonConfig = new ModuleSettingsButtonConfigUI(everestModule) {
                    OnClose = () => { subMenu.Focused = true; TH_Hotkeys.HotkeyInitialize(); }
                };

                Engine.Scene.Add(buttonConfig);
                Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
            }));
        });
    }

    private static EaseInSubMenu CreateMoreOptionsSubMenu(TextMenu menu) {
        return new EaseInSubMenu("More Options".ToDialogText(), false).Apply(subMenu => {
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
            subMenu.Add(new TextMenu.OnOff("FireBall Track".ToDialogText(), TasHelperSettings.UsingFireBallTrack).Change(value => TasHelperSettings.UsingFireBallTrack = value));
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
    private static IEnumerable<KeyValuePair<bool, string>> CreateMainSwitchStatesOptions() {
        return new List<KeyValuePair<bool, string>> {
           new(true, "Main Switch Three States".ToDialogText()),
           new(false,"Main Switch Two States".ToDialogText()),
        };
    }

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

    internal static TextMenu.Item mainItem;

    private static List<TextMenu.Item> disabledItems = new List<TextMenu.Item>();

    public static void CreateMenu(EverestModule everestModule, TextMenu menu, bool inGame) {
        menu.Add(mainItem = new TextMenu.OnOff("Enabled".ToDialogText(), TasHelperSettings.Enabled).Change((value) => { TasHelperSettings.Enabled = value; UpdateEnableItems(value, true, everestModule, menu, inGame); }));
        UpdateEnableItems(TasHelperSettings.Enabled, false, everestModule, menu, inGame);
    }
    private static void UpdateEnableItems(bool enable, bool fromChange, EverestModule everestModule, TextMenu menu, bool inGame) {
        if (enable) {
            // we create all other menus on value change
            // so the values in these submenus will be correct
            if (fromChange) {
                TasHelperSettings.Awake(true);
                // prevent fake die by Ctrl+E
            }
            foreach (TextMenu.Item item in disabledItems) {
                menu.Remove(item);
            }
            disabledItems.Clear();
            EaseInOptionSubMenuCountExt colorItem = CreateColorCustomizationSubMenu(menu, inGame);
            EaseInSubMenu countdownItem = CreateCountdownSubMenu(menu);
            EaseInSubMenu loadrangeItem = CreateLoadRangeSubMenu(menu);
            EaseInSubMenu simpspinnerItem = CreateSimplifiedSpinnerSubMenu(menu);
            EaseInSubMenu predictItem = CreatePredictFutureSubMenu(menu);
            EaseInSubMenu moreoptionItem = CreateMoreOptionsSubMenu(menu);
            EaseInSubMenu hotkeysItem = CreateHotkeysSubMenu(everestModule, menu);
            int N = menu.IndexOf(mainItem);
            menu.Insert(N + 1, colorItem);
            menu.Insert(N + 2, countdownItem);
            menu.Insert(N + 3, loadrangeItem);
            menu.Insert(N + 4, simpspinnerItem);
            menu.Insert(N + 5, predictItem);
            menu.Insert(N + 6, moreoptionItem);
            menu.Insert(N + 7, hotkeysItem);
            hotkeysItem.AddDescription(menu, "Hotkey Description".ToDialogText());
            disabledItems = new List<TextMenu.Item>() { colorItem, countdownItem, loadrangeItem, simpspinnerItem, predictItem, moreoptionItem, hotkeysItem };
            foreach (IEaseInItem item in disabledItems) {
                item.Initialize();
            }
        }
        else {
            if (fromChange) {
                TasHelperSettings.Sleep();
                // prevent fake die by Ctrl+E
            }
            // we remove items, to prevent unexcepted setting value changes
            foreach (IEaseInItem item in disabledItems) {
                item.FadeVisible = false;
            }
        }
    }

}

public interface IEaseInItem {
    public void Initialize();
    public bool FadeVisible { get; set; }
}
public class EaseInSubMenu : TextMenuExt.SubMenu, IEaseInItem {
    private float alpha;
    private float unEasedAlpha;

    public void Initialize() {
        alpha = unEasedAlpha = 0f;
        Visible = FadeVisible = true;
    }
    public bool FadeVisible { get; set; }
    public EaseInSubMenu(string label, bool enterOnSelect) : base(label, enterOnSelect) {
        alpha = unEasedAlpha = 1f;
        FadeVisible = Visible = true;
    }

    public override float Height() => MathHelper.Lerp(-Container.ItemSpacing, base.Height(), alpha);

    public override void Update() {
        base.Update();

        float targetAlpha = FadeVisible ? 1 : 0;
        if (Math.Abs(unEasedAlpha - targetAlpha) > 0.001f) {
            unEasedAlpha = Calc.Approach(unEasedAlpha, targetAlpha, Engine.RawDeltaTime * 3f);
            alpha = FadeVisible ? Ease.SineOut(unEasedAlpha) : Ease.SineIn(unEasedAlpha);
        }

        Visible = alpha != 0;
    }

    public override void Render(Vector2 position, bool highlighted) {
        float c = Container.Alpha;
        Container.Alpha = alpha;
        base.Render(position, highlighted);
        Container.Alpha = c;
    }
}

public class EaseInOptionSubMenuCountExt : OptionSubMenuCountExt, IEaseInItem {
    private float alpha;
    private float unEasedAlpha;

    public void Initialize() {
        alpha = unEasedAlpha = 0f;
        Visible = FadeVisible = true;
    }
    public bool FadeVisible { get; set; }
    public EaseInOptionSubMenuCountExt(string label) : base(label) {
        alpha = unEasedAlpha = 1f;
        FadeVisible = Visible = true;
    }
    public override float Height() => MathHelper.Lerp(-Container.ItemSpacing, base.Height(), alpha);

    public override void Update() {
        base.Update();

        float targetAlpha = FadeVisible ? 1 : 0;
        if (Math.Abs(unEasedAlpha - targetAlpha) > 0.001f) {
            unEasedAlpha = Calc.Approach(unEasedAlpha, targetAlpha, Engine.RawDeltaTime * 3f);
            alpha = FadeVisible ? Ease.SineOut(unEasedAlpha) : Ease.SineIn(unEasedAlpha);
        }

        Visible = alpha != 0;
    }
    public override void Render(Vector2 position, bool highlighted) {
        float c = Container.Alpha;
        Container.Alpha = alpha;
        base.Render(position, highlighted);
        Container.Alpha = c;
    }
}


[Tracked(false)]
public class ModuleSettingsKeyboardConfigUIExt : ModuleSettingsKeyboardConfigUI {

    public ModuleSettingsKeyboardConfigUIExt(EverestModule module) : base(module) {
    }

    public override void Reload(int index = -1) {
        if (Module == null)
            return;

        Clear();
        Add(new Header(Dialog.Clean("KEY_CONFIG_TITLE")));
        Add(new InputMappingInfo(false));

        object settings = Module._Settings;

        // The default name prefix.
        string typeName = Module.SettingsType.Name.ToLowerInvariant();
        if (typeName.EndsWith("settings"))
            typeName = typeName.Substring(0, typeName.Length - 8);
        string nameDefaultPrefix = $"modoptions_{typeName}_";

        SettingInGameAttribute attribInGame;

        foreach (PropertyInfo prop in Module.SettingsType.GetProperties()) {
            if ((attribInGame = prop.GetCustomAttribute<SettingInGameAttribute>()) != null &&
                attribInGame.InGame != Engine.Scene is Level)
                continue;

            if (prop.GetCustomAttribute<SettingIgnoreAttribute>() != null)
                continue;

            if (!prop.CanRead || !prop.CanWrite)
                continue;

            if (typeof(ButtonBinding).IsAssignableFrom(prop.PropertyType)) {
                if (!(prop.GetValue(settings) is ButtonBinding binding))
                    continue;

                string name = prop.GetCustomAttribute<SettingNameAttribute>()?.Name ?? $"{nameDefaultPrefix}{prop.Name.ToLowerInvariant()}";
                name = name.DialogCleanOrNull() ?? (prop.Name.ToLowerInvariant().StartsWith("button") ? prop.Name.Substring(6) : prop.Name).SpacedPascalCase();

                DefaultButtonBindingAttribute defaults = prop.GetCustomAttribute<DefaultButtonBindingAttribute>();

                Bindings.Add(new ButtonBindingEntry(binding, defaults));

                string subheader = prop.GetCustomAttribute<SettingSubHeaderAttribute>()?.SubHeader;
                if (subheader != null)
                    Add(new TextMenuExt.SubHeaderExt(subheader.DialogCleanOrNull() ?? subheader) {
                        TextColor = Color.Gray,
                        Offset = new Vector2(0f, -60f),
                        HeightExtra = 60f
                    });

                AddMapForceLabel(name, binding.Binding);

                string description = prop.GetCustomAttribute<SettingDescriptionHardcodedAttribute>()?.description();
                if (description != null)
                    Add(new TextMenuExt.SubHeaderExt(description) {
                        TextColor = Color.Gray,
                        Offset = new Vector2(0f, -90f),
                        HeightExtra = 60f
                    });
            }
        }

        Add(new SubHeader(""));
        Add(new Button(Dialog.Clean("KEY_CONFIG_RESET")) {
            IncludeWidthInMeasurement = false,
            AlwaysCenter = true,
            OnPressed = () => ResetPressed()
        });

        if (index >= 0)
            Selection = index;
    }
}

public class EaseInSubHeaderExtPub : TextMenuExt.SubHeaderExt {
    // same as EaseInSubHeaderExt, except make uneasedAlpha public to me
    public float uneasedAlpha;

    private TextMenu containingMenu;

    public bool FadeVisible { get; set; } = true;


    public EaseInSubHeaderExtPub(string title, bool initiallyVisible, TextMenu containingMenu, string icon = null)
        : base(title, icon) {
        this.containingMenu = containingMenu;
        FadeVisible = initiallyVisible;
        Alpha = FadeVisible ? 1 : 0;
        uneasedAlpha = Alpha;
    }

    public override float Height() {
        return MathHelper.Lerp(0f - containingMenu.ItemSpacing, base.Height(), Alpha);
    }

    public override void Update() {
        base.Update();
        float num = FadeVisible ? 1 : 0;
        if (uneasedAlpha != num) {
            uneasedAlpha = Calc.Approach(uneasedAlpha, num, Engine.RawDeltaTime * 3f);
            if (FadeVisible) {
                Alpha = Ease.SineOut(uneasedAlpha);
            }
            else {
                Alpha = Ease.SineIn(uneasedAlpha);
            }
        }

        Visible = Alpha != 0f;
    }
}