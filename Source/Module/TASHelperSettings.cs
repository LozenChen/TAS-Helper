using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using TAS.EverestInterop;
using YamlDotNet.Serialization;

namespace Celeste.Mod.TASHelper.Module;

[SettingName("TAS_HELPER_NAME")]
public class TASHelperSettings : EverestModuleSettings {

    public static TASHelperSettings Instance { get; private set; }

    public TASHelperSettings() {
        Instance = this;

        // LoadSettings will call ctor to set default values, which seem to go wrong for enum if we write these codes outside
        mainSwitch = MainSwitchModes.OnlyDefault;
        usingNotInViewColorMode = UsingNotInViewColorModes.WhenUsingInViewRange;
        countdownMode = CountdownModes.Off;
        CountdownFont = CountdownFonts.HiresFont;
        loadRangeMode = LoadRangeModes.Neither;
        enforceClearSprites = ClearSpritesMode.WhenSimplifyGraphics;
    }

    internal void OnLoadSettings() {
        UpdateAuxiliaryVariable();

        keyMainSwitch ??= new((Buttons)0, Keys.LeftControl, Keys.E);
        keyCountDown ??= new((Buttons)0, Keys.LeftControl, Keys.R);
        keyLoadRange ??= new((Buttons)0, Keys.LeftControl, Keys.T);
        keyPixelGridWidth ??= new((Buttons)0, Keys.LeftControl, Keys.F);
        keyPredictFuture ??= new((Buttons)0, Keys.LeftControl, Keys.W);
        // it seems some bug can happen with deserialization
    }

    public bool Enabled = true;

    #region MainSwitch

    // it will only affect main options, will not affect suboptions (which will not work if corresponding main option is not on)

    public enum MainSwitchModes { Off, OnlyDefault, AllowAll }

    public MainSwitchModes mainSwitch;

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

    public bool showCycleHitboxColor = true;

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

    public UsingNotInViewColorModes usingNotInViewColorMode;

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

    public CountdownModes countdownMode;

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

    public CountdownFonts CountdownFont;

    public int HiresFontSize = 8;

    public bool usingHiresFont => CountdownFont == CountdownFonts.HiresFont;

    public int HiresFontStroke = 5;

    public bool DoNotRenderWhenFarFromView = true;

    public bool CountdownBoost = false;

    #endregion

    #region LoadRange

    public bool Awake_LoadRange = true;
    public enum LoadRangeModes { Neither, InViewRange, NearPlayerRange, Both };

    public LoadRangeModes loadRangeMode;

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

    public int InViewRangeWidth = 16;

    public int NearPlayerRangeWidth = 8;

    public int loadRangeOpacity = 4;

    [YamlIgnore]
    public int LoadRangeOpacity {
        get => loadRangeOpacity;
        set {
            loadRangeOpacity = value;
            RangeAlpha = value * 0.1f;
        }
    }

    public bool ApplyCameraZoom = false;

    #endregion

    #region Simplified Spinner

    public bool Awake_EnableSimplifiedSpinner = true;

    public bool enableSimplifiedSpinner = true;

    [YamlIgnore]
    public bool EnableSimplifiedSpinner {
        get => Enabled && Awake_EnableSimplifiedSpinner && enableSimplifiedSpinner;
        set {
            enableSimplifiedSpinner = value;
            Awake_EnableSimplifiedSpinner = true;
        }
    }

    public enum ClearSpritesMode { Off, WhenSimplifyGraphics, Always };

    public ClearSpritesMode enforceClearSprites;

    [YamlIgnore]
    public ClearSpritesMode EnforceClearSprites {
        get => enforceClearSprites;
        set => enforceClearSprites = value;
    }

    public bool ClearSpinnerSprites => EnableSimplifiedSpinner && (EnforceClearSprites == ClearSpritesMode.Always || (EnforceClearSprites == ClearSpritesMode.WhenSimplifyGraphics && TasSettings.SimplifiedGraphics));

    public int spinnerFillerOpacity_Collidable = 8;

    [YamlIgnore]
    public int SpinnerFillerOpacity_Collidable {
        get => spinnerFillerOpacity_Collidable;
        set {
            spinnerFillerOpacity_Collidable = value;
            SpinnerFillerAlpha_Collidable = value * 0.1f;
        }
    }

    public int spinnerFillerOpacity_Uncollidable = 0;

    [YamlIgnore]
    public int SpinnerFillerOpacity_Uncollidable {
        get => spinnerFillerOpacity_Uncollidable;
        set {
            spinnerFillerOpacity_Uncollidable = value;
            SpinnerFillerAlpha_Uncollidable = value * 0.1f;
        }
    }

    public bool Ignore_TAS_UnCollidableAlpha = true;

    #endregion


    public bool Awake_EntityActivatorReminder = true;

    public bool entityActivatorReminder = true;

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
    public float SpinnerFillerAlpha_Collidable = 0.8f;
    public float SpinnerFillerAlpha_Uncollidable = 0f;
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

    public bool predictFuture = false;

    [YamlIgnore]
    public bool PredictFuture {
        get => Enabled && ModUtils.SpeedrunToolInstalled && predictFuture;
        set {
            predictFuture = value;
        }
    }

    public bool PredictOnFrameStep = true;

    public bool PredictOnFileChange = false;

    public bool PredictOnHotkeyPressed = true;

    public int FutureLength = 20;

    public bool Awake_CameraTarget = true;

    public bool usingCameraTarget = false;

    [YamlIgnore]
    public bool UsingCameraTarget {
        get => Enabled && Awake_CameraTarget && usingCameraTarget;
        set {
            usingCameraTarget = value;
            Awake_CameraTarget = true;
        }
    }

    public int CameraTargetLinkOpacity = 6;

    public bool Awake_PixelGrid = true;

    public bool enablePixelGrid = false;

    [YamlIgnore]
    public bool EnablePixelGrid {
        get => Enabled && Awake_PixelGrid && enablePixelGrid;
        set {
            enablePixelGrid = value;
            Awake_PixelGrid = true;
        }
    }

    public int PixelGridWidth = 2;
    public int PixelGridOpacity = 8;

    public bool Awake_SpawnPoint = true;

    public bool usingSpawnPoint = true;

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

    public bool usingFireBallTrack = true;

    [YamlIgnore]
    public bool UsingFireBallTrack {
        get => Enabled && Awake_FireBallTrack && usingFireBallTrack;
        set {
            usingFireBallTrack = value;
            Awake_FireBallTrack = true;
        }
    }

    public bool AllowEnableModWithMainSwitch = true;

    public bool mainSwitchStateVisualize = true;

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

    public bool mainSwitchThreeStates = true;

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

    [SettingName("TAS_HELPER_MAIN_SWITCH_HOTKEY")]
    [SettingSubHeader("TAS_HELPER_HOTKEY_DESCRIPTION")]
    [SettingDescriptionHardcoded]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.E)]
    public ButtonBinding keyMainSwitch { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.E);


    [SettingName("TAS_HELPER_SWITCH_COUNT_DOWN_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.R)]
    public ButtonBinding keyCountDown { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.R);


    [SettingName("TAS_HELPER_SWITCH_LOAD_RANGE_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.T)]
    public ButtonBinding keyLoadRange { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.T);


    [SettingName("TAS_HELPER_SWITCH_PIXEL_GRID_WIDTH_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.F)]
    public ButtonBinding keyPixelGridWidth { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.F);

    [SettingName("TAS_HELPER_PREDICT_FUTURE_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.W)]
    public ButtonBinding keyPredictFuture { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.W);


    // should not use a List<Hotkey> var, coz changing KeyPixelGridWidth will cause the hotkey get newed
    public bool SettingsHotkeysPressed() {
        if (Engine.Scene is not Level level) {
            return false;
        }

        bool updateKey = true;
        bool updateButton = true;
        bool InOuiModOption = TASHelperMenu.mainItem?.Container?.Focused is bool b && b;
        if (InOuiModOption || (level.Tracker.Entities.TryGetValue(typeof(KeyboardConfigUI), out var list) && list.Count > 0) ||
            (level.Tracker.Entities.TryGetValue(typeof(ModuleSettingsKeyboardConfigUIExt), out var list2) && list2.Count > 0)) {
            updateKey = false;
        }
        if (InOuiModOption || (level.Tracker.Entities.TryGetValue(typeof(ButtonConfigUI), out var list3) && list3.Count > 0)) {
            updateButton = false;
        }

        TH_Hotkeys.MainSwitchHotkey.Update(updateKey, updateButton);
        TH_Hotkeys.CountDownHotkey.Update(updateKey, updateButton);
        TH_Hotkeys.LoadRangeHotkey.Update(updateKey, updateButton);
        TH_Hotkeys.PixelGridWidthHotkey.Update(updateKey, updateButton);
        TH_Hotkeys.PredictFutureHotkey.Update(updateKey, updateButton);

        bool changed = false;

        if (TH_Hotkeys.MainSwitchHotkey.Pressed) {
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
        if (TH_Hotkeys.CountDownHotkey.Pressed) {
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
        if (TH_Hotkeys.LoadRangeHotkey.Pressed) {
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
        if (TH_Hotkeys.PixelGridWidthHotkey.Pressed) {
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
        if (TH_Hotkeys.PredictFutureHotkey.Pressed) {
            if (TasHelperSettings.PredictFuture && TasHelperSettings.PredictOnHotkeyPressed && FrameStep) {
                Predictor.Core.hasDelayedPredict = true;
            }
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

