using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using TAS.EverestInterop;
using static Celeste.Mod.TASHelper.Module.TASHelperSettings;
using static TAS.EverestInterop.Hotkeys;

namespace Celeste.Mod.TASHelper.Module;

[SettingName("TAS_HELPER_NAME")]
public class TASHelperSettings : EverestModuleSettings {

    public static TASHelperSettings Instance { get; private set; }

    public TASHelperSettings() {
        Instance = this;
    }

    internal void OnLoadSettings() {
        // do nothing currently

        // Everest will save & load settings of public fields/properties when open the game
        // i don't want the setters of those properties which has a do not save attribute, which will break the awakes
        // so i have to make them internal
        // but i also want to expose them to users, so i add TasHelperSettingsAlias class
        // i need to save those loadRangeMode fields, so they should be public
        // although they should not be exposed to users...
    }

    public void InitializeSettings() {
        UpdateAuxiliaryVariable();
        Hotkeys = new() { MainSwitchHotkey, CountDownHotkey, LoadRangeHotkey, PixelGridWidthHotkey };
    }

    private bool enabled = true;

    public bool Enabled { get => enabled; set => enabled = value; }

    #region MainSwitch

    // it will only affect main options, will not affect suboptions (which will not work if corresponding main option is not on)

    public enum MainSwitchModes { Off, OnlyDefault, AllowAll }

    private MainSwitchModes mainSwitch = MainSwitchModes.AllowAll;

    public MainSwitchModes MainSwitch {
        get => mainSwitch;
        set {
            if (mainSwitch == value) {
                return;
            }
            mainSwitch = value;
            if (MainSwitchWatcher.instance is MainSwitchWatcher watcher) {
                watcher.Refresh();
            }
            switch (value) {
                case MainSwitchModes.Off:
                    Enabled = false;
                    Sleep();
                    break;
                default:
                    Enabled = true;
                    Awake(value == MainSwitchModes.AllowAll);
                    break;
            }
            // the setters are not called, so the auxiliary variables don't get update, we have to update them
            // we can't call the setters, which will otherwise break the Awake...s
            UpdateAuxiliaryVariable();
            return;
        }
    }
    internal void Sleep() {
        MainSwitch = MainSwitchModes.Off;
        Awake_CycleHitboxColors = false;
        Awake_UsingNotInViewColor = false;
        Awake_EnableSimplifiedSpinner = false;
        Awake_CountdownModes = false;
        Awake_LoadRange = false;
        Awake_CameraTarget = false;
        Awake_PixelGrid = false;
        Awake_SpawnPoint = false;
        Awake_EntityActivatorReminder = false;
    }
    internal void Awake(bool awakeAll) {
        MainSwitch = awakeAll ? MainSwitchModes.AllowAll : MainSwitchModes.OnlyDefault;
        Awake_CycleHitboxColors = true;
        Awake_UsingNotInViewColor = true;
        Awake_EnableSimplifiedSpinner = true;
        Awake_CountdownModes = awakeAll;
        Awake_LoadRange = awakeAll;
        Awake_CameraTarget = awakeAll;
        Awake_PixelGrid = awakeAll;
        Awake_SpawnPoint = true;
        Awake_EntityActivatorReminder = true;
    }

    #endregion

    #region CycleHitboxColor
    public bool Awake_CycleHitboxColors = true;

    // we need to make it public, so this setting is stored
    // though we don't want anyone to visit it directly...
    public bool showCycleHitboxColor = true;

    [SettingDoNotSave]
    internal bool ShowCycleHitboxColors {
        get => Enabled && Awake_CycleHitboxColors && showCycleHitboxColor;
        set {
            showCycleHitboxColor = value;
            Awake_CycleHitboxColors = true;
        }
    }

    public bool Awake_UsingNotInViewColor = true;
    public enum UsingNotInViewColorModes { Off, WhenUsingInViewRange, Always };

    public UsingNotInViewColorModes usingNotInViewColorMode = UsingNotInViewColorModes.WhenUsingInViewRange;

    [SettingDoNotSave]
    internal UsingNotInViewColorModes UsingNotInViewColorMode {
        get => Enabled && Awake_UsingNotInViewColor ? usingNotInViewColorMode : UsingNotInViewColorModes.Off;
        set {
            usingNotInViewColorMode = value;
            Awake_UsingNotInViewColor = true;
            UsingNotInViewColor = (value == UsingNotInViewColorModes.Always) || (value == UsingNotInViewColorModes.WhenUsingInViewRange && UsingInViewRange);
        }
    }

    public bool UsingNotInViewColor = true;

    #endregion

    #region Countdown
    public bool Awake_CountdownModes = true;
    public enum CountdownModes { Off, _3fCycle, _15fCycle };

    public CountdownModes countdownMode = CountdownModes.Off;

    [SettingDoNotSave]
    internal CountdownModes CountdownMode {
        get => Enabled && Awake_CountdownModes ? countdownMode : CountdownModes.Off;
        set {
            countdownMode = value;
            Awake_CountdownModes = true;

            UsingCountDown = (CountdownMode != CountdownModes.Off);
            if (CountdownMode == CountdownModes._3fCycle) {
                SpinnerCountdownLoad = true;
                SpinnerInterval = 0.05f;
            }
            else {
                SpinnerCountdownLoad = false;
                SpinnerInterval = 0.25f;
            }
        }
    }

    public enum CountdownFonts { PixelFont, HiresFont };

    public CountdownFonts CountdownFont = CountdownFonts.HiresFont;

    public int HiresFontSize = 8;

    public bool usingHiresFont => CountdownFont == CountdownFonts.HiresFont;

    public int HiresFontStroke = 5;

    public bool DoNotRenderWhenFarFromView = true;

    #endregion

    #region LoadRange

    public bool Awake_LoadRange = true;
    public enum LoadRangeModes { Neither, InViewRange, NearPlayerRange, Both };

    public LoadRangeModes loadRangeMode = LoadRangeModes.Neither;

    [SettingDoNotSave]
    internal LoadRangeModes LoadRangeMode {
        get => Enabled && Awake_LoadRange ? loadRangeMode : LoadRangeModes.Neither;
        set {
            loadRangeMode = value;
            Awake_LoadRange = true;

            UsingLoadRange = LoadRangeMode != LoadRangeModes.Neither;
            UsingInViewRange = LoadRangeMode == LoadRangeModes.InViewRange || LoadRangeMode == LoadRangeModes.Both;
            UsingNearPlayerRange = LoadRangeMode == LoadRangeModes.NearPlayerRange || LoadRangeMode == LoadRangeModes.Both;
            UsingNotInViewColor = (UsingNotInViewColorMode == UsingNotInViewColorModes.Always) || (UsingNotInViewColorMode == UsingNotInViewColorModes.WhenUsingInViewRange && UsingInViewRange);
        }
    }

    public int InViewRangeWidth { get; set; } = 16;

    public int NearPlayerRangeWidth { get; set; } = 8;

    private int loadRangeOpacity = 4;
    public int LoadRangeOpacity {
        get => loadRangeOpacity;
        set {
            loadRangeOpacity = value;
            RangeAlpha = value * 0.1f;
        }
    }

    public bool ApplyCameraZoom { get; set; } = false;

    #endregion

    #region Simplified Spinner

    public bool Awake_EnableSimplifiedSpinner = true;
    public bool enableSimplifiedSpinner = true;

    [SettingDoNotSave]
    internal bool EnableSimplifiedSpinner {
        get => Enabled && Awake_EnableSimplifiedSpinner && enableSimplifiedSpinner;
        set {
            enableSimplifiedSpinner = value;
            Awake_EnableSimplifiedSpinner = true;
        }
    }

    public enum ClearSpritesMode { Off, WhenSimplifyGraphics, Always };

    private ClearSpritesMode enforceClearSprites = ClearSpritesMode.WhenSimplifyGraphics;

    public ClearSpritesMode EnforceClearSprites {
        get => enforceClearSprites;
        set => enforceClearSprites = value;
    }

    public bool ClearSpinnerSprites => EnableSimplifiedSpinner && (EnforceClearSprites == ClearSpritesMode.Always || (EnforceClearSprites == ClearSpritesMode.WhenSimplifyGraphics && TasSettings.SimplifiedGraphics));


    private int spinnerFillerOpacity = 3;
    public int SpinnerFillerOpacity {
        get => spinnerFillerOpacity;
        set {
            spinnerFillerOpacity = value;
            SpinnerFillerAlpha = value * 0.1f;
        }
    }
    #endregion


    public bool Awake_EntityActivatorReminder = true;
    public bool entityActivatorReminder = true;

    [SettingDoNotSave]
    internal bool EntityActivatorReminder {
        get => Enabled && Awake_EntityActivatorReminder && entityActivatorReminder;
        set {
            entityActivatorReminder = value;
            Awake_EntityActivatorReminder = true;
        }
    }

    #region Auxilary Variables
    public void UpdateAuxiliaryVariable() {
        // update the variables associated to variables govern by spinner main switch
        // it can happen their value is changed but not via the setter (i.e. change the Awake_...s)

        UsingNotInViewColor = (UsingNotInViewColorMode == UsingNotInViewColorModes.Always) || (UsingNotInViewColorMode == UsingNotInViewColorModes.WhenUsingInViewRange && UsingInViewRange);
        UsingCountDown = (CountdownMode != CountdownModes.Off);
        if (CountdownMode == CountdownModes._3fCycle) {
            SpinnerCountdownLoad = true;
            SpinnerInterval = 0.05f;
        }
        else {
            SpinnerCountdownLoad = false;
            SpinnerInterval = 0.25f;
        }
        UsingLoadRange = (LoadRangeMode != LoadRangeModes.Neither);
        UsingInViewRange = (LoadRangeMode == LoadRangeModes.InViewRange || LoadRangeMode == LoadRangeModes.Both);
        UsingNearPlayerRange = (LoadRangeMode == LoadRangeModes.NearPlayerRange || LoadRangeMode == LoadRangeModes.Both);
    }
    public bool UsingCountDown = false;
    public bool UsingLoadRange = true;
    public bool UsingInViewRange = true;
    public bool UsingNearPlayerRange = true;
    public bool SpinnerCountdownLoad = true;

    [Obsolete]
    public int SpinnerCountdownUpperBound => SpinnerCountdownLoad ? 9 : 99;
    public float SpinnerInterval = 0.05f;
    public float RangeAlpha = 0.4f;
    public float SpinnerFillerAlpha = 0.4f;
    public bool UsingFreezeColor = true;

    public Color LoadRangeColliderColor = CustomColors.defaultLoadRangeColliderColor;
    public Color InViewRangeColor = CustomColors.defaultInViewRangeColor;
    public Color NearPlayerRangeColor = CustomColors.defaultNearPlayerRangeColor;
    public Color CameraTargetColor = CustomColors.defaultCameraTargetColor;
    public Color NotInViewColor = CustomColors.defaultNotInViewColor;
    public Color NeverActivateColor = CustomColors.defaultNeverActivateColor;
    public Color ActivateEveryFrameColor = CustomColors.defaultActivateEveryFrameColor;

    #endregion

    #region Other

    public bool Awake_CameraTarget = true;

    public bool usingCameraTarget = false;

    [SettingDoNotSave]
    internal bool UsingCameraTarget {
        get => Enabled && Awake_CameraTarget && usingCameraTarget;
        set {
            usingCameraTarget = value;
            Awake_CameraTarget = true;
        }
    }

    public int CameraTargetLinkOpacity { get; set; } = 6;

    public bool Awake_PixelGrid = true;

    public bool enablePixelGrid = false;

    [SettingDoNotSave]
    internal bool EnablePixelGrid {
        get => Enabled && Awake_PixelGrid && enablePixelGrid;
        set {
            enablePixelGrid = value;
            Awake_PixelGrid = true;
        }
    }

    public int PixelGridWidth = 2;
    public int PixelGridOpacity { get; set; } = 8;

    public bool Awake_SpawnPoint = true;

    public bool usingSpawnPoint = true;

    [SettingDoNotSave]
    internal bool UsingSpawnPoint {
        get => Enabled && Awake_SpawnPoint && usingSpawnPoint;
        set {
            usingSpawnPoint = value;
            Awake_SpawnPoint = true;
        }
    }

    public int CurrentSpawnPointOpacity = 5;

    public int OtherSpawnPointOpacity = 2;

    public bool AllowEnableModWithMainSwitch = true;

    private bool mainSwitchStateVisualize = true;

    public bool MainSwitchStateVisualize {
        get => mainSwitchStateVisualize;
        set {
            mainSwitchStateVisualize = value;
            if (MainSwitchWatcher.instance is MainSwitchWatcher watcher) {
                watcher.Visible = mainSwitchStateVisualize;
            }
        }
    }

    private bool mainSwitchThreeStates = false;

    public bool MainSwitchThreeStates {
        get => mainSwitchThreeStates;
        set {
            mainSwitchThreeStates = value;
            if (MainSwitchWatcher.instance is MainSwitchWatcher watcher) {
                watcher.Refresh();
            }
        }
    }

    #endregion

    #region HotKey

    private static ButtonBinding keyMainSwitch { get; set; } = new(0, Keys.LeftControl, Keys.E);
    private static ButtonBinding keyCountDown { get; set; } = new(0, Keys.LeftControl, Keys.R);
    private static ButtonBinding keyLoadRange { get; set; } = new(0, Keys.LeftControl, Keys.T);
    private static ButtonBinding keyPixelGridWidth { get; set; } = new(0, Keys.LeftControl, Keys.F);


    [SettingName("TAS_HELPER_MAIN_SWITCH_HOTKEY")]
    [SettingSubHeader("TAS_HELPER_HOTKEY_DESCRIPTION")]
    [SettingDescriptionHardcoded]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.E)]
    public ButtonBinding KeyMainSwitch {
        get => keyMainSwitch;
        set {
            keyMainSwitch = value;
            MainSwitchHotkey = new Hotkey(keyMainSwitch.Keys, keyMainSwitch.Buttons, true, false);
        }
    }

    [SettingName("TAS_HELPER_SWITCH_COUNT_DOWN_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.R)]
    public ButtonBinding KeyCountDown {
        get => keyCountDown;
        set {
            keyCountDown = value;
            CountDownHotkey = new Hotkey(keyCountDown.Keys, keyCountDown.Buttons, true, false);
        }
    }

    [SettingName("TAS_HELPER_SWITCH_LOAD_RANGE_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.T)]
    public ButtonBinding KeyLoadRange {
        get => keyLoadRange;
        set {
            keyLoadRange = value;
            LoadRangeHotkey = new Hotkey(keyLoadRange.Keys, keyLoadRange.Buttons, true, false);
        }
    }

    [SettingName("TAS_HELPER_SWITCH_PIXEL_GRID_WIDTH_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.F)]
    public ButtonBinding KeyPixelGridWidth {
        get => keyPixelGridWidth;
        set {
            keyPixelGridWidth = value;
            PixelGridWidthHotkey = new Hotkey(keyPixelGridWidth.Keys, keyPixelGridWidth.Buttons, true, false);
        }
    }

    private Hotkey MainSwitchHotkey { get; set; } = new Hotkey(keyMainSwitch.Keys, keyMainSwitch.Buttons, true, false);

    private Hotkey CountDownHotkey { get; set; } = new Hotkey(keyCountDown.Keys, keyCountDown.Buttons, true, false);

    private Hotkey LoadRangeHotkey { get; set; } = new Hotkey(keyLoadRange.Keys, keyLoadRange.Buttons, true, false);

    private Hotkey PixelGridWidthHotkey { get; set; } = new Hotkey(keyPixelGridWidth.Keys, keyPixelGridWidth.Buttons, true, false);

    private static List<Hotkey> Hotkeys;

    public bool SettingsHotkeysPressed() {
        if (Engine.Scene is not Level level) {
            return false;
        }
        bool updateKey = true;
        bool updateButton = true;
        bool InOuiModOption = TASHelperMenu.mainItem?.Container?.Focused is bool b && b;
        if (InOuiModOption || level.Tracker.GetEntity<KeyboardConfigUI>() is not null ||
            (level.Tracker.Entities.TryGetValue(typeof(ModuleSettingsKeyboardConfigUIExt), out var list) && list.Count > 0)) {
            updateKey = false;
        }
        if (InOuiModOption || level.Tracker.GetEntity<ButtonConfigUI>() is not null) {
            updateButton = false;
        }
        foreach (Hotkey hotkey in Hotkeys) {
            hotkey.Update(updateKey, updateButton);
        }
        bool changed = false;
        if (MainSwitchHotkey.Pressed) {
            changed = true;
            switch (MainSwitch) {
                case MainSwitchModes.Off: {
                        if (!AllowEnableModWithMainSwitch) {
                            changed = false;
                            if (MainSwitchWatcher.instance is MainSwitchWatcher watcher) {
                                watcher.Refresh(true);
                            }
                            break;
                        }
                        MainSwitch = MainSwitchThreeStates ? MainSwitchModes.OnlyDefault : MainSwitchModes.AllowAll;
                        break;
                    }
                // it may happen that MainSwitchThreeStates = false but MainSwitch = OnlyDefault... it's ok
                case MainSwitchModes.OnlyDefault: MainSwitch = MainSwitchModes.AllowAll; break;
                case MainSwitchModes.AllowAll: MainSwitch = MainSwitchModes.Off; break;
            }
        }
        if (CountDownHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                switch (CountdownMode) {
                    case CountdownModes.Off: CountdownMode = CountdownModes._3fCycle; break;
                    case CountdownModes._3fCycle: CountdownMode = CountdownModes._15fCycle; break;
                    case CountdownModes._15fCycle: CountdownMode = CountdownModes.Off; break;
                }
            }
            else {
                if (MainSwitchWatcher.instance is MainSwitchWatcher watcher) {
                    watcher.RefreshOther();
                }
            }
        }
        if (LoadRangeHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                switch (LoadRangeMode) {
                    case LoadRangeModes.Neither: LoadRangeMode = LoadRangeModes.InViewRange; break;
                    case LoadRangeModes.InViewRange: LoadRangeMode = LoadRangeModes.NearPlayerRange; break;
                    case LoadRangeModes.NearPlayerRange: LoadRangeMode = LoadRangeModes.Both; break;
                    case LoadRangeModes.Both: LoadRangeMode = LoadRangeModes.Neither; break;
                }
            }
            else {
                if (MainSwitchWatcher.instance is MainSwitchWatcher watcher) {
                    watcher.RefreshOther();
                }
            }
        }
        if (PixelGridWidthHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                EnablePixelGrid = true;
                PixelGridWidth = PixelGridWidth switch {
                    < 2 => 2,
                    < 4 => 4,
                    < 8 => 8,
                    _ => 0,
                };
                if (PixelGridWidth == 0) {
                    EnablePixelGrid = false;
                }
            }
            else {
                if (MainSwitchWatcher.instance is MainSwitchWatcher watcher) {
                    watcher.RefreshOther();
                }
            }
        }
        return changed;
    }

    #endregion


}

[AttributeUsage(AttributeTargets.Property)]
public class SettingDoNotSaveAttribute : Attribute {
    // if possible i wanna solve this issue using only such an attribute
}

[AttributeUsage(AttributeTargets.Property)]
public class SettingDescriptionHardcodedAttribute : Attribute {
    public string description() {
        if (Dialog.Language == Dialog.Languages["schinese"]) {
            return TasHelperSettings.MainSwitchThreeStates ? "在 [关 - 默认 - 全部] 三者间切换\n配置其他设置时请在 全部 状态下进行." : "在 [关 - 全部] 两者间切换";
        }
        return TasHelperSettings.MainSwitchThreeStates ? "Switch among [Off - Default - All]\nPlease configure other settings in State All." : "Switch between [Off - All]";
    }
}


public static class TasHelperSettingsAlias {
    public static bool ShowCycleHitboxColors {
        get => TasHelperSettings.ShowCycleHitboxColors;
        set => TasHelperSettings.ShowCycleHitboxColors = value;
    }

    public static UsingNotInViewColorModes UsingNotInViewColorMode {
        get => TasHelperSettings.UsingNotInViewColorMode;
        set => TasHelperSettings.UsingNotInViewColorMode = value;
    }

    public static CountdownModes CountdownMode {
        get => TasHelperSettings.CountdownMode;
        set => TasHelperSettings.CountdownMode = value;
    }

    public static LoadRangeModes LoadRangeMode {
        get => TasHelperSettings.LoadRangeMode;
        set => TasHelperSettings.LoadRangeMode = value;
    }

    public static bool EnableSimplifiedSpinner {
        get => TasHelperSettings.EnableSimplifiedSpinner;
        set => TasHelperSettings.EnableSimplifiedSpinner = value;
    }

    public static bool EntityActivatorReminder {
        get => TasHelperSettings.EntityActivatorReminder;
        set => TasHelperSettings.EntityActivatorReminder = value;
    }

    public static bool UsingCameraTarget {
        get => TasHelperSettings.UsingCameraTarget;
        set => TasHelperSettings.UsingCameraTarget = value;
    }

    public static bool EnablePixelGrid {
        get => TasHelperSettings.EnablePixelGrid;
        set => TasHelperSettings.EnablePixelGrid = value;
    }

    public static bool UsingSpawnPoint {
        get => TasHelperSettings.UsingSpawnPoint;
        set => TasHelperSettings.UsingSpawnPoint = value;
    }
}

