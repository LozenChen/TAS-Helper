using Celeste.Mod.TASHelper.Gameplay;
using Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;
using Celeste.Mod.TASHelper.Utils;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System.Reflection;
using static Celeste.Mod.TASHelper.Module.TASHelperSettings;
using CMCore = Celeste.Mod.Core;

namespace Celeste.Mod.TASHelper.Module.Menu;

internal static class TASHelperMenu {
    internal static string ToDialogText(this string input) => Dialog.Clean("TAS_HELPER_" + input.ToUpper().Replace(" ", "_")).Replace("\\S", " ");
    private static EaseInOptionSubMenuExt CreateColorCustomizationSubMenu(TextMenu menu, bool inGame) {
        EaseInOptionSubMenuExt ColorCustomizationItem = new EaseInOptionSubMenuExt("Color Customization".ToDialogText());
        ColorCustomizationItem.OnLeave += () => ColorCustomizationItem.MenuIndex = 0;
        ColorCustomizationItem.Add("Color Customization Finished".ToDialogText(), new List<TextMenu.Item>());
        ColorCustomizationItem.Add("Color Customization OnOff".ToDialogText(), CustomColors.Create_PageOnOff(menu, inGame));
        ColorCustomizationItem.Add("Color Customization Spinner Color".ToDialogText(), CustomColors.Create_PageSpinnerColor(menu, inGame));
        ColorCustomizationItem.Add("Color Customization Predictor".ToDialogText(), CustomColors.Create_PagePredictor(menu, inGame));
        ColorCustomizationItem.Add("Color Customization Other".ToDialogText(), CustomColors.Create_PageOther(menu, inGame));
        return ColorCustomizationItem.Apply(item => item.IncludeWidthInMeasurement = false);
    }

    private static EaseInOptionSubMenuExt CreatePredictorSubMenu(TextMenu menu, bool inGame) {
        EaseInOptionSubMenuExt PredictorItem = new EaseInOptionSubMenuExt("Predictor".ToDialogText());
        PredictorItem.OnLeave += () => PredictorItem.MenuIndex = 0;
        PredictorItem.Add("Predictor Finished".ToDialogText(), new List<TextMenu.Item>());
        PredictorItem.Add("Predictor OnOff".ToDialogText(), PredictorMenu.Create_PageOnOff(menu, inGame));
        PredictorItem.Add("Predictor Keyframe 1".ToDialogText(), PredictorMenu.Create_PageKeyframe_1(menu, inGame));
        PredictorItem.Add("Predictor Keyframe 2".ToDialogText(), PredictorMenu.Create_PageKeyframe_2(menu, inGame));
        PredictorItem.Add("Predictor Style".ToDialogText(), PredictorMenu.Create_PageStyle(menu, inGame));
        PredictorItem.Add("Predictor Other".ToDialogText(), PredictorMenu.Create_PageOther(menu, inGame));
        return PredictorItem.Apply(item => item.IncludeWidthInMeasurement = false);
    }

    private static EaseInOptionSubMenuExt CreateAutoWatchSubMenu(TextMenu menu, bool inGame) {
        EaseInOptionSubMenuExt AutoWatchItem = new EaseInOptionSubMenuExt("Auto Watch".ToDialogText());
        AutoWatchItem.OnLeave += () => {
            AutoWatchItem.MenuIndex = 0;
            if (Engine.Scene is Level level) {
                level.OnEndOfFrame += () => CoreLogic.OnConfigChange(TasHelperSettings.AutoWatchEnable);
            }
        };
        AutoWatchItem.Add("Auto Watch Finished".ToDialogText(), new List<TextMenu.Item>());
        AutoWatchItem.Add("Auto Watch Page OnOff".ToDialogText(), AutoWatchMenu.Create_Page_OnOff(menu));
        AutoWatchItem.Add("Auto Watch Page 2".ToDialogText(), AutoWatchMenu.Create_Page2(menu));
        return AutoWatchItem.Apply(item => item.IncludeWidthInMeasurement = false);
    }

    private static EaseInSubMenu CreateCountdownSubMenu(TextMenu menu) {
        return new EaseInSubMenu("Countdown".ToDialogText(), false).Apply(subMenu => {
            TextMenu.Item CountdownModeItem;
            EaseInSubHeaderExtVarTitle descriptionText = new("Countdown Exact Group Description".ToDialogText(), "Countdown Mode Description".ToDialogText(), false, menu, "", TasHelperSettings.CountdownMode is CountdownModes.ExactGroupMod3 or CountdownModes.ExactGroupMod15) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            subMenu.Add(CountdownModeItem = new EnumerableSliderExt<CountdownModes>("Countdown Mode".ToDialogText(), CreateCountdownOptions(),
                    TasHelperSettings.CountdownMode).Change(value => {
                        TasHelperSettings.CountdownMode = value;
                        descriptionText.SetTitle(TasHelperSettings.CountdownMode is CountdownModes.ExactGroupMod3 or CountdownModes.ExactGroupMod15);
                    }));
            subMenu.Add(descriptionText);
            CountdownModeItem.OnEnter += () => descriptionText.FadeVisible = true;
            CountdownModeItem.OnLeave += () => descriptionText.FadeVisible = false;


            TextMenu.Item CountdownBoostItem;
            subMenu.Add(CountdownBoostItem = new TextMenu.OnOff("Countdown Boost".ToDialogText(), TasHelperSettings.CountdownBoost).Change(value => TasHelperSettings.CountdownBoost = value));
            subMenu.AddDescription(menu, CountdownBoostItem, "Countdown Boost Description".ToDialogText());
            subMenu.Add(new EnumerableSliderExt<CountdownFonts>("Font".ToDialogText(), CreateCountdownFontOptions(),
                TasHelperSettings.CountdownFont).Change(value => TasHelperSettings.CountdownFont = value));
            subMenu.Add(new TextMenu.OnOff("Darken When Uncollidable".ToDialogText(), TasHelperSettings.DarkenWhenUncollidable).Change(value => TasHelperSettings.DarkenWhenUncollidable = value));
            subMenu.Add(new IntSliderExt("Hires Font Size".ToDialogText(), 1, 20, TasHelperSettings.HiresFontSize).Change(value => TasHelperSettings.HiresFontSize = value));
            subMenu.Add(new IntSliderExt("Hires Font Stroke".ToDialogText(), 0, 20, TasHelperSettings.HiresFontStroke).Change(value => TasHelperSettings.HiresFontStroke = value));
            /*
            TextMenu.Item OptimizationItem;
            subMenu.Add(OptimizationItem = new TextMenu.OnOff("Performance Optimization".ToDialogText(), TasHelperSettings.DoNotRenderWhenFarFromView).Change(value => TasHelperSettings.DoNotRenderWhenFarFromView = value));
            subMenu.AddDescription(menu, OptimizationItem, "Performance Optimization Description".ToDialogText());
            */
            subMenu.Add(new HLine(Color.Gray));
        });
    }
    private static EaseInSubMenu CreateLoadRangeSubMenu(TextMenu menu) {
        return new EaseInSubMenu("Load Range".ToDialogText(), false).Apply(subMenu => {
            TextMenu.Item LoadRangeModeItem;
            subMenu.Add(LoadRangeModeItem = new EnumerableSliderExt<LoadRangeModes>("Load Range Mode".ToDialogText(), CreateLoadRangeOptions(),
                TasHelperSettings.LoadRangeMode).Change(value => TasHelperSettings.LoadRangeMode = value));
            subMenu.AddDescription(menu, LoadRangeModeItem, "Load Range Description".ToDialogText());
            TextMenu.Item InViewRangeWidthItem;
            subMenu.Add(InViewRangeWidthItem = new IntSliderExt("In View Range Width".ToDialogText(), 0, 32, TasHelperSettings.InViewRangeWidth).Change(value => TasHelperSettings.InViewRangeWidth = value));
            subMenu.AddDescription(menu, InViewRangeWidthItem, "In View Description".ToDialogText());
            subMenu.Add(new IntSliderExt("Near Player Range Width".ToDialogText(), 1, 16, TasHelperSettings.NearPlayerRangeWidth).Change(value => TasHelperSettings.NearPlayerRangeWidth = value));
            subMenu.Add(new IntSliderExt("Load Range Opacity".ToDialogText(), 0, 9, TasHelperSettings.LoadRangeOpacity).Change(value => TasHelperSettings.LoadRangeOpacity = value));
            TextMenu.Item CameraZoomItem;
            subMenu.Add(CameraZoomItem = new TextMenu.OnOff("Apply Camera Zoom".ToDialogText(), TasHelperSettings.ApplyCameraZoom).Change(value => TasHelperSettings.ApplyCameraZoom = value));
            subMenu.AddDescription(menu, CameraZoomItem, "Apply Camera Zoom Description".ToDialogText());
            TextMenu.Item LRCItem;
            subMenu.Add(LRCItem = new EnumerableSliderExt<LoadRangeColliderModes>("Load Range Collider".ToDialogText(), CreateEnableLoadRangeColliderOptions(),
                TasHelperSettings.LoadRangeColliderMode).Change(value => TasHelperSettings.LoadRangeColliderMode = value));
            subMenu.AddDescription(menu, LRCItem, "LRC Description".ToDialogText());
            subMenu.Add(new HLine(Color.Gray));
        });
    }

    private static EaseInSubMenu CreateSimplifiedGraphicSubMenu(TextMenu menu) {
        return new EaseInSubMenu("Simplified Graphics".ToDialogText(), false).Apply(subMenu => {
            subMenu.Add(new TextMenu.OnOff("Simplified Spinners".ToDialogText(), TasHelperSettings.EnableSimplifiedSpinner).Change(value => TasHelperSettings.EnableSimplifiedSpinner = value));
            subMenu.Add(new EnumerableSliderExt<SimplifiedGraphicsMode>("Clear Spinner Sprites".ToDialogText(), CreateSimplifiedGraphicsModeOptions(), TasHelperSettings.EnforceClearSprites).Change(value => TasHelperSettings.EnforceClearSprites = value));
            subMenu.Add(new IntSliderExt("Spinner Filler Opacity".ToDialogText(), 0, 9, TasHelperSettings.SpinnerFillerOpacity_Collidable).Change(value => TasHelperSettings.SpinnerFillerOpacity_Collidable = value));
            subMenu.Add(new IntSliderExt("Spinner Filler Opacity Extra".ToDialogText(), 0, 9, TasHelperSettings.SpinnerFillerOpacity_Uncollidable).Change(value => TasHelperSettings.SpinnerFillerOpacity_Uncollidable = value));
            subMenu.Add(new TextMenu.OnOff("Spinner Dashed Border".ToDialogText(), TasHelperSettings.SimplifiedSpinnerDashedBorder).Change(value => TasHelperSettings.SimplifiedSpinnerDashedBorder = value));
            subMenu.Add(new TextMenu.OnOff("Spinner_Ignore_TAS_UncollidableAlpha".ToDialogText(), TasHelperSettings.Ignore_TAS_UnCollidableAlpha).Change(value => TasHelperSettings.Ignore_TAS_UnCollidableAlpha = value));
            subMenu.Add(new TextMenu.OnOff("ACH For Spinner".ToDialogText(), TasHelperSettings.ApplyActualCollideHitboxForSpinner).Change(value => TasHelperSettings.ApplyActualCollideHitboxForSpinner = value));
            subMenu.Add(new HLine(Color.Gray));
            TextMenu.Item simplifiedLightning;
            subMenu.Add(simplifiedLightning = new EnumerableSliderExt<SimplifiedGraphicsMode>("Simplified Lightning".ToDialogText(), CreateSimplifiedGraphicsModeOptions(), TasHelperSettings.EnableSimplifiedLightningMode).Change(value => TasHelperSettings.EnableSimplifiedLightningMode = value));
            subMenu.AddDescription(menu, simplifiedLightning, "Simplified Lightning Description".ToDialogText());
            TextMenu.Item highlightItem;
            subMenu.Add(highlightItem = new TextMenu.OnOff("Highlight Load Unload".ToDialogText(), TasHelperSettings.HighlightLoadUnload).Change(value => TasHelperSettings.HighlightLoadUnload = value));
            subMenu.AddDescription(menu, highlightItem, "Highlight Description".ToDialogText());
            TextMenu.Item ACH_LightningItem;
            subMenu.Add(ACH_LightningItem = new TextMenu.OnOff("ACH For Lightning".ToDialogText(), TasHelperSettings.ApplyActualCollideHitboxForLightning).Change(value => TasHelperSettings.ApplyActualCollideHitboxForLightning = value));
            subMenu.AddDescription(menu, ACH_LightningItem, "ACH Warn Lightning".ToDialogText());
            subMenu.Add(new HLine(Color.Gray));
            TextMenu.Item simplifiedTrigger;
            subMenu.Add(simplifiedTrigger = new EnumerableSliderExt<SimplifiedGraphicsMode>("Simplified Triggers".ToDialogText(), CreateSimplifiedGraphicsModeOptions(), TasHelperSettings.EnableSimplifiedTriggersMode).Change(value => TasHelperSettings.EnableSimplifiedTriggersMode = value));
            subMenu.Add(new TextMenu.OnOff("Hide Camera Trigger".ToDialogText(), TasHelperSettings.HideCameraTriggers).Change(value => { TasHelperSettings.HideCameraTriggers = value; SimplifiedTrigger.OnHideCameraChange(value); }));
            subMenu.Add(new TextMenu.OnOff("Hide Gold Berry".ToDialogText(), TasHelperSettings.HideGoldBerryCollectTrigger).Change(value => { TasHelperSettings.HideGoldBerryCollectTrigger = value; SimplifiedTrigger.OnHideBerryChange(value); }));
            subMenu.Add(new HLine(Color.Gray));
        });
    }

    private static EaseInSubMenu CreateHotkeysSubMenu(EverestModule everestModule, TextMenu menu) {
        return new EaseInSubMenu("Hotkeys".ToDialogText(), false).Apply(subMenu => {
            TextMenu.Item MainSwitchStateItem;
            EaseInSubHeaderExtPub StateDescription = new EaseInSubHeaderExtPub("Configure At State All".ToDialogText(), false, menu) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            subMenu.Add(MainSwitchStateItem = new EnumerableSliderExt<bool>("Main Switch State".ToDialogText(), CreateMainSwitchStatesOptions(), TasHelperSettings.MainSwitchThreeStates).Change(value => { TasHelperSettings.MainSwitchThreeStates = value; StateDescription.FadeVisible = value; StateDescription.uneasedAlpha = StateDescription.Alpha; }));
            subMenu.Add(StateDescription);
            MainSwitchStateItem.OnEnter += () => StateDescription.FadeVisible = TasHelperSettings.MainSwitchThreeStates && true;
            MainSwitchStateItem.OnLeave += () => StateDescription.FadeVisible = false;
            subMenu.Add(new TextMenu.OnOff("Main Switch Visualize".ToDialogText(), TasHelperSettings.HotkeyStateVisualize).Change(value => TasHelperSettings.HotkeyStateVisualize = value));
            subMenu.Add(new TextMenu.OnOff("Main Switch Prevent".ToDialogText(), TasHelperSettings.AllowEnableModWithMainSwitch).Change(value => TasHelperSettings.AllowEnableModWithMainSwitch = value));

            TextMenu.Item keyConfig = new TextMenu.Button(Dialog.Clean("options_keyconfig")).Pressed(() => {
                subMenu.Focused = false;
                KeyboardConfigUI keyboardConfig = new ModuleSettingsKeyboardConfigUIExt(everestModule) {
                    OnClose = () => { subMenu.Focused = true; TH_Hotkeys.HotkeyInitialize(); }
                };

                Engine.Scene.Add(keyboardConfig);
                Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
            });
            TextMenuExt.EaseInSubHeaderExt descriptionText = new("Hotkey Description".ToDialogText(), false, menu) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            TextMenu.Item buttonConfig = new TextMenu.Button(Dialog.Clean("options_btnconfig")).Pressed(() => {
                subMenu.Focused = false;
                ButtonConfigUI buttonConfig = new ModuleSettingsButtonConfigUI(everestModule) {
                    OnClose = () => { subMenu.Focused = true; TH_Hotkeys.HotkeyInitialize(); }
                };

                Engine.Scene.Add(buttonConfig);
                Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
            });
            subMenu.Add(keyConfig);
            subMenu.Add(descriptionText);
            subMenu.Add(buttonConfig);

            keyConfig.OnEnter += () => descriptionText.FadeVisible = true;
            buttonConfig.OnEnter += () => descriptionText.FadeVisible = true;
            keyConfig.OnLeave += () => descriptionText.FadeVisible = false;
            buttonConfig.OnLeave += () => descriptionText.FadeVisible = false;
            subMenu.Add(new HLine(Color.Gray));
        });
    }

    private static EaseInOptionSubMenuExt CreateMoreOptionsSubMenu(TextMenu menu) {
        EaseInOptionSubMenuExt MoreOptionsItem = new EaseInOptionSubMenuExt("More Options".ToDialogText());
        MoreOptionsItem.OnLeave += () => MoreOptionsItem.MenuIndex = 0;
        MoreOptionsItem.Add("More Options Finished".ToDialogText(), new List<TextMenu.Item>());
        MoreOptionsItem.Add("More Options Page1".ToDialogText(), MoreOptionsMenu.Create_Page1(menu));
        MoreOptionsItem.Add("More Options Page2".ToDialogText(), MoreOptionsMenu.Create_Page2(menu));
        MoreOptionsItem.Add("More Options Page3".ToDialogText(), MoreOptionsMenu.Create_Page3(menu));
        return MoreOptionsItem.Apply(item => item.IncludeWidthInMeasurement = false);
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

    private static IEnumerable<KeyValuePair<LoadRangeColliderModes, string>> CreateEnableLoadRangeColliderOptions() {
        return new List<KeyValuePair<LoadRangeColliderModes, string>> {
            new(LoadRangeColliderModes.Off, "LRC Mode Off".ToDialogText()),
            new(LoadRangeColliderModes.Auto, "LRC Mode Auto".ToDialogText()),
            //new(LoadRangeColliderModes.Always, "LRC Mode Always".ToDialogText()),
            // Always Mode is a bit cursed, so i remove it
        };
    }
    private static IEnumerable<KeyValuePair<CountdownModes, string>> CreateCountdownOptions() {
        return new List<KeyValuePair<CountdownModes, string>> {
            new(CountdownModes.Off, "Countdown Mode Off".ToDialogText()),
            new(CountdownModes._3fCycle, "Countdown Mode 3f Cycle".ToDialogText()),
            new(CountdownModes._15fCycle, "Countdown Mode 15f Cycle".ToDialogText()),
            new(CountdownModes.ExactGroupMod3, "Countdown Mode Exact Group Mod 3".ToDialogText()),
            new(CountdownModes.ExactGroupMod15, "Countdown Mode Exact Group Mod 15".ToDialogText()),
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
    private static IEnumerable<KeyValuePair<SimplifiedGraphicsMode, string>> CreateSimplifiedGraphicsModeOptions() {
        return new List<KeyValuePair<SimplifiedGraphicsMode, string>> {
            new(SimplifiedGraphicsMode.Off, "Simplified Graphics Mode Off".ToDialogText()),
            new(SimplifiedGraphicsMode.WhenSimplifyGraphics, "Simplified Graphics Mode When Simplified Graphics".ToDialogText()),
            new(SimplifiedGraphicsMode.Always, "Simplified Graphics Mode Always".ToDialogText()),
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
        menu.OnClose += () => disabledItems.Clear();
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

            EaseInOptionSubMenuExt colorItem = CreateColorCustomizationSubMenu(menu, inGame);
            EaseInSubMenu countdownItem = CreateCountdownSubMenu(menu);
            EaseInSubMenu loadrangeItem = CreateLoadRangeSubMenu(menu);
            EaseInSubMenu simpspinnerItem = CreateSimplifiedGraphicSubMenu(menu);
            EaseInOptionSubMenuExt predictItem = CreatePredictorSubMenu(menu, inGame);
            EaseInOptionSubMenuExt autoWatchItem = CreateAutoWatchSubMenu(menu, inGame);
            EaseInOptionSubMenuExt moreoptionItem = CreateMoreOptionsSubMenu(menu);
            EaseInSubMenu hotkeysItem = CreateHotkeysSubMenu(everestModule, menu);
            disabledItems = new List<TextMenu.Item>() { colorItem, countdownItem, loadrangeItem, simpspinnerItem, predictItem, autoWatchItem ,moreoptionItem, hotkeysItem };
            int N = menu.IndexOf(mainItem);
            if (WhatsNew.ShouldShow) {
                EaseInSubMenu whatsnewItem = WhatsNew.CreateWhatsNewSubMenu(menu);
                menu.Insert(N + 1, whatsnewItem);
                disabledItems.Add(whatsnewItem);
                N++;
            }
            menu.Insert(N + 1, colorItem);
            menu.Insert(N + 2, countdownItem);
            menu.Insert(N + 3, loadrangeItem);
            menu.Insert(N + 4, simpspinnerItem);
            menu.Insert(N + 5, predictItem);
            menu.Insert(N + 6, autoWatchItem);
            menu.Insert(N + 7, moreoptionItem);
            menu.Insert(N + 8, hotkeysItem);

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

        if (Focused && ease > 0.9f) {
            if (CMCore.CoreModule.Settings.MenuPageDown.Pressed && Selection != LastPossibleSelection) {
                int selection = Selection;
                float yOffsetOf = GetYOffsetOf(Current);
                while (GetYOffsetOf(Current) < yOffsetOf + 1080f && Selection < LastPossibleSelection) {
                    MoveSelection(1);
                }
                if (selection != Selection) { Audio.Play("event:/ui/main/rollover_down"); }
            }
            else if (CMCore.CoreModule.Settings.MenuPageUp.Pressed && Selection != FirstPossibleSelection) {
                int selection2 = Selection;
                float yOffsetOf2 = GetYOffsetOf(Current);
                while (GetYOffsetOf(Current) > yOffsetOf2 - 1080f && Selection > FirstPossibleSelection) {
                    MoveSelection(-1);
                }
                if (selection2 != Selection) { Audio.Play("event:/ui/main/rollover_up"); }
            }
        }
    }

    public override void Render(Vector2 position, bool highlighted) {
        float c = Container.Alpha;
        Container.Alpha = alpha;
        base.Render(position, highlighted);
        Container.Alpha = c;
    }
}


public class EaseInSubHeaderExtVarTitle : TextMenuExt.EaseInSubHeaderExt {

    public string Title1;

    public string Title2;

    public float TitleLerp;

    public float TitleLerpTarget;
    public EaseInSubHeaderExtVarTitle(string title1, string title2, bool initiallyVisible, TextMenu containingMenu, string icon = "", bool initialFirstTitle = true) : base(title1, initiallyVisible, containingMenu, icon) {
        Title1 = title1;
        Title2 = title2;
        SetTitle(initialFirstTitle);
        TitleLerp = initialFirstTitle ? 0 : 1;
    }

    public void SetTitle(bool first) {
        if (first) {
            Title = Title1;
            TitleLerpTarget = 0;
        }
        else {
            Title = Title2;
            TitleLerpTarget = 1;
        }
    }

    public float BaseHeight(string title) {
        return (((Title.Length > 0) ? (ActiveFont.HeightOf(title) * 0.6f) : 0f) + (float)(TopPadding ? 48 : 0)) - 48f + HeightExtra;
    }

    public override float Height() {
        return MathHelper.Lerp(0f - containingMenu.ItemSpacing, MathHelper.Lerp(BaseHeight(Title1), BaseHeight(Title2), TitleLerp), Alpha);
    }

    public override void Update() {
        base.Update();
        TitleLerp = Calc.Clamp(TitleLerp + 10f * Math.Sign(TitleLerpTarget - TitleLerp) * Engine.DeltaTime, 0, 1);
    }
}

public class EaseInOptionSubMenuExt : OptionSubMenuExt, IEaseInItem {
    private float alpha;
    private float unEasedAlpha;

    public void Initialize() {
        alpha = unEasedAlpha = 0f;
        Visible = FadeVisible = true;
    }
    public bool FadeVisible { get; set; }
    public EaseInOptionSubMenuExt(string label) : base(label) {
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

        shouldGotoMainMenu &= !Visible || MenuIndex == 0;
    }
    public override void Render(Vector2 position, bool highlighted) {
        float c = Container.Alpha;
        Container.Alpha = alpha;
        base.Render(position, highlighted);
        Container.Alpha = c;
    }

    [Initialize]

    private static void InitializeHook() {
        typeof(OuiModOptions).GetMethod("Update").IlHook(PreventGotoMainMenu);
    }

    private static void PreventGotoMainMenu(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        if (cursor.TryGotoNext(ins => ins.OpCode == OpCodes.Brfalse_S)) {
            ILLabel label = (ILLabel)cursor.Next.Operand;
            cursor.Index -= 2;
            cursor.EmitDelegate(GetShouldGotoMainMenu);
            cursor.Emit(OpCodes.Brfalse, label);
        }
    }

    private static bool shouldGotoMainMenu = true;

    private static bool GetShouldGotoMainMenu() {
        bool result = shouldGotoMainMenu;
        shouldGotoMainMenu = true;
        return result;
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

#pragma warning disable CS8600
                string subheader = prop.GetCustomAttribute<SettingSubHeaderAttribute>()?.SubHeader;
#pragma warning restore CS8600
                if (subheader != null)
                    Add(new TextMenuExt.SubHeaderExt(subheader.DialogCleanOrNull() ?? subheader) {
                        TextColor = Color.Gray,
                        Offset = new Vector2(0f, -60f),
                        HeightExtra = 60f
                    });

                AddMapForceLabel(name, binding.Binding);

#pragma warning disable CS8600
                string description = prop.GetCustomAttribute<SettingDescriptionHardcodedAttribute>()?.description();
#pragma warning restore CS8600
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


#pragma warning disable CS8625
    public EaseInSubHeaderExtPub(string title, bool initiallyVisible, TextMenu containingMenu, string icon = null)
#pragma warning restore CS8625
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

public class HLine : TextMenu.Item {
    public Color lineColor;

    public float leftMargin;

    public float rightMargin;

    public string text;

    public float textHorizontalAlign;

    public HLine(Color color, float leftMargin = 20f, float rightMargin = 0f, string label = "", float textAlign = 0.5f) {
        Selectable = false;
        lineColor = color;
        this.leftMargin = leftMargin;
        this.rightMargin = rightMargin;
        text = label;
        textHorizontalAlign = textAlign;
    }

    public override float LeftWidth() {
        return 0f;
    }

    public override float RightWidth() {
        return 0f;
    }

    public override float Height() {
        return 20f;
    }

    public override void Render(Vector2 position, bool highlighted) {
        float left = Container.X - Container.Width / 2f + leftMargin;
        float right = Container.X + Container.Width / 2f - rightMargin;
        float y = position.Y;
        if (text.IsNullOrEmpty()) {
            Monocle.Draw.Line(new Vector2(left, y), new Vector2(right, y), lineColor, 4f);
        }
        else {
            float textCenter = MathHelper.Lerp(left, right, textHorizontalAlign);
            float halfWidth = ActiveFont.Measure(text).X / 2f * 0.6f + 10f;
            ActiveFont.DrawOutline(text, new Vector2(textCenter, y), new Vector2(0.5f, 0.5f), Vector2.One * 0.6f, Color.Gray, 2f, Color.Black);
            Monocle.Draw.Line(new Vector2(left, y), new Vector2(textCenter - halfWidth, y), lineColor, 4f);
            Monocle.Draw.Line(new Vector2(textCenter + halfWidth, y), new Vector2(right, y), lineColor, 4f);
        }
    }
}

public class EnumerableSliderExt<T> : TextMenu.Option<T> {

    public EnumerableSliderExt(string label, IEnumerable<T> options, T startValue)
        : base(label) {
        foreach (T option in options) {
            Add(option.ToString(), option, option.Equals(startValue));
        }
    }

    public EnumerableSliderExt(string label, IEnumerable<KeyValuePair<T, string>> options, T startValue)
        : base(label) {
        foreach (KeyValuePair<T, string> option in options) {
            Add(option.Value, option.Key, option.Key.Equals(startValue));
        }
    }

    public override void Render(Vector2 position, bool highlighted) {
        float alpha = Container.Alpha;
        Color strokeColor = Color.Black * (alpha * alpha * alpha);
        Color color = (Disabled ? Color.DarkSlateGray : ((highlighted ? Container.HighlightColor : UnselectedColor) * alpha));
        ActiveFont.DrawOutline(Label, position, new Vector2(0f, 0.5f), Vector2.One, color, 2f, strokeColor);
        if (Values.Count > 0) {
            float num = RightWidth();
            string option = Values[Index].Item1;
            if (num > 300f) {
                num = ActiveFont.Measure(option).X * 0.8f + 132f;
            }
            ActiveFont.DrawOutline(option, position + new Vector2(Container.Width - num * 0.5f + (float)lastDir * ValueWiggler.Value * 8f, 0f), new Vector2(0.5f, 0.5f), Vector2.One * 0.8f, color, 2f, strokeColor);
            Vector2 vector = Vector2.UnitX * (highlighted ? ((float)Math.Sin(sine * 4f) * 4f) : 0f);
            bool flag = Index > 0;
            Color color2 = (flag ? color : (Color.DarkSlateGray * alpha));
            Vector2 position2 = position + new Vector2(Container.Width - num + 40f + ((lastDir < 0) ? ((0f - ValueWiggler.Value) * 8f) : 0f), 0f) - (flag ? vector : Vector2.Zero);
            ActiveFont.DrawOutline("<", position2, new Vector2(0.5f, 0.5f), Vector2.One, color2, 2f, strokeColor);
            bool flag2 = Index < Values.Count - 1;
            Color color3 = (flag2 ? color : (Color.DarkSlateGray * alpha));
            Vector2 position3 = position + new Vector2(Container.Width - 40f + ((lastDir > 0) ? (ValueWiggler.Value * 8f) : 0f), 0f) + (flag2 ? vector : Vector2.Zero);
            ActiveFont.DrawOutline(">", position3, new Vector2(0.5f, 0.5f), Vector2.One, color3, 2f, strokeColor);
        }
    }
}

public class IntSliderExt : TextMenuExt.IntSlider {
    public IntSliderExt(string label, int min, int max, int value = 0) : base(label, min, max, value) { }

    public override float RightWidth() {
        return Math.Max(base.RightWidth(), 180f); // ensure 1 digits and 2 digits intSliders have same width, 170 is enough for chinese, and 180 is enough for english
    }
}