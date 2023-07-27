using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Utils.Menu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using TAS.EverestInterop;
using YamlDotNet.Serialization;
using static TAS.EverestInterop.Hotkeys;

namespace Celeste.Mod.TASHelper.Module;

[SettingName("TAS_HELPER_NAME")]
public class TASHelperSettings : EverestModuleSettings {

    public static TASHelperSettings Instance { get; private set; }

    public TASHelperSettings() {
        Instance = this;
    }

    internal void OnLoadSettings() {
        // Everest will save & load settings of public fields/properties when open the game
        // but in the Awake system, only those private field like showCycleHitboxColor matters
        // so i hack SaveSettings, upgrade to YamlDotNet9 to support Serializing private properties
        // also i mark those public fields as YamlIgnore, as there's no need to save them

        UpdateAuxiliaryVariable();

        if (keyMainSwitch is null) {
            keyMainSwitch = new((Buttons)0, Keys.LeftControl, Keys.E);
        }
        if (keyCountDown is null) {
            keyCountDown = new((Buttons)0, Keys.LeftControl, Keys.R);
        }
        if (keyLoadRange is null) {
            keyLoadRange = new((Buttons)0, Keys.LeftControl, Keys.T);
        }
        if (keyPixelGridWidth is null) {
            keyPixelGridWidth = new((Buttons)0, Keys.LeftControl, Keys.F);
        }
        // it seems some bug can happen with deserialization (though not for me, so i add these codes in case of accident)

        MainSwitchHotkey = new Hotkey(keyMainSwitch.Keys, keyMainSwitch.Buttons, true, false);
        CountDownHotkey = new Hotkey(keyCountDown.Keys, keyCountDown.Buttons, true, false);
        LoadRangeHotkey = new Hotkey(keyLoadRange.Keys, keyLoadRange.Buttons, true, false);
        PixelGridWidthHotkey = new Hotkey(keyPixelGridWidth.Keys, keyPixelGridWidth.Buttons, true, false);
    }

    public bool Enabled = true;

    #region MainSwitch

    // it will only affect main options, will not affect suboptions (which will not work if corresponding main option is not on)

    public enum MainSwitchModes { Off, OnlyDefault, AllowAll }

    private MainSwitchModes mainSwitch { get; set; } = MainSwitchModes.AllowAll;

    [YamlIgnore]
    public MainSwitchModes MainSwitch {
        get => mainSwitch;
        set {
            if (mainSwitch == value) {
                // prevent infinite recursion
                return;
            }
            mainSwitch = value;
            MainSwitchWatcher.instance?.Refresh();
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
        Awake_FireBallTrack = false;
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
        Awake_FireBallTrack = true;
    }

    #endregion

    #region CycleHitboxColor
    public bool Awake_CycleHitboxColors = true;

    // we need to make it public, so this setting is stored
    // though we don't want anyone to visit it directly...

    private bool showCycleHitboxColor { get; set; } = true;

    [YamlIgnore]
    public bool ShowCycleHitboxColors {
        get => Enabled && Awake_CycleHitboxColors && showCycleHitboxColor;
        set {
            showCycleHitboxColor = value;
            Awake_CycleHitboxColors = true;
        }
    }

    public bool Awake_UsingNotInViewColor = true;
    public enum UsingNotInViewColorModes { Off, WhenUsingInViewRange, Always };

    private UsingNotInViewColorModes usingNotInViewColorMode { get; set; } = UsingNotInViewColorModes.WhenUsingInViewRange;

    [YamlIgnore]
    public UsingNotInViewColorModes UsingNotInViewColorMode {
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

    private CountdownModes countdownMode { get; set; } = CountdownModes.Off;

    [YamlIgnore]
    public CountdownModes CountdownMode {
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

    private LoadRangeModes loadRangeMode { get; set; } = LoadRangeModes.Neither;

    [YamlIgnore]
    public LoadRangeModes LoadRangeMode {
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

    private int loadRangeOpacity { get; set; } = 4;

    [YamlIgnore]
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

    private bool enableSimplifiedSpinner { get; set; } = true;

    [YamlIgnore]
    public bool EnableSimplifiedSpinner {
        get => Enabled && Awake_EnableSimplifiedSpinner && enableSimplifiedSpinner;
        set {
            enableSimplifiedSpinner = value;
            Awake_EnableSimplifiedSpinner = true;
        }
    }

    public enum ClearSpritesMode { Off, WhenSimplifyGraphics, Always };

    private ClearSpritesMode enforceClearSprites { get; set; } = ClearSpritesMode.WhenSimplifyGraphics;

    [YamlIgnore]
    public ClearSpritesMode EnforceClearSprites {
        get => enforceClearSprites;
        set => enforceClearSprites = value;
    }

    public bool ClearSpinnerSprites => EnableSimplifiedSpinner && (EnforceClearSprites == ClearSpritesMode.Always || (EnforceClearSprites == ClearSpritesMode.WhenSimplifyGraphics && TasSettings.SimplifiedGraphics));

    private int spinnerFillerOpacity { get; set; } = 2;

    [YamlIgnore]
    public int SpinnerFillerOpacity {
        get => spinnerFillerOpacity;
        set {
            spinnerFillerOpacity = value;
            SpinnerFillerAlpha = value * 0.1f;
        }
    }
    #endregion


    public bool Awake_EntityActivatorReminder = true;

    private bool entityActivatorReminder { get; set; } = true;

    [YamlIgnore]
    public bool EntityActivatorReminder {
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

    private bool usingCameraTarget { get; set; } = false;

    [YamlIgnore]
    public bool UsingCameraTarget {
        get => Enabled && Awake_CameraTarget && usingCameraTarget;
        set {
            usingCameraTarget = value;
            Awake_CameraTarget = true;
        }
    }

    public int CameraTargetLinkOpacity { get; set; } = 6;

    public bool Awake_PixelGrid = true;

    private bool enablePixelGrid { get; set; } = false;

    [YamlIgnore]
    public bool EnablePixelGrid {
        get => Enabled && Awake_PixelGrid && enablePixelGrid;
        set {
            enablePixelGrid = value;
            Awake_PixelGrid = true;
        }
    }

    public int PixelGridWidth = 2;
    public int PixelGridOpacity { get; set; } = 8;

    public bool Awake_SpawnPoint = true;

    private bool usingSpawnPoint { get; set; } = true;

    [YamlIgnore]
    public bool UsingSpawnPoint {
        get => Enabled && Awake_SpawnPoint && usingSpawnPoint;
        set {
            usingSpawnPoint = value;
            Awake_SpawnPoint = true;
        }
    }

    public int CurrentSpawnPointOpacity = 5;

    public int OtherSpawnPointOpacity = 2;

    public bool Awake_FireBallTrack = true;

    private bool usingFireBallTrack { get; set; } = false;

    [YamlIgnore]
    public bool UsingFireBallTrack {
        get => Enabled && Awake_FireBallTrack && usingFireBallTrack;
        set {
            usingFireBallTrack = value;
            Awake_FireBallTrack = true;
        }
    }

    public bool AllowEnableModWithMainSwitch = true;

    private bool mainSwitchStateVisualize { get; set; } = true;

    [YamlIgnore]
    public bool MainSwitchStateVisualize {
        get => mainSwitchStateVisualize;
        set {
            mainSwitchStateVisualize = value;
            if (MainSwitchWatcher.instance is MainSwitchWatcher watcher) {
                watcher.Visible = mainSwitchStateVisualize;
            }
        }
    }

    private bool mainSwitchThreeStates { get; set; } = false;

    [YamlIgnore]
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

    private ButtonBinding keyMainSwitch { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.E);
    private ButtonBinding keyCountDown { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.R);
    private ButtonBinding keyLoadRange { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.T);
    private ButtonBinding keyPixelGridWidth { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.F);

    [YamlIgnore]
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

    [YamlIgnore]
    [SettingName("TAS_HELPER_SWITCH_COUNT_DOWN_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.R)]
    public ButtonBinding KeyCountDown {
        get => keyCountDown;
        set {
            keyCountDown = value;
            CountDownHotkey = new Hotkey(keyCountDown.Keys, keyCountDown.Buttons, true, false);
        }
    }

    [YamlIgnore]
    [SettingName("TAS_HELPER_SWITCH_LOAD_RANGE_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.T)]
    public ButtonBinding KeyLoadRange {
        get => keyLoadRange;
        set {
            keyLoadRange = value;
            LoadRangeHotkey = new Hotkey(keyLoadRange.Keys, keyLoadRange.Buttons, true, false);
        }
    }

    [YamlIgnore]
    [SettingName("TAS_HELPER_SWITCH_PIXEL_GRID_WIDTH_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.F)]
    public ButtonBinding KeyPixelGridWidth {
        get => keyPixelGridWidth;
        set {
            keyPixelGridWidth = value;
            PixelGridWidthHotkey = new Hotkey(keyPixelGridWidth.Keys, keyPixelGridWidth.Buttons, true, false);
        }
    }

    [YamlIgnore]
    private Hotkey MainSwitchHotkey;

    [YamlIgnore]
    private Hotkey CountDownHotkey;

    [YamlIgnore]
    private Hotkey LoadRangeHotkey;

    [YamlIgnore]
    private Hotkey PixelGridWidthHotkey;

    // should not use a List<Hotkey> var, coz changing KeyPixelGridWidth will cause the hotkey get newed
    public bool SettingsHotkeysPressed() {
        if (Engine.Scene is not Level level) {
            return false;
        }

        bool updateKey = true;
        bool updateButton = true;
        try {
            bool InOuiModOption = TASHelperMenu.mainItem?.Container?.Focused is bool b && b;
            if (InOuiModOption || (level.Tracker.Entities.TryGetValue(typeof(KeyboardConfigUI), out var list) && list.Count > 0) ||
                (level.Tracker.Entities.TryGetValue(typeof(ModuleSettingsKeyboardConfigUIExt), out var list2) && list2.Count > 0)) {
                updateKey = false;
            }
            if (InOuiModOption || (level.Tracker.Entities.TryGetValue(typeof(ButtonConfigUI), out var list3) && list3.Count > 0)) {
                updateButton = false;
            }
        }
        catch (Exception ex1) {
            Logger.Log(LogLevel.Error, "TASHelper", "PossibleBugPlace1");
            Logger.LogDetailed(ex1);
        }
        try {
            MainSwitchHotkey.Update(updateKey, updateButton);
            CountDownHotkey.Update(updateKey, updateButton);
            LoadRangeHotkey.Update(updateKey, updateButton);
            PixelGridWidthHotkey.Update(updateKey, updateButton);
        }
        catch (Exception ex2) {
            Logger.Log(LogLevel.Error, "TASHelper", "PossibleBugPlace2");
            Logger.LogDetailed(ex2);
        }
        bool changed = false;
        try {
            if (MainSwitchHotkey.Pressed) {
                changed = true;
                switch (MainSwitch) {
                    case MainSwitchModes.Off: {
                            if (!AllowEnableModWithMainSwitch) {
                                changed = false;
                                MainSwitchWatcher.instance?.Refresh(true);
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
                    MainSwitchWatcher.instance?.RefreshOther();
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
                    MainSwitchWatcher.instance?.RefreshOther();
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
                    MainSwitchWatcher.instance?.RefreshOther();
                }
            }
        }
        catch (Exception ex3) {
            Logger.Log(LogLevel.Error, "TASHelper", "PossibleBugPlace3");
            Logger.LogDetailed(ex3);
        }
        return changed;
    }

    #endregion


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


