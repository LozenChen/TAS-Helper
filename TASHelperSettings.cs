using Microsoft.Xna.Framework.Input;
using TAS.EverestInterop;
using TAS.Module;
using static TAS.EverestInterop.Hotkeys;

namespace Celeste.Mod.TASHelper;

[SettingName("TAS_HELPER_NAME")]
public class TASHelperSettings : EverestModuleSettings {

    public static TASHelperSettings Instance { get; private set; }

    public TASHelperSettings() {
        Instance = this;
    }

    private bool enabled = true;

    public bool Enabled { get => enabled; set => enabled = value; }

    #region Spinner Settings

    #region SpinnerMainSwitch

    private void EnabledEnforceRaiseSettings(bool raiseAll) {
        ShowCycleHitboxColors = ShowCycleHitboxColors;
        EnableSimplifiedSpinner = EnableSimplifiedSpinner;
        if (raiseAll) {
            CountdownMode = CountdownMode;
            LoadRangeMode = LoadRangeMode;
        }
    }

    public enum MainSwitchModes { Off, OnlyDefault, AllowAll }

    private MainSwitchModes mainSwitch = MainSwitchModes.OnlyDefault;

    public MainSwitchModes MainSwitch {
        get => mainSwitch;
        set {
            mainSwitch = value;
            Enabled = (value != MainSwitchModes.Off);
            if (value == MainSwitchModes.AllowAll) {
                MainSwitch2All();
            }
            else {
                MainSwitch2Default();
            }
            if (Enabled) {
                EnabledEnforceRaiseSettings(value == MainSwitchModes.AllowAll);
                if (value == MainSwitchModes.OnlyDefault) {
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
            CountdownMode = CountdownModes.Activation;
        }
        if (LoadRangeMode == LoadRangeModes.Neither) {
            LoadRangeMode = LoadRangeModes.Both;
        }
    }
    #endregion

    #region CycleHitboxColor
    public bool EnableCycleHitboxColors = true;

    private bool showCycleHitboxColor = true;

    [SettingName("SHOW_CYCLE_HITBOX_COLOR")]
    public bool ShowCycleHitboxColors {
        get => Enabled && EnableCycleHitboxColors && showCycleHitboxColor;
        set { if (Enabled) showCycleHitboxColor = value; }
    }
    #endregion

    #region Countdown
    public bool EnableCountdownModes = false;
    public enum CountdownModes { Off, Activation, Deactivation };

    private CountdownModes countdownMode = CountdownModes.Activation;

    [SettingSubText("Activation checks per 3 frames" + "\n" + "Deactivation checks per 15 frames")]
    [SettingName("COUNTDOWN_MODE")]
    public CountdownModes CountdownMode {
        get => Enabled && EnableCountdownModes ? countdownMode : CountdownModes.Off;
        set {
            if (Enabled) {
                countdownMode = value;
                EnableCountdownModes = true;
                UpdateAuxiliaryVariable();
            }
        }
    }

    #endregion

    #region LoadRange

    public bool EnableLoadRange = false;
    public enum LoadRangeModes { Neither, InViewRange, NearPlayerRange, Both };

    private LoadRangeModes loadRangeMode = LoadRangeModes.Both;
    [SettingSubText("InView: inside the 352px*212px rectangle around camera" + "\n" + "NearPlayer: inside the 256px*256px square around player")]
    [SettingName("LOAD_RANGE_MODE")]
    public LoadRangeModes LoadRangeMode {
        get => Enabled && EnableLoadRange ? loadRangeMode : LoadRangeModes.Neither;
        set {
            if (Enabled) {
                loadRangeMode = value;
                EnableLoadRange = true;
                UpdateAuxiliaryVariable();
            }
        }
    }

    [SettingRange(0, 32)]
    [SettingName("IN_VIEW_RANGE_WIDTH")]
    [SettingSubText("When InView Range Width = 16" + "\n" + "It matches Celeste TAS's Camera Hitboxes")]
    public int InViewRangeWidth { get; set; } = 16;

    [SettingRange(1, 16)]
    [SettingName("NEAR_PLAYER_RANGE_WIDTH")]
    public int NearPlayerRangeWidth { get; set; } = 8;

    [SettingRange(0, 9)]
    [SettingName("LOAD_RANGE_OPACITY")]
    public int LoadRangeOpacity { get; set; } = 4;

    #endregion

    #region Simplified Spinner

    public bool EnableEnableSimplifiedSpinner = true;
    // actually this term is forever true, in current setting
    [SettingIgnore]
    private bool enableSimplifiedSpinner { get; set; } = true;

    [SettingName("SIMPLIFIED_SPINNERS")]
    public bool EnableSimplifiedSpinner {
        get => Enabled && EnableEnableSimplifiedSpinner && enableSimplifiedSpinner;
        set {
            if (Enabled) {
                enableSimplifiedSpinner = value;
                EnableEnableSimplifiedSpinner = true;
            }
        }
    }

    public enum ClearSpritesMode {WhenSimplifyGraphics, Always};

    private ClearSpritesMode enforceClearSprites = ClearSpritesMode.Always;

    [SettingName("CLEAR_SPINNER_SPRITES")]
    public ClearSpritesMode EnforceClearSprites {
        get => enforceClearSprites;
        set => enforceClearSprites = value;
    } 
    public bool ClearSpinnerSprites => CelesteTasSettings.Instance.SimplifiedGraphics || EnforceClearSprites == ClearSpritesMode.Always;

    [SettingRange(0, 9)]
    [SettingName("SPINNER_FILLER_OPACITY")]
    public int SpinnerFillerOpacity { get; set; } = 4;
    #endregion

    #region Auxilary Variables
    public void UpdateAuxiliaryVariable() {
        isUsingCountDown = (CountdownMode != CountdownModes.Off);
        if (CountdownMode == CountdownModes.Activation) {
            SpinnerCountdownUpperBound = 9;
            SpinnerInterval = 0.05f;
        }
        else {
            SpinnerCountdownUpperBound = 99;
            SpinnerInterval = 0.25f;
        }
        isUsingLoadRange = isUsingInViewRange = isUsingNearPlayerRange = false;
        if (LoadRangeMode == LoadRangeModes.Neither) {
            // do nothing
        }
        else {
            isUsingLoadRange = true;
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
    public bool isUsingLoadRange = true;
    public bool isUsingInViewRange = true;
    public bool isUsingNearPlayerRange = true;
    public int SpinnerCountdownUpperBound = 9;
    public float SpinnerInterval = 0.05f;
    public float RangeAlpha = 0.4f;
    public float SpinnerFillerAlpha = 0.4f;
    #endregion

    #endregion

    // todo: to be governed by main switch? maybe no, coz it's not very about spinner stun

    private bool usingCameraTarget = false;

    [SettingName("CAMERA_TARGET")]
    public bool UsingCameraTarget {
        get => Enabled && usingCameraTarget;
        set => usingCameraTarget = value;
    }


    #region HotKey
    [SettingIgnore]
    private static ButtonBinding keyMainSwitch { get; set; } = new(0, Keys.LeftControl, Keys.E);
    [SettingIgnore]
    private static ButtonBinding keyCountDown { get; set; } = new(0, Keys.LeftControl, Keys.R);
    [SettingIgnore]
    private static ButtonBinding keyLoadRange { get; set; } = new(0, Keys.LeftControl, Keys.T);

    [SettingSubHeader("LOZEN_TASHELPER_HOTKEY_DESCRIPTION")]
    // [DefaultButtonBinding2(0, Keys.LeftControl, Keys.E)]
    [SettingName("MAIN_SWITCH")]
    public ButtonBinding KeyMainSwitch {
        get => keyMainSwitch;
        set {
            keyMainSwitch = value;
            MainSwitchHK = new Hotkey(keyMainSwitch.Keys, keyMainSwitch.Buttons, true, false);
        }
    }


    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.R)]
    [SettingName("SWITCH_COUNT_DOWN")]
    public ButtonBinding KeyCountDown {
        get => keyCountDown;
        set {
            keyCountDown = value;
            CountDownHK = new Hotkey(keyCountDown.Keys, keyCountDown.Buttons, true, false);
        }
    }

    [SettingName("SWITCH_LOAD_RANGE")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.T)]
    public ButtonBinding KeyLoadRange {
        get => keyLoadRange;
        set {
            keyLoadRange = value;
            LoadRangeHK = new Hotkey(keyLoadRange.Keys, keyLoadRange.Buttons, true, false);
        }
    }

    [SettingIgnore]
    public Hotkey MainSwitchHK { get; private set; } = new Hotkey(keyMainSwitch.Keys, keyMainSwitch.Buttons, true, false);

    [SettingIgnore]
    public Hotkey CountDownHK { get; private set; } = new Hotkey(keyCountDown.Keys, keyCountDown.Buttons, true, false);

    [SettingIgnore]
    public Hotkey LoadRangeHK { get; private set; } = new Hotkey(keyLoadRange.Keys, keyLoadRange.Buttons, true, false);

    public void SettingsHotkeysPressed() {
        MainSwitchHK.Update();
        CountDownHK.Update();
        LoadRangeHK.Update();
        if (MainSwitchHK.Pressed) {
            switch (MainSwitch) {
                case MainSwitchModes.Off: MainSwitch = MainSwitchModes.OnlyDefault; break;
                case MainSwitchModes.OnlyDefault: MainSwitch = MainSwitchModes.AllowAll; break;
                case MainSwitchModes.AllowAll: MainSwitch = MainSwitchModes.Off; break;
            }
        }
        if (Enabled) {
            if (CountDownHK.Pressed) {
                switch (CountdownMode) {
                    case CountdownModes.Off: CountdownMode = CountdownModes.Activation; break;
                    case CountdownModes.Activation: CountdownMode = CountdownModes.Deactivation; break;
                    case CountdownModes.Deactivation: CountdownMode = CountdownModes.Off; break;
                }
            }
            if (LoadRangeHK.Pressed) {
                switch (LoadRangeMode) {
                    case LoadRangeModes.Neither: LoadRangeMode = LoadRangeModes.InViewRange; break;
                    case LoadRangeModes.InViewRange: LoadRangeMode = LoadRangeModes.NearPlayerRange; break;
                    case LoadRangeModes.NearPlayerRange: LoadRangeMode = LoadRangeModes.Both; break;
                    case LoadRangeModes.Both: LoadRangeMode = LoadRangeModes.Neither; break;
                }
            }
        }
    }

    #endregion




}

