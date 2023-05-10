using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
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

    #region MainSwitch

    // it will only affect main options, will not affect suboptions (which will not work if corresponding main option is not on)

    public enum MainSwitchModes { Off, OnlyDefault, AllowAll }

    private MainSwitchModes mainSwitch = MainSwitchModes.OnlyDefault;

    public MainSwitchModes MainSwitch {
        get => mainSwitch;
        set {
            mainSwitch = value;
            switch (value) {
                case MainSwitchModes.Off:
                    Sleep();
                    break;
                default:
                    Awake(value == MainSwitchModes.AllowAll);
                    break;
            }
            // the setters are not called, so the auxiliary variables don't get update, we have to update them
            // we can't call the setters, which will otherwise break the Awake...s
            UpdateAuxiliaryVariable();
            return;
        }
    }
    private void Sleep() {
        Enabled = false;
        Awake_CycleHitboxColors = false;
        Awake_UsingNotInViewColor = false;
        Awake_EnableSimplifiedSpinner = false;
        Awake_CountdownModes = false;
        Awake_LoadRange = false;
        Awake_CameraTarget= false;
        Awake_PixelGrid =false;
        Awake_SpawnPoint=false;
        Awake_EntityActivatorReminder= false;
    }
    internal void Awake(bool awakeAll) {
        Enabled = true;
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

    private bool showCycleHitboxColor = true;

    public bool ShowCycleHitboxColors {
        get => Enabled && Awake_CycleHitboxColors && showCycleHitboxColor;
        set {
            showCycleHitboxColor = value;
            Awake_CycleHitboxColors = true;
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

    public bool Awake_EntityActivatorReminder = true;
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

    private bool usingCameraTarget = false;

    public bool Awake_CameraTarget = true;
    public bool UsingCameraTarget {
        get => Enabled && Awake_CameraTarget && usingCameraTarget;
        set {
            usingCameraTarget = value;
            Awake_CameraTarget = true;
        }
    }

    [SettingRange(1, 9)]
    [SettingName("TAS_HELPER_CAMERA_TARGET_VECTOR_OPACITY")]
    public int CameraTargetLinkOpacity { get; set; } = 6;

    private bool enablePixelGrid = false;

    public bool Awake_PixelGrid = true;
    public bool EnablePixelGrid { get => Enabled && Awake_PixelGrid && enablePixelGrid; set { enablePixelGrid = value; Awake_PixelGrid = true; } }

    public int PixelGridWidth = 2;

    [SettingRange(1, 10)]
    public int PixelGridOpacity { get; set; } = 8;

    private bool usingSpawnPoint = true;

    public bool Awake_SpawnPoint = true;
    public bool UsingSpawnPoint {
        get => Enabled && Awake_SpawnPoint && usingSpawnPoint;
        set {
            usingSpawnPoint = value;
            Awake_SpawnPoint = true;
        }
    }

    public int CurrentSpawnPointOpacity = 5;

    public int OtherSpawnPointOpacity = 2;

    public bool EnableModWithMainSwitch = true;

    public bool MainSwitchStateVisualize = true;

    #endregion

    #region HotKey

    private static ButtonBinding keyMainSwitch { get; set; } = new(0, Keys.LeftControl, Keys.E);
    private static ButtonBinding keyCountDown { get; set; } = new(0, Keys.LeftControl, Keys.R);
    private static ButtonBinding keyLoadRange { get; set; } = new(0, Keys.LeftControl, Keys.T);
    private static ButtonBinding keyPixelGridWidth { get; set; } = new(0, Keys.LeftControl, Keys.F);

    [SettingSubHeader("TAS_HELPER_HOTKEY_DESCRIPTION")]
    [SettingName("TAS_HELPER_MAIN_SWITCH_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.E)]
    public ButtonBinding KeySpinnerMainSwitch {
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

    public void SettingsHotkeysPressed() {
        MainSwitchHotkey.Update();
        CountDownHotkey.Update();
        LoadRangeHotkey.Update();
        PixelGridWidthHotkey.Update();
        if (MainSwitchHotkey.Pressed) {
            switch (MainSwitch) {
                case MainSwitchModes.Off: {
                        if (EnableModWithMainSwitch) {
                            MainSwitch = MainSwitchModes.OnlyDefault; 
                        }
                        break;
                    }
                case MainSwitchModes.OnlyDefault: MainSwitch = MainSwitchModes.AllowAll; break;
                case MainSwitchModes.AllowAll: MainSwitch = MainSwitchModes.Off; break;
            }
        }
        if (Enabled && CountDownHotkey.Pressed) {
            switch (CountdownMode) {
                case CountdownModes.Off: CountdownMode = CountdownModes._3fCycle; break;
                case CountdownModes._3fCycle: CountdownMode = CountdownModes._15fCycle; break;
                case CountdownModes._15fCycle: CountdownMode = CountdownModes.Off; break;
            }
        }
        if (Enabled && LoadRangeHotkey.Pressed) {
            switch (LoadRangeMode) {
                case LoadRangeModes.Neither: LoadRangeMode = LoadRangeModes.InViewRange; break;
                case LoadRangeModes.InViewRange: LoadRangeMode = LoadRangeModes.NearPlayerRange; break;
                case LoadRangeModes.NearPlayerRange: LoadRangeMode = LoadRangeModes.Both; break;
                case LoadRangeModes.Both: LoadRangeMode = LoadRangeModes.Neither; break;
            }
        }
        if (Enabled && PixelGridWidthHotkey.Pressed) {
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

