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

    private bool enabled = true;

    public bool Enabled { get => enabled; set => enabled = value; }

    #region Spinner Settings

    private bool spinnerEnabled = true;

    public bool SpinnerEnabled { get => Enabled && spinnerEnabled; set => spinnerEnabled = value; }

    #region SpinnerMainSwitch
    private void EnabledEnforceRaiseSettings(bool raiseAll) {
        ShowCycleHitboxColors = ShowCycleHitboxColors;
        EnableSimplifiedSpinner = EnableSimplifiedSpinner;
        if (raiseAll) {
            CountdownMode = CountdownMode;
            LoadRangeMode = LoadRangeMode;
        }
    }

    public enum SpinnerMainSwitchModes { Off, OnlyDefault, AllowAll }

    private SpinnerMainSwitchModes spinnerMainSwitch = SpinnerMainSwitchModes.OnlyDefault;

    public SpinnerMainSwitchModes SpinnerMainSwitch {
        get => spinnerMainSwitch;
        set {
            if (!Enabled) {
                return;
            }
            spinnerMainSwitch = value;
            SpinnerEnabled = (value != SpinnerMainSwitchModes.Off);
            if (value == SpinnerMainSwitchModes.AllowAll) {
                MainSwitch2All();
            }
            else {
                MainSwitch2Default();
            }
            if (SpinnerEnabled) {
                EnabledEnforceRaiseSettings(value == SpinnerMainSwitchModes.AllowAll);
                if (value == SpinnerMainSwitchModes.OnlyDefault) {
                    MainSwitch2DefaultPart2();
                }
                else {
                    MainSwitch2AllPart2();
                }
            }

            UpdateAuxiliaryVariable();
        }
    }

    private void MainSwitch2Default() {
        // switch to default or off
        EnableCycleHitboxColors = true;
        EnableEnableSimplifiedSpinner = true;
        EnableCountdownModes = false;
        EnableLoadRange = false;
        // use EnableCountdownModes/LoadRange to change the getter, but don't affect the setter. So once setter is called, enable them
    }
    private void MainSwitch2All() {
        EnableCycleHitboxColors = true;
        EnableEnableSimplifiedSpinner = true;
        EnableCountdownModes = true;
        EnableLoadRange = true;
    }
    private void MainSwitch2DefaultPart2() {
        // switch to default
        ShowCycleHitboxColors = true;
        EnableSimplifiedSpinner = true;
    }
    private void MainSwitch2AllPart2() {
        ShowCycleHitboxColors = true;
        EnableSimplifiedSpinner = true;
        if (CountdownMode == CountdownModes.Off) {
            CountdownMode = CountdownModes._3fCycle;
        }
        if (LoadRangeMode == LoadRangeModes.Neither) {
            LoadRangeMode = LoadRangeModes.Both;
        }
    }
    #endregion

    #region CycleHitboxColor
    public bool EnableCycleHitboxColors = true;

    private bool showCycleHitboxColor = true;

    public bool ShowCycleHitboxColors {
        get => SpinnerEnabled && EnableCycleHitboxColors && showCycleHitboxColor;
        set {
            showCycleHitboxColor = value;
            if (value) {
                SpinnerEnabled = true;
            }
        }
    }
    #endregion

    #region Countdown
    public bool EnableCountdownModes = false;
    public enum CountdownModes { Off, _3fCycle, _15fCycle };

    private CountdownModes countdownMode = CountdownModes._3fCycle;

    public CountdownModes CountdownMode {
        get => SpinnerEnabled && EnableCountdownModes ? countdownMode : CountdownModes.Off;
        set {
            countdownMode = value;
            EnableCountdownModes = true;
            if (value != CountdownModes.Off) {
                SpinnerEnabled = true;
            }
            UpdateAuxiliaryVariable();
        }
    }

    #endregion

    #region LoadRange

    public bool EnableLoadRange = false;
    public enum LoadRangeModes { Neither, InViewRange, NearPlayerRange, Both };

    private LoadRangeModes loadRangeMode = LoadRangeModes.Both;

    public LoadRangeModes LoadRangeMode {
        get => SpinnerEnabled && EnableLoadRange ? loadRangeMode : LoadRangeModes.Neither;
        set {
            loadRangeMode = value;
            EnableLoadRange = true;
            if (value != LoadRangeModes.Neither) {
                SpinnerEnabled = true;
            }
            UpdateAuxiliaryVariable();

        }
    }

    [SettingRange(0, 32)]
    public int InViewRangeWidth { get; set; } = 16;

    [SettingRange(1, 16)]
    public int NearPlayerRangeWidth { get; set; } = 8;

    [SettingRange(0, 9)]
    public int LoadRangeOpacity { get; set; } = 4;

    public bool ApplyCameraZoom { get; set; } = false;

    #endregion

    #region Simplified Spinner

    public bool EnableEnableSimplifiedSpinner = true;
    // actually this term is forever true, in current setting
    [SettingIgnore]
    private bool enableSimplifiedSpinner { get; set; } = true;

    public bool EnableSimplifiedSpinner {
        get => SpinnerEnabled && EnableEnableSimplifiedSpinner && enableSimplifiedSpinner;
        set {
            enableSimplifiedSpinner = value;
            EnableEnableSimplifiedSpinner = true;
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

    [SettingRange(0, 9)]
    public int SpinnerFillerOpacity { get; set; } = 3;
    #endregion

    private bool entityActivatorReminder = true;

    public bool EntityActivatorReminder {
        get => Enabled && entityActivatorReminder;
        set => entityActivatorReminder = value;
    }

    #region Auxilary Variables
    public void UpdateAuxiliaryVariable() {
        isUsingCountDown = (CountdownMode != CountdownModes.Off);
        if (CountdownMode == CountdownModes._3fCycle) {
            SpinnerCountdownUpperBound = 9;
            SpinnerInterval = 0.05f;
        }
        else {
            SpinnerCountdownUpperBound = 99;
            SpinnerInterval = 0.25f;
        }
        UsingLoadRange = isUsingInViewRange = isUsingNearPlayerRange = false;
        if (LoadRangeMode == LoadRangeModes.Neither) {
            // do nothing
        }
        else {
            UsingLoadRange = true;
            if (LoadRangeMode != LoadRangeModes.NearPlayerRange) {
                isUsingInViewRange = true;
            }
            if (LoadRangeMode != LoadRangeModes.InViewRange) {
                isUsingNearPlayerRange = true;
            }
        }
        RangeAlpha = TasHelperSettings.LoadRangeOpacity * 0.1f;
        SpinnerFillerAlpha = TasHelperSettings.SpinnerFillerOpacity * 0.1f;
    }
    public bool isUsingCountDown = false;
    public bool UsingLoadRange = true;
    public bool isUsingInViewRange = true;
    public bool isUsingNearPlayerRange = true;
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

    private bool enablePixelGrid = true;
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

    public Hotkey SpinnerMainSwitchHotkey { get; private set; } = new Hotkey(keySpinnerMainSwitch.Keys, keySpinnerMainSwitch.Buttons, true, false);

    public Hotkey CountDownHotkey { get; private set; } = new Hotkey(keyCountDown.Keys, keyCountDown.Buttons, true, false);

    public Hotkey LoadRangeHotkey { get; private set; } = new Hotkey(keyLoadRange.Keys, keyLoadRange.Buttons, true, false);

    public Hotkey PixelGridWidthHotkey { get; private set; } = new Hotkey(keyPixelGridWidth.Keys, keyPixelGridWidth.Buttons, true, false);

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

