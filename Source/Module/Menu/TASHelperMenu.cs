using Celeste.Mod.TASHelper.Utils;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System.Reflection;
using static Celeste.Mod.TASHelper.Module.TASHelperSettings;

namespace Celeste.Mod.TASHelper.Module.Menu;

internal static class TASHelperMenu {
    internal static string ToDialogText(this string input) => Dialog.Clean("TAS_HELPER_" + input.ToUpper().Replace(" ", "_")).Replace("\\S", " ");
    private static EaseInOptionSubMenuCountExt CreateColorCustomizationSubMenu(TextMenu menu, bool inGame) {
        EaseInOptionSubMenuCountExt ColorCustomizationItem = new EaseInOptionSubMenuCountExt("Color Customization".ToDialogText());
        ColorCustomizationItem.OnLeave += () => ColorCustomizationItem.MenuIndex = 0;
        ColorCustomizationItem.Add("Color Customization Finished".ToDialogText(), new List<TextMenu.Item>());
        ColorCustomizationItem.Add("Color Customization OnOff".ToDialogText(), CustomColors.Create_PageOnOff(menu, inGame));
        ColorCustomizationItem.Add("Color Customization Spinner Color".ToDialogText(), CustomColors.Create_PageSpinnerColor(menu, inGame));
        ColorCustomizationItem.Add("Color Customization Predictor".ToDialogText(), CustomColors.Create_PagePredictor(menu, inGame));
        ColorCustomizationItem.Add("Color Customization Other".ToDialogText(), CustomColors.Create_PageOther(menu, inGame));
        return ColorCustomizationItem.Apply(item => item.IncludeWidthInMeasurement = false);
    }

    private static EaseInOptionSubMenuCountExt CreatePredictorSubMenu(TextMenu menu, bool inGame) {
        EaseInOptionSubMenuCountExt PredictorItem = new EaseInOptionSubMenuCountExt("Predictor".ToDialogText());
        PredictorItem.OnLeave += () => PredictorItem.MenuIndex = 0;
        PredictorItem.Add("Predictor Finished".ToDialogText(), new List<TextMenu.Item>());
        PredictorItem.Add("Predictor OnOff".ToDialogText(), PredictorMenu.Create_PageOnOff(menu, inGame));
        PredictorItem.Add("Predictor Keyframe 1".ToDialogText(), PredictorMenu.Create_PageKeyframe_1(menu, inGame));
        PredictorItem.Add("Predictor Keyframe 2".ToDialogText(), PredictorMenu.Create_PageKeyframe_2(menu, inGame));
        PredictorItem.Add("Predictor Style".ToDialogText(), PredictorMenu.Create_PageStyle(menu, inGame));
        PredictorItem.Add("Predictor Other".ToDialogText(), PredictorMenu.Create_PageOther(menu, inGame));
        return PredictorItem.Apply(item => item.IncludeWidthInMeasurement = false);
    }

    private static EaseInSubMenu CreateCountdownSubMenu(TextMenu menu) {
        return new EaseInSubMenu("Countdown".ToDialogText(), false).Apply(subMenu => {
            TextMenu.Item CountdownModeItem;
            EaseInSubHeaderExtVarTitle descriptionText = new("Countdown Exact Group Description".ToDialogText(), "Countdown Mode Description".ToDialogText(), false, menu, null, TasHelperSettings.CountdownMode is CountdownModes.ExactGroupMod3 or CountdownModes.ExactGroupMod15) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            subMenu.Add(CountdownModeItem = new TextMenuExt.EnumerableSlider<CountdownModes>("Countdown Mode".ToDialogText(), CreateCountdownOptions(),
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
            subMenu.Add(new TextMenuExt.EnumerableSlider<CountdownFonts>("Font".ToDialogText(), CreateCountdownFontOptions(),
                TasHelperSettings.CountdownFont).Change(value => TasHelperSettings.CountdownFont = value));
            subMenu.Add(new TextMenu.OnOff("Darken When Uncollidable".ToDialogText(), TasHelperSettings.DarkenWhenUncollidable).Change(value => TasHelperSettings.DarkenWhenUncollidable = value));
            subMenu.Add(new TextMenuExt.IntSlider("Hires Font Size".ToDialogText(), 1, 20, TasHelperSettings.HiresFontSize).Change(value => TasHelperSettings.HiresFontSize = value));
            subMenu.Add(new TextMenuExt.IntSlider("Hires Font Stroke".ToDialogText(), 0, 20, TasHelperSettings.HiresFontStroke).Change(value => TasHelperSettings.HiresFontStroke = value));
            /*
            TextMenu.Item OptimizationItem;
            subMenu.Add(OptimizationItem = new TextMenu.OnOff("Performance Optimization".ToDialogText(), TasHelperSettings.DoNotRenderWhenFarFromView).Change(value => TasHelperSettings.DoNotRenderWhenFarFromView = value));
            subMenu.AddDescription(menu, OptimizationItem, "Performance Optimization Description".ToDialogText());
            */
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
            TextMenu.Item LRCItem;
            subMenu.Add(LRCItem = new TextMenuExt.EnumerableSlider<LoadRangeColliderModes>("Load Range Collider".ToDialogText(), CreateEnableLoadRangeColliderOptions(),
                TasHelperSettings.LoadRangeColliderMode).Change(value => TasHelperSettings.LoadRangeColliderMode = value));
            subMenu.AddDescription(menu, LRCItem, "LRC Description".ToDialogText());
        });
    }

    private static EaseInSubMenu CreateSimplifiedGraphicSubMenu(TextMenu menu) {
        return new EaseInSubMenu("Simplified Graphics".ToDialogText(), false).Apply(subMenu => {
            subMenu.Add(new TextMenu.OnOff("Simplified Spinners".ToDialogText(), TasHelperSettings.EnableSimplifiedSpinner).Change(value => TasHelperSettings.EnableSimplifiedSpinner = value));
            subMenu.Add(new TextMenuExt.EnumerableSlider<SimplifiedGraphicsMode>("Clear Spinner Sprites".ToDialogText(), CreateSimplifiedGraphicsModeOptions(), TasHelperSettings.EnforceClearSprites).Change(value => TasHelperSettings.EnforceClearSprites = value));
            subMenu.Add(new TextMenuExt.IntSlider("Spinner Filler Opacity".ToDialogText(), 0, 9, TasHelperSettings.SpinnerFillerOpacity_Collidable).Change(value => TasHelperSettings.SpinnerFillerOpacity_Collidable = value));
            subMenu.Add(new TextMenuExt.IntSlider("Spinner Filler Opacity Extra".ToDialogText(), 0, 9, TasHelperSettings.SpinnerFillerOpacity_Uncollidable).Change(value => TasHelperSettings.SpinnerFillerOpacity_Uncollidable = value));
            subMenu.Add(new TextMenu.OnOff("Spinner Dashed Border".ToDialogText(), TasHelperSettings.SimplifiedSpinnerDashedBorder).Change(value => TasHelperSettings.SimplifiedSpinnerDashedBorder = value));
            subMenu.Add(new TextMenu.OnOff("Spinner_Ignore_TAS_UncollidableAlpha".ToDialogText(), TasHelperSettings.Ignore_TAS_UnCollidableAlpha).Change(value => TasHelperSettings.Ignore_TAS_UnCollidableAlpha = value));
            subMenu.Add(new TextMenu.OnOff("ACH For Spinner".ToDialogText(), TasHelperSettings.ApplyActualCollideHitboxForSpinner).Change(value => TasHelperSettings.ApplyActualCollideHitboxForSpinner = value));
            subMenu.Add(new HLine());
            TextMenu.Item simplifiedLightning;
            subMenu.Add(simplifiedLightning = new TextMenuExt.EnumerableSlider<SimplifiedGraphicsMode>("Simplified Lightning".ToDialogText(), CreateSimplifiedGraphicsModeOptions(), TasHelperSettings.EnableSimplifiedLightningMode).Change(value => TasHelperSettings.EnableSimplifiedLightningMode = value));
            subMenu.AddDescription(menu, simplifiedLightning, "Simplified Lightning Description".ToDialogText());
            TextMenu.Item highlightItem;
            subMenu.Add(highlightItem = new TextMenu.OnOff("Highlight Load Unload".ToDialogText(), TasHelperSettings.HighlightLoadUnload).Change(value => TasHelperSettings.HighlightLoadUnload = value));
            subMenu.AddDescription(menu, highlightItem, "Highlight Description".ToDialogText());
            TextMenu.Item ACH_LightningItem;
            subMenu.Add(ACH_LightningItem = new TextMenu.OnOff("ACH For Lightning".ToDialogText(), TasHelperSettings.ApplyActualCollideHitboxForLightning).Change(value => TasHelperSettings.ApplyActualCollideHitboxForLightning = value));
            subMenu.AddDescription(menu, ACH_LightningItem, "ACH Warn Lightning".ToDialogText());
            subMenu.Add(new HLine());
            TextMenu.Item simplifiedTrigger;
            subMenu.Add(simplifiedTrigger = new TextMenuExt.EnumerableSlider<SimplifiedGraphicsMode>("Simplified Triggers".ToDialogText(), CreateSimplifiedGraphicsModeOptions(), TasHelperSettings.EnableSimplifiedTriggersMode).Change(value => TasHelperSettings.EnableSimplifiedTriggersMode = value));
            subMenu.Add(new TextMenu.OnOff("Hide Camera Trigger".ToDialogText(), TasHelperSettings.HideCameraTriggers).Change(value => TasHelperSettings.HideCameraTriggers = value));
            subMenu.Add(new TextMenu.OnOff("Hide Gold Berry".ToDialogText(), TasHelperSettings.HideGoldBerryCollectTrigger).Change(value => TasHelperSettings.HideGoldBerryCollectTrigger = value));
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
            subMenu.Add(new TextMenu.OnOff("Main Switch Visualize".ToDialogText(), TasHelperSettings.HotkeyStateVisualize).Change(value => TasHelperSettings.HotkeyStateVisualize = value));
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
            TextMenu.Item cassetteBlock;
            subMenu.Add(cassetteBlock = new TextMenu.OnOff("Cassette Block Helper".ToDialogText(), TasHelperSettings.EnableCassetteBlockHelper).Change((value) => TasHelperSettings.EnableCassetteBlockHelper = value));
            subMenu.AddDescription(menu, cassetteBlock, "Cassette Block Description".ToDialogText());
            subMenu.Add(new TextMenu.OnOff("Cassette Block Helper Extra Info".ToDialogText(), TasHelperSettings.CassetteBlockHelperShowExtraInfo).Change((value) => TasHelperSettings.CassetteBlockHelperShowExtraInfo = value));
            TextMenu.Item EntityActivatorReminderItem;
            subMenu.Add(EntityActivatorReminderItem = new TextMenu.OnOff("Entity Activator Reminder".ToDialogText(), TasHelperSettings.EntityActivatorReminder).Change((value) => TasHelperSettings.EntityActivatorReminder = value));
            subMenu.AddDescription(menu, EntityActivatorReminderItem, "Entity Activator Reminder Description".ToDialogText());
            subMenu.Add(new TextMenu.OnOff("Enable Pixel Grid".ToDialogText(), TasHelperSettings.EnablePixelGrid).Change(value => TasHelperSettings.EnablePixelGrid = value));
            subMenu.Add(new TextMenuExt.IntSlider("Pixel Grid Width".ToDialogText(), 0, 50, TasHelperSettings.PixelGridWidth).Change(value => TasHelperSettings.PixelGridWidth = value));
            subMenu.Add(new TextMenuExt.IntSlider("Pixel Grid Opacity".ToDialogText(), 1, 10, TasHelperSettings.PixelGridOpacity).Change(value => TasHelperSettings.PixelGridOpacity = value));
            subMenu.Add(new TextMenu.OnOff("Camera Target".ToDialogText(), TasHelperSettings.UsingCameraTarget).Change(value => TasHelperSettings.UsingCameraTarget = value));
            subMenu.Add(new TextMenuExt.IntSlider("Camera Target Vector Opacity".ToDialogText(), 1, 9, TasHelperSettings.CameraTargetLinkOpacity).Change(value => TasHelperSettings.CameraTargetLinkOpacity = value));
            subMenu.Add(new TextMenu.OnOff("FireBall Track".ToDialogText(), TasHelperSettings.UsingFireBallTrack).Change(value => TasHelperSettings.UsingFireBallTrack = value));
            subMenu.Add(new TextMenu.OnOff("RotateSpinner Track".ToDialogText(), TasHelperSettings.UsingRotateSpinnerTrack).Change(value => TasHelperSettings.UsingRotateSpinnerTrack = value));
            subMenu.Add(new TextMenu.OnOff("TrackSpinner Track".ToDialogText(), TasHelperSettings.UsingTrackSpinnerTrack).Change(value => TasHelperSettings.UsingTrackSpinnerTrack = value));
            subMenu.Add(new TextMenu.OnOff("Show Wind Speed".ToDialogText(), TasHelperSettings.ShowWindSpeed).Change(value => TasHelperSettings.ShowWindSpeed = value));
            subMenu.Add(new TextMenu.OnOff("Open Console In Tas".ToDialogText(), TasHelperSettings.EnableOpenConsoleInTas).Change(value => TasHelperSettings.EnableOpenConsoleInTas = value));
            subMenu.Add(new TextMenu.OnOff("Scrollable History Log".ToDialogText(), TasHelperSettings.EnableScrollableHistoryLog).Change(value => TasHelperSettings.EnableScrollableHistoryLog = value));
            TextMenu.Item betterInvincible;
            subMenu.Add(betterInvincible = new TextMenu.OnOff("Better Invincibility".ToDialogText(), TasHelperSettings.BetterInvincible).Change(value => TasHelperSettings.BetterInvincible = value));
            subMenu.AddDescription(menu, betterInvincible, "Better Invincible Description".ToDialogText());
            TextMenu.Item subscribeWhatsNew;
            subMenu.Add(subscribeWhatsNew = new TextMenu.OnOff("Subscribe Whats New".ToDialogText(), TasHelperSettings.SubscribeWhatsNew).Change(value => TasHelperSettings.SubscribeWhatsNew = value));
            subMenu.AddDescription(menu, subscribeWhatsNew, "Subscribe Whats New Description".ToDialogText());
            TextMenu.Item OoOItem;
            subMenu.Add(OoOItem = new TextMenu.OnOff("Order of Operation Stepping".ToDialogText(), TasHelperSettings.EnableOoO).Change(value => TasHelperSettings.EnableOoO = value));
            subMenu.AddDescription(menu, OoOItem, "Order of Operation Description".ToDialogText());
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

            EaseInOptionSubMenuCountExt colorItem = CreateColorCustomizationSubMenu(menu, inGame);
            EaseInSubMenu countdownItem = CreateCountdownSubMenu(menu);
            EaseInSubMenu loadrangeItem = CreateLoadRangeSubMenu(menu);
            EaseInSubMenu simpspinnerItem = CreateSimplifiedGraphicSubMenu(menu);
            EaseInOptionSubMenuCountExt predictItem = CreatePredictorSubMenu(menu, inGame);
            EaseInSubMenu moreoptionItem = CreateMoreOptionsSubMenu(menu);
            EaseInSubMenu hotkeysItem = CreateHotkeysSubMenu(everestModule, menu);
            disabledItems = new List<TextMenu.Item>() { colorItem, countdownItem, loadrangeItem, simpspinnerItem, predictItem, moreoptionItem, hotkeysItem };
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
            menu.Insert(N + 6, moreoptionItem);
            menu.Insert(N + 7, hotkeysItem);
            hotkeysItem.AddDescription(menu, "Hotkey Description".ToDialogText());

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


public class EaseInSubHeaderExtVarTitle : TextMenuExt.EaseInSubHeaderExt {

    public string Title1;

    public string Title2;

    public float TitleLerp;

    public float TitleLerpTarget;
    public EaseInSubHeaderExtVarTitle(string title1, string title2, bool initiallyVisible, TextMenu containingMenu, string icon = null, bool initialFirstTitle = true) : base(title1, initiallyVisible, containingMenu, icon) {
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
    public float margins;
    public HLine(float margins = 0f) {
        Selectable = false;
        this.margins = margins;
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
        Monocle.Draw.Line(new Vector2(Container.X - Container.Width / 2f + margins, position.Y), new Vector2(Container.X + Container.Width / 2f - margins, position.Y), Color.Gray, 4f);
    }
}