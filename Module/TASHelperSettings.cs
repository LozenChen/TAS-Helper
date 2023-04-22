using Microsoft.Xna.Framework.Input;
using TAS.EverestInterop;
using static TAS.EverestInterop.Hotkeys;

namespace Celeste.Mod.TASHelper.Module;

[SettingName("TAS_HELPER_NAME")]
public class TASHelperSettings : EverestModuleSettings {

    public static TASHelperSettings Instance { get; private set; }

    public TASHelperSettings() {
        Instance = this;
    }

    public void InitializeSettings() {
        UpdateAuxiliaryVariable();
    }

    private bool enabled = true;

    public bool Enabled { get => enabled; set => enabled = value; }

    #region Spinner Settings

    private bool spinnerEnabled = true;

    public bool SpinnerEnabled {
        get => Enabled && spinnerEnabled;
        private set => spinnerEnabled = value;
    }

    #region SpinnerMainSwitch

    // it will only affect main options, will not affect suboptions (which will not work if corresponding main option is not on)

    public enum SpinnerMainSwitchModes { Off, OnlyDefault, AllowAll }

    private SpinnerMainSwitchModes spinnerMainSwitch = SpinnerMainSwitchModes.OnlyDefault;

    public SpinnerMainSwitchModes SpinnerMainSwitch {
        get => spinnerMainSwitch;
        set {
            if (!Enabled) {
                return;
            }
            spinnerMainSwitch = value;
            switch (value) {
                case SpinnerMainSwitchModes.Off:
                    Sleep();
                    UpdateAuxiliaryVariable();
                    return;
                default:
                    Awake(value == SpinnerMainSwitchModes.AllowAll);
                    break;
            }
            FurthurAwake(value == SpinnerMainSwitchModes.AllowAll);
            RaiseSettings(value == SpinnerMainSwitchModes.AllowAll);

            // some of the setters are not called, so the auxiliary variables don't get update, we have to update them
            // and we can't call the setters, which will otherwise break the Awake...s
            UpdateAuxiliaryVariable();
        }
    }
    private void Sleep() {
        Awake_CycleHitboxColors = false;
        Awake_UsingNotInViewColor = false;
        Awake_EnableSimplifiedSpinner = false;
        Awake_CountdownModes = false;
        Awake_LoadRange = false;
    }
    private void Awake(bool awakeAll) {
        Awake_CycleHitboxColors = true;
        Awake_UsingNotInViewColor = true;
        Awake_EnableSimplifiedSpinner = true;
        Awake_CountdownModes = awakeAll;
        Awake_LoadRange = awakeAll;
    }
    private void FurthurAwake(bool awakeAll) {
        ShowCycleHitboxColors = ShowCycleHitboxColors;
        UsingNotInViewColorMode = UsingNotInViewColorMode;
        EnableSimplifiedSpinner = EnableSimplifiedSpinner;
        if (awakeAll) {
            CountdownMode = CountdownMode;
            LoadRangeMode = LoadRangeMode;
        }
    }
    private void RaiseSettings(bool raiseAll) {
        ShowCycleHitboxColors = true;
        EnableSimplifiedSpinner = true;
        if (UsingNotInViewColorMode == UsingNotInViewColorModes.Off) {
            UsingNotInViewColorMode = UsingNotInViewColorModes.WhenUsingInViewRange;
        }
        if (raiseAll) {
            if (CountdownMode == CountdownModes.Off) {
                CountdownMode = CountdownModes._3fCycle;
            }
            if (LoadRangeMode == LoadRangeModes.Neither) {
                LoadRangeMode = LoadRangeModes.Both;
            }
        }
    }

    #endregion

    #region CycleHitboxColor
    public bool Awake_CycleHitboxColors = true;

    private bool showCycleHitboxColor = true;

    public bool ShowCycleHitboxColors {
        get => SpinnerEnabled && Awake_CycleHitboxColors && showCycleHitboxColor;
        set {
            showCycleHitboxColor = value;
            Awake_CycleHitboxColors = true;
            if (value) {
                SpinnerEnabled = true;
            }
        }
    }

    public bool Awake_UsingNotInViewColor = true;
    public enum UsingNotInViewColorModes { Off, WhenUsingInViewRange, Always };

    private UsingNotInViewColorModes usingNotInViewColorMode = UsingNotInViewColorModes.WhenUsingInViewRange;

    public UsingNotInViewColorModes UsingNotInViewColorMode {
        get => Enabled && Awake_UsingNotInViewColor ? usingNotInViewColorMode : UsingNotInViewColorModes.Off;
        set {
            usingNotInViewColorMode = value;
            Awake_UsingNotInViewColor = true;
            UsingNotInViewColor = (value == UsingNotInViewColorModes.Always) || (value == UsingNotInViewColorModes.WhenUsingInViewRange && UsingInViewRange);
            if (UsingNotInViewColor) {
                SpinnerEnabled = true;
            }
        }
    }

    public bool UsingNotInViewColor = true;

    #endregion

    #region Countdown
    public bool Awake_CountdownModes = false;
    public enum CountdownModes { Off, _3fCycle, _15fCycle };

    private CountdownModes countdownMode = CountdownModes._3fCycle;

    public CountdownModes CountdownMode {
        get => Enabled && Awake_CountdownModes ? countdownMode : CountdownModes.Off;
        set {
            countdownMode = value;
            Awake_CountdownModes = true;

            UsingCountDown = (CountdownMode != CountdownModes.Off);
            if (CountdownMode == CountdownModes._3fCycle) {
                SpinnerCountdownUpperBound = 9;
                SpinnerInterval = 0.05f;
            }
            else {
                SpinnerCountdownUpperBound = 99;
                SpinnerInterval = 0.25f;
            }

            if (UsingCountDown) {
                SpinnerEnabled = true;
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

    public bool Awake_LoadRange = false;
    public enum LoadRangeModes { Neither, InViewRange, NearPlayerRange, Both };

    private LoadRangeModes loadRangeMode = LoadRangeModes.Both;

    public LoadRangeModes LoadRangeMode {
        get => Enabled && Awake_LoadRange ? loadRangeMode : LoadRangeModes.Neither;
        set {
            loadRangeMode = value;
            Awake_LoadRange = true;

            UsingLoadRange = LoadRangeMode != LoadRangeModes.Neither;
            UsingInViewRange = LoadRangeMode == LoadRangeModes.InViewRange || LoadRangeMode == LoadRangeModes.Both;
            UsingNearPlayerRange = LoadRangeMode == LoadRangeModes.NearPlayerRange || LoadRangeMode == LoadRangeModes.Both;
            UsingNotInViewColor = (UsingNotInViewColorMode == UsingNotInViewColorModes.Always) || (UsingNotInViewColorMode == UsingNotInViewColorModes.WhenUsingInViewRange && UsingInViewRange);

            if (UsingLoadRange) {
                SpinnerEnabled = true;
            }
        }
    }

    [SettingRange(0, 32)]
    public int InViewRangeWidth { get; set; } = 16;

    [SettingRange(1, 16)]
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
    // actually this term is forever true, in current setting
    [SettingIgnore]
    private bool enableSimplifiedSpinner { get; set; } = true;

    public bool EnableSimplifiedSpinner {
        get => Enabled && Awake_EnableSimplifiedSpinner && enableSimplifiedSpinner;
        set {
            enableSimplifiedSpinner = value;
            Awake_EnableSimplifiedSpinner = true;
            if (value) {
                SpinnerEnabled = true;
            }
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

    private bool entityActivatorReminder = true;

    public bool EntityActivatorReminder {
        get => Enabled && entityActivatorReminder;
        set => entityActivatorReminder = value;
    }

    #region Auxilary Variables
    public void UpdateAuxiliaryVariable() {
        // update the variables associated to variables govern by spinner main switch
        // it can happen their value is changed but not via the setter (i.e. change the Awake_...s)

        UsingNotInViewColor = (UsingNotInViewColorMode == UsingNotInViewColorModes.Always) || (UsingNotInViewColorMode == UsingNotInViewColorModes.WhenUsingInViewRange && UsingInViewRange);
        UsingCountDown = (CountdownMode != CountdownModes.Off);
        if (CountdownMode == CountdownModes._3fCycle) {
            SpinnerCountdownUpperBound = 9;
            SpinnerInterval = 0.05f;
        }
        else {
            SpinnerCountdownUpperBound = 99;
            SpinnerInterval = 0.25f;
        }
        UsingLoadRange = (LoadRangeMode != LoadRangeModes.Neither);
        UsingInViewRange = (LoadRangeMode == LoadRangeModes.InViewRange || LoadRangeMode == LoadRangeModes.Both);
        UsingNearPlayerRange = (LoadRangeMode == LoadRangeModes.NearPlayerRange || LoadRangeMode == LoadRangeModes.Both);
        SpinnerEnabled = ShowCycleHitboxColors || UsingNotInViewColor || UsingLoadRange || UsingCountDown || EnableSimplifiedSpinner;
    }
    public bool UsingCountDown = false;
    public bool UsingLoadRange = true;
    public bool UsingInViewRange = true;
    public bool UsingNearPlayerRange = true;
    public int SpinnerCountdownUpperBound = 9;
    public float SpinnerInterval = 0.05f;
    public float RangeAlpha = 0.4f;
    public float SpinnerFillerAlpha = 0.4f;
    #endregion

    #endregion


    private bool usingCameraTarget = false;

    [SettingName("TAS_HELPER_CAMERA_TARGET")]
    public bool UsingCameraTarget {
        get => Enabled && usingCameraTarget;
        set => usingCameraTarget = value;
    }

    [SettingRange(1, 9)]
    [SettingName("TAS_HELPER_CAMERA_TARGET_VECTOR_OPACITY")]
    public int CameraTargetLinkOpacity { get; set; } = 6;

    private bool enablePixelGrid = false;
    public bool EnablePixelGrid { get => Enabled && enablePixelGrid; set => enablePixelGrid = value; }

    public int PixelGridWidth = 2;

    [SettingRange(1, 10)]
    public int PixelGridOpacity { get; set; } = 8;

    #region HotKey

    private static ButtonBinding keySpinnerMainSwitch { get; set; } = new(0, Keys.LeftControl, Keys.E);
    private static ButtonBinding keyCountDown { get; set; } = new(0, Keys.LeftControl, Keys.R);
    private static ButtonBinding keyLoadRange { get; set; } = new(0, Keys.LeftControl, Keys.T);
    private static ButtonBinding keyPixelGridWidth { get; set; } = new(0, Keys.LeftControl, Keys.F);

    [SettingSubHeader("TAS_HELPER_HOTKEY_DESCRIPTION")]
    [SettingName("TAS_HELPER_MAIN_SWITCH_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.E)]
    public ButtonBinding KeySpinnerMainSwitch {
        get => keySpinnerMainSwitch;
        set {
            keySpinnerMainSwitch = value;
            SpinnerMainSwitchHotkey = new Hotkey(keySpinnerMainSwitch.Keys, keySpinnerMainSwitch.Buttons, true, false);
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

    private Hotkey SpinnerMainSwitchHotkey { get; set; } = new Hotkey(keySpinnerMainSwitch.Keys, keySpinnerMainSwitch.Buttons, true, false);

    private Hotkey CountDownHotkey { get; set; } = new Hotkey(keyCountDown.Keys, keyCountDown.Buttons, true, false);

    private Hotkey LoadRangeHotkey { get; set; } = new Hotkey(keyLoadRange.Keys, keyLoadRange.Buttons, true, false);

    private Hotkey PixelGridWidthHotkey { get; set; } = new Hotkey(keyPixelGridWidth.Keys, keyPixelGridWidth.Buttons, true, false);

    public void SettingsHotkeysPressed() {
        SpinnerMainSwitchHotkey.Update();
        CountDownHotkey.Update();
        LoadRangeHotkey.Update();
        PixelGridWidthHotkey.Update();
        if (SpinnerMainSwitchHotkey.Pressed) {
            switch (SpinnerMainSwitch) {
                case SpinnerMainSwitchModes.Off: SpinnerMainSwitch = SpinnerMainSwitchModes.OnlyDefault; break;
                case SpinnerMainSwitchModes.OnlyDefault: SpinnerMainSwitch = SpinnerMainSwitchModes.AllowAll; break;
                case SpinnerMainSwitchModes.AllowAll: SpinnerMainSwitch = SpinnerMainSwitchModes.Off; break;
            }
        }
        if (CountDownHotkey.Pressed) {
            switch (CountdownMode) {
                case CountdownModes.Off: CountdownMode = CountdownModes._3fCycle; break;
                case CountdownModes._3fCycle: CountdownMode = CountdownModes._15fCycle; break;
                case CountdownModes._15fCycle: CountdownMode = CountdownModes.Off; break;
            }
        }
        if (LoadRangeHotkey.Pressed) {
            switch (LoadRangeMode) {
                case LoadRangeModes.Neither: LoadRangeMode = LoadRangeModes.InViewRange; break;
                case LoadRangeModes.InViewRange: LoadRangeMode = LoadRangeModes.NearPlayerRange; break;
                case LoadRangeModes.NearPlayerRange: LoadRangeMode = LoadRangeModes.Both; break;
                case LoadRangeModes.Both: LoadRangeMode = LoadRangeModes.Neither; break;
            }
        }
        if (PixelGridWidthHotkey.Pressed) {
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
    }

    #endregion


}

