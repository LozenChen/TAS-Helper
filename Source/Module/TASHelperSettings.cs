using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Gameplay;
using Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;
using Celeste.Mod.TASHelper.Gameplay.Spinner;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.OrderOfOperation;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using YamlDotNet.Serialization;
using Mode = Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity.RenderMode;

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
        EnforceClearSprites = SimplifiedGraphicsMode.WhenSimplifyGraphics;
        EnableSimplifiedLightningMode = SimplifiedGraphicsMode.WhenSimplifyGraphics;
        EnableSimplifiedTriggersMode = SimplifiedGraphicsMode.Always;
        LoadRangeColliderMode = LoadRangeColliderModes.Auto;
        TimelineFinestScale = TimelineFinestStyle.PolygonLine;
        TimelineFineScale = TimelineScales._5;
        TimelineCoarseScale = TimelineScales.NotApplied;
        cassetteBlockInfoAlignment = CassetteBlockHelper.Alignments.TopRight;
    }

    internal void OnLoadSettings() {
        UpdateAuxiliaryVariable();

        SpeedrunTimerDisplayOpacityToFloat = SpeedrunTimerDisplayOpacity * 0.1f;

        // it seems some bug can happen with deserialization
        keyMainSwitch ??= new((Buttons)0, Keys.LeftControl, Keys.E);
        keyFrameStepBack = new((Buttons)0, Keys.LeftControl, Keys.I);
        keyCountDown ??= new((Buttons)0, Keys.LeftControl, Keys.R);
        keyLoadRange ??= new((Buttons)0, Keys.LeftControl, Keys.T);
        keyPixelGridWidth ??= new((Buttons)0, Keys.LeftControl, Keys.F);
        keyPredictEnable ??= new((Buttons)0, Keys.LeftControl, Keys.W);
        keyPredictFuture ??= new((Buttons)0, Keys.LeftControl, Keys.P);
        keyOoO_Step ??= new((Buttons)0, Keys.LeftControl, Keys.G);
        keyOoO_Fastforward ??= new((Buttons)0, Keys.LeftControl, Keys.Y);
        keyAutoWatch ??= new((Buttons)0, Keys.LeftControl, Keys.Q);
        AutoWatchInitialize();
    }

    public bool Enabled = true;

    public bool FirstInstall = true;

    // idk, it seems that Deserializer cant handle version well, so i use string instead
    // last version of which the update log has been read
    public string LastVersion = "0.0.1";

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
            HotkeyWatcher.RefreshMainSwitch();
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
        Awake_CountdownModes = false;
        Awake_LoadRange = false;
        Awake_CameraTarget = false;
        Awake_PixelGrid = false;
    }
    internal void Awake(bool awakeAll) {
        MainSwitch = awakeAll ? MainSwitchModes.AllowAll : MainSwitchModes.OnlyDefault;
        Awake_CountdownModes = awakeAll;
        Awake_LoadRange = awakeAll;
        Awake_CameraTarget = awakeAll;
        Awake_PixelGrid = awakeAll;

        // if you have an Awake_XX = true, drop it
        // coz now we won't make this settings show in mod settings if the mod is disabled
        // so there's no possibility that users see a wrong value
    }

    #endregion

    #region CycleHitboxColor

    // we need to make it public, so this setting is stored
    // though we don't want anyone to visit it directly...

    public bool showCycleHitboxColor = true;

    [YamlIgnore]
    public bool ShowCycleHitboxColors {
        get => Enabled && showCycleHitboxColor;
        set {
            showCycleHitboxColor = value;
        }
    }

    public enum UsingNotInViewColorModes { Off, WhenUsingInViewRange, Always };

    public UsingNotInViewColorModes usingNotInViewColorMode;

    [YamlIgnore]
    public UsingNotInViewColorModes UsingNotInViewColorMode {
        get => Enabled ? usingNotInViewColorMode : UsingNotInViewColorModes.Off;
        set {
            usingNotInViewColorMode = value;
            UsingNotInViewColor = (value == UsingNotInViewColorModes.Always) || (value == UsingNotInViewColorModes.WhenUsingInViewRange && UsingInViewRange);
        }
    }

    public bool UsingNotInViewColor = true;

    #endregion

    #region Countdown
    public bool Awake_CountdownModes = true;
    public enum CountdownModes { Off, _3fCycle, _15fCycle, ExactGroupMod3, ExactGroupMod15 };

    public CountdownModes countdownMode;

    public bool CountdownHotkeyCycleOrMod = false;

    [YamlIgnore]
    public CountdownModes CountdownMode {
        get => Enabled && Awake_CountdownModes ? countdownMode : CountdownModes.Off;
        set {
            countdownMode = value;
            Awake_CountdownModes = true;

            UsingCountDown = value is CountdownModes._3fCycle or CountdownModes._15fCycle;
            int modulo = value switch {
                CountdownModes.ExactGroupMod3 => 3,
                CountdownModes.ExactGroupMod15 => 15,
                _ => -1
            };
            if (UsingCountDown) {
                CountdownHotkeyCycleOrMod = false;
            }
            else if (modulo > 0) {
                CountdownHotkeyCycleOrMod = true;
            }
            ExactSpinnerGroup.SetModulo(modulo);
            CountdownRenderer.ClearCache();
            if (CountdownMode == CountdownModes._3fCycle) {
                SpinnerCountdownLoad = true;
            }
            else if (CountdownMode == CountdownModes._15fCycle) {
                SpinnerCountdownLoad = false;
            }
        }
    }

    public enum CountdownFonts { PixelFont, HiresFont };

    public CountdownFonts CountdownFont;

    public int HiresFontSize = 8;

    public bool UsingHiresFont => CountdownFont == CountdownFonts.HiresFont;

    public int HiresFontStroke = 5;

    [Obsolete]
    public bool DoNotRenderWhenFarFromView = true;

    public bool CountdownBoost = false;

    public bool DarkenWhenUncollidable = true;

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
            UsingLoadRangeCollider = LoadRangeColliderMode switch {
                LoadRangeColliderModes.Off => false,
                LoadRangeColliderModes.Auto => UsingLoadRange,
                LoadRangeColliderModes.Always => true,
                _ => true
            };
            LoadRangeColliderRenderer.ClearCache();
            CountdownRenderer.ClearCache(); // using load range affects count down position, so we need clear cache
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

    public enum LoadRangeColliderModes { Off, Auto, Always };

    public LoadRangeColliderModes loadRangeColliderMode = LoadRangeColliderModes.Auto;

    [YamlIgnore]

    public LoadRangeColliderModes LoadRangeColliderMode {
        get => loadRangeColliderMode;
        set {
            loadRangeColliderMode = value;
            UsingLoadRangeCollider = LoadRangeColliderMode switch {
                LoadRangeColliderModes.Off => false,
                LoadRangeColliderModes.Auto => UsingLoadRange,
                LoadRangeColliderModes.Always => true,
                _ => true
            };
            LoadRangeColliderRenderer.ClearCache();
        }
    }

    #endregion

    #region Simplified Graphics
    public bool enableSimplifiedSpinner = true;

    [YamlIgnore]
    public bool EnableSimplifiedSpinner {
        get => Enabled && enableSimplifiedSpinner;
        set {
            enableSimplifiedSpinner = value;
        }
    }

    public enum SimplifiedGraphicsMode { Off, WhenSimplifyGraphics, Always };

    public static bool SGModeToBool(SimplifiedGraphicsMode mode) {
        return mode == SimplifiedGraphicsMode.Always || (mode == SimplifiedGraphicsMode.WhenSimplifyGraphics && TasSettings.SimplifiedGraphics);
    }

    public SimplifiedGraphicsMode EnforceClearSprites;

    public bool ClearSpinnerSprites => EnableSimplifiedSpinner && SGModeToBool(EnforceClearSprites);

    public int spinnerFillerOpacity_Collidable = 8;

    [YamlIgnore]
    public int SpinnerFillerOpacity_Collidable {
        get => spinnerFillerOpacity_Collidable;
        set {
            spinnerFillerOpacity_Collidable = value;
            SpinnerFillerAlpha_Collidable = value * 0.1f;
        }
    }

    public int spinnerFillerOpacity_Uncollidable = 2;

    [YamlIgnore]
    public int SpinnerFillerOpacity_Uncollidable {
        get => spinnerFillerOpacity_Uncollidable;
        set {
            spinnerFillerOpacity_Uncollidable = value;
            SpinnerFillerAlpha_Uncollidable = value * 0.1f;
        }
    }

    public bool Ignore_TAS_UnCollidableAlpha = true;

    public bool SimplifiedSpinnerDashedBorder = true;

    public SimplifiedGraphicsMode EnableSimplifiedLightningMode;

    public bool EnableSimplifiedLightning => Enabled && SGModeToBool(EnableSimplifiedLightningMode); // both inner and outline

    public bool HighlightLoadUnload = false;

    public bool ApplyActualCollideHitboxForSpinner = true;

    public bool ApplyActualCollideHitboxForLightning = true;

    public SimplifiedGraphicsMode EnableSimplifiedTriggersMode;

    public bool EnableSimplifiedTriggers => Enabled && SGModeToBool(EnableSimplifiedTriggersMode);

    public bool HideCameraTriggers = false;

    public bool HideGoldBerryCollectTrigger = false;

    public bool enableCameraTriggerColor = true;


    [YamlIgnore]
    public bool EnableCameraTriggerColor {
        get => Enabled && enableCameraTriggerColor;
        set {
            enableCameraTriggerColor = value;
        }
    }

    public Color CameraTriggerColor = CustomColors.defaultCameraTriggerColor;

    #endregion

    #region Auxilary Variables
    public void UpdateAuxiliaryVariable() {
        // update the variables associated to variables govern by spinner main switch
        // it can happen their value is changed but not via the setter (i.e. change the Awake_...s)

        UsingNotInViewColor = (UsingNotInViewColorMode == UsingNotInViewColorModes.Always) || (UsingNotInViewColorMode == UsingNotInViewColorModes.WhenUsingInViewRange && UsingInViewRange);
        UsingCountDown = CountdownMode is CountdownModes._3fCycle or CountdownModes._15fCycle;
        int modulo = CountdownMode switch {
            CountdownModes.ExactGroupMod3 => 3,
            CountdownModes.ExactGroupMod15 => 15,
            _ => -1
        };
        ExactSpinnerGroup.SetModulo(modulo);
        if (UsingCountDown) {
            CountdownHotkeyCycleOrMod = false;
        }
        else if (modulo > 0) {
            CountdownHotkeyCycleOrMod = true;
        }
        if (CountdownMode == CountdownModes._3fCycle) {
            SpinnerCountdownLoad = true;
        }
        else if (CountdownMode == CountdownModes._15fCycle) {
            SpinnerCountdownLoad = false;
        }
        UsingLoadRange = (LoadRangeMode != LoadRangeModes.Neither);
        UsingInViewRange = (LoadRangeMode == LoadRangeModes.InViewRange || LoadRangeMode == LoadRangeModes.Both);
        UsingNearPlayerRange = (LoadRangeMode == LoadRangeModes.NearPlayerRange || LoadRangeMode == LoadRangeModes.Both);

        UsingLoadRangeCollider = LoadRangeColliderMode switch {
            LoadRangeColliderModes.Off => false,
            LoadRangeColliderModes.Auto => UsingLoadRange,
            LoadRangeColliderModes.Always => true,
            _ => true
        };
        LoadRangeColliderRenderer.ClearCache();
        CountdownRenderer.ClearCache();
        MovementOvershootAssistant.UpdateDetourState();
        CassetteBlockHelper.OnEnabledChanged();
        Gameplay.AutoWatchEntity.CoreLogic.OnConfigChange();
    }
    public bool UsingCountDown = false;
    public bool UsingLoadRange = true;
    public bool UsingInViewRange = true;
    public bool UsingNearPlayerRange = true;
    public bool SpinnerCountdownLoad = true;
    public bool UsingLoadRangeCollider = true;
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
    public Color MOAColor = CustomColors.defaultMOAColor;

    #endregion

    #region Predictor

    public bool predictFutureEnabled = false;

    [YamlIgnore]
    public bool PredictFutureEnabled {
        get => Enabled && ModUtils.SpeedrunToolInstalled && predictFutureEnabled;
        set {
            predictFutureEnabled = value;
        }
    }

    public bool DropPredictionWhenTasFileChange = true;

    public bool PredictOnFrameStep = true;

    public bool PredictOnFileChange = false;

    public bool PredictOnHotkeyPressed = true;

    public int TimelineLength = 100;

    public int UltraSpeedLowerLimit = 170;

    public TimelineFinestStyle TimelineFinestScale;

    public TimelineScales TimelineFineScale;

    public TimelineScales TimelineCoarseScale;

    public int PredictorPointSize = 8;

    public int PredictorLineWidth = 3;

    public enum TimelineFinestStyle { NotApplied, HitboxPerFrame, PolygonLine, DottedPolygonLine };

    public enum TimelineScales { NotApplied, _2, _5, _10, _15, _20, _25, _30, _45, _60, _100 }

    public static int ToInt(TimelineScales scale) {
        return scale switch {
            TimelineScales.NotApplied => -1,
            TimelineScales._2 => 2,
            TimelineScales._5 => 5,
            TimelineScales._10 => 10,
            TimelineScales._15 => 15,
            TimelineScales._20 => 20,
            TimelineScales._25 => 25,
            TimelineScales._30 => 30,
            TimelineScales._45 => 45,
            TimelineScales._60 => 60,
            TimelineScales._100 => 100,
            _ => -1
        };
    }

    public bool TimelineFadeOut = true;

    public bool StartPredictWhenTransition = true;

    public bool StopPredictWhenTransition = true;

    public bool StopPredictWhenDeath = true;

    public bool StopPredictWhenKeyframe = false;

    public bool UseKeyFrame = true;

    public bool UseKeyFrameTime = true;

    public bool UseFlagDead = true;

    public bool UseFlagGainCrouched = false;

    public bool UseFlagLoseCrouched = false;

    public bool UseFlagGainOnGround = true;

    public bool UseFlagLoseOnGround = false;

    public bool UseFlagGainPlayerControl = true;

    public bool UseFlagLosePlayerControl = true;

    public bool UseFlagOnEntityState = true;

    public bool UseFlagRefillDash = false;

    public bool UseFlagGainUltra = true;

    public bool UseFlagOnBounce = true;

    public bool UseFlagCanDashInStLaunch = true;

    public bool UseFlagGainLevelControl = true;

    public bool UseFlagLoseLevelControl = true;

    public bool UseFlagRespawnPointChange = false;

    public bool UseFlagGainFreeze = false;

    public bool UseFlagLoseFreeze = false;

    public bool UseFlagGetRetained = false;

    public Color PredictorEndpointColor = CustomColors.defaultPredictorEndpointColor;

    public Color PredictorFinestScaleColor = CustomColors.defaultPredictorFinestScaleColor;

    public Color PredictorFineScaleColor = CustomColors.defaultPredictorFineScaleColor;

    public Color PredictorCoarseScaleColor = CustomColors.defaultPredictorCoarseScaleColor;

    public Color PredictorKeyframeColor = CustomColors.defaultPredictorKeyframeColor;

    public Color PredictorPolygonalLineColor = CustomColors.defaultPredictorPolygonalLineColor;

    public Color PredictorDotColor = CustomColors.defaultPredictorDotColor;

    #endregion

    #region AutoWatchEntity

    public bool autoWatchEnable = false;

    [YamlIgnore]
    public bool AutoWatchEnable {
        get => Enabled && autoWatchEnable;
        set {
            autoWatchEnable = value;
        }
    }

    public int autoWatch_FontSize = 8;

    [YamlIgnore]
    public int AutoWatch_FontSize {
        get => autoWatch_FontSize;
        set {
            autoWatch_FontSize = value;
            Config.HiresFontSize = new Vector2(autoWatch_FontSize / 10f);
        }
    }

    public int autoWatch_FontStroke = 5;

    [YamlIgnore]
    public int AutoWatch_FontStroke {
        get => autoWatch_FontStroke;
        set {
            autoWatch_FontStroke = value;
            Config.HiresFontStroke = value * 0.4f;
        }
    }

    public bool autoWatch_Speed_PixelPerSecond = true;

    [YamlIgnore]
    public bool AutoWatch_Speed_PixelPerSecond {
        get => autoWatch_Speed_PixelPerSecond;
        set {
            autoWatch_Speed_PixelPerSecond = value;
            Format.Speed_UsePixelPerSecond = value;
        }
    }

    private void AutoWatchInitialize() {
        AutoWatch_FontSize = autoWatch_FontSize;
        AutoWatch_FontStroke = autoWatch_FontStroke;
        AutoWatch_Speed_PixelPerSecond = autoWatch_Speed_PixelPerSecond;
    }

    public Mode AutoWatch_BadelineOrb = Mode.Always;

    public Mode AutoWatch_Booster = Mode.Always;

    public Mode AutoWatch_Bumper = Mode.Always;

    public ShakeRenderMode AutoWatch_Bumper_NoneOrVelocityOrOffset = ShakeRenderMode.Offset;

    public Mode AutoWatch_Cloud = Mode.Always;

    public Mode AutoWatch_FallingBlock = Mode.Always;

    public Mode AutoWatch_FlingBird = Mode.Always;

    public Mode AutoWatch_Jelly = Mode.Always;

    public Mode AutoWatch_Kevin = Mode.Always;

    public Mode AutoWatch_MoonBlock = Mode.WhenWatched;

    public ShakeRenderMode AutoWatch_MoonBlock_VelocityOrOffset = ShakeRenderMode.Offset;

    public Mode AutoWatch_MoveBlock = Mode.Always;

    public Mode AutoWatch_Puffer = Mode.Always;

    public ShakeRenderMode AutoWatch_Puffer_NoneOrVelocityOrOffset = ShakeRenderMode.Offset;

    public Mode AutoWatch_Refill = Mode.Always;

    public Mode AutoWatch_Seeker = Mode.WhenWatched;

    public Mode AutoWatch_SwapBlock = Mode.Always;

    public Mode AutoWatch_TheoCrystal = Mode.Always;

    public Mode AutoWatch_Trigger = Mode.Never;

    public Mode AutoWatch_ZipMover = Mode.Always;

    public Mode AutoWatch_Cutscene = Mode.Always;

    public Mode AutoWatch_Player = Mode.Always;

    public bool AutoWatch_ShowDashAttackTimer = false;

    public bool AutoWatch_ShowDashTimer = false;

    public bool AutoWatch_ShowDreamDashCanEndTimer = true;

    public bool AutoWatch_ShowPlayerGliderBoostTimer = true;

    public bool AutoWatch_ShowStLaunchSpeed = true;

    public bool AutoWatch_ShowWallBoostTimer = true;

    #endregion

    #region Other

    public bool enableCassetteBlockHelper = true;

    [YamlIgnore]
    public bool EnableCassetteBlockHelper {
        get => Enabled && enableCassetteBlockHelper;
        set {
            enableCassetteBlockHelper = value;
            CassetteBlockHelper.OnEnabledChanged();
        }
    }

    public bool CassetteBlockHelperShowExtraInfo = false;

    public CassetteBlockHelper.Alignments cassetteBlockInfoAlignment;

    [YamlIgnore]
    public CassetteBlockHelper.Alignments CassetteBlockInfoAlignment {
        get => cassetteBlockInfoAlignment;
        set {
            cassetteBlockInfoAlignment = value;
            CassetteBlockHelper.CassetteBlockVisualizer.needReAlignment = true;
        }
    }

    public bool entityActivatorReminder = false;

    [YamlIgnore]
    public bool EntityActivatorReminder {
        get => Enabled && entityActivatorReminder;
        set {
            entityActivatorReminder = value;
        }
    }

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

    public bool usingSpawnPoint = true;

    [YamlIgnore]
    public bool UsingSpawnPoint {
        get => Enabled && usingSpawnPoint;
        set {
            usingSpawnPoint = value;
        }
    }

    public int CurrentSpawnPointOpacity = 5;

    public int OtherSpawnPointOpacity = 2;

    public bool usingFireBallTrack = true;

    [YamlIgnore]
    public bool UsingFireBallTrack {
        get => Enabled && usingFireBallTrack;
        set {
            usingFireBallTrack = value;
        }
    }

    public bool usingTrackSpinnerTrack = false;

    [YamlIgnore]
    public bool UsingTrackSpinnerTrack {
        get => Enabled && usingTrackSpinnerTrack;
        set {
            usingTrackSpinnerTrack = value;
        }
    }

    public bool usingRotateSpinnerTrack = false;

    [YamlIgnore]
    public bool UsingRotateSpinnerTrack {
        get => Enabled && usingRotateSpinnerTrack;
        set {
            usingRotateSpinnerTrack = value;
        }
    }

    public bool AllowEnableModWithMainSwitch = true;

    public bool hotKeyStateVisualize = true;

    [YamlIgnore]
    public bool HotkeyStateVisualize {
        get => hotKeyStateVisualize;
        set {
            hotKeyStateVisualize = value;
            if (HotkeyWatcher.Instance is HotkeyWatcher watcher) {
                watcher.Visible = hotKeyStateVisualize;
            }
        }
    }

    public bool mainSwitchThreeStates = true;

    [YamlIgnore]
    public bool MainSwitchThreeStates {
        get => mainSwitchThreeStates;
        set {
            mainSwitchThreeStates = value;
            HotkeyWatcher.RefreshMainSwitch();
        }
    }

    public bool enableOoO = false;

    [YamlIgnore]

    public bool EnableOoO {
        get => Enabled && enableOoO;
        set {
            enableOoO = value;
        }
    }

    public bool enableOpenConsoleInTas { get; set; } = true;

    [YamlIgnore]
    public bool EnableOpenConsoleInTas {
        get => Enabled && enableOpenConsoleInTas;
        set {
            enableOpenConsoleInTas = value;
        }
    }

    public bool enableScrollableHistoryLog { get; set; } = true;

    [YamlIgnore]
    public bool EnableScrollableHistoryLog {
        get => Enabled && enableScrollableHistoryLog;
        set {
            enableScrollableHistoryLog = value;
        }
    }

    public bool betterInvincible = true;

    [YamlIgnore]

    public bool BetterInvincible {
        get => Enabled && betterInvincible;
        set {
            betterInvincible = value;
        }
    }

    public bool showWindSpeed = false;

    [YamlIgnore]

    public bool ShowWindSpeed {
        get => Enabled && showWindSpeed;
        set {
            showWindSpeed = value;
        }
    }

    public bool SubscribeWhatsNew = true;

    public bool enableMovementOvershootAssistant = true;

    [YamlIgnore]
    public bool EnableMovementOvershootAssistant {
        get => Enabled && enableMovementOvershootAssistant;
        set {
            enableMovementOvershootAssistant = value;
            MovementOvershootAssistant.UpdateDetourState();
        }
    }

    public bool moaAbovePlayer = false;

    [YamlIgnore]
    public bool MOAAbovePlayer {
        get => moaAbovePlayer;
        set {
            moaAbovePlayer = value;
            if (MovementOvershootAssistant.MOA_Renderer.Instance is { } renderer) {
                renderer.Depth = value ? -1 : 1;
            }
        }
    }

    public int speedrunTimerDisplayOpacity = 5;

    [YamlIgnore]
    public int SpeedrunTimerDisplayOpacity {
        get => speedrunTimerDisplayOpacity;
        set {
            speedrunTimerDisplayOpacity = value;
            SpeedrunTimerDisplayOpacityToFloat = value * 0.1f;
        }
    }

    [YamlIgnore]

    public float SpeedrunTimerDisplayOpacityToFloat = 1f;

    #endregion

    #region HotKey

    [SettingName("TAS_HELPER_MAIN_SWITCH_HOTKEY")]
    [SettingSubHeader("TAS_HELPER_HOTKEY_DESCRIPTION")]
    [SettingDescriptionHardcoded]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.E })]
    public ButtonBinding keyMainSwitch { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.E);

    [SettingName("TAS_HELPER_FRAME_STEP_BACK")]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.I })]
    public ButtonBinding keyFrameStepBack { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.I);

    [SettingName("TAS_HELPER_SWITCH_COUNT_DOWN_HOTKEY")]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.R })]
    public ButtonBinding keyCountDown { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.R);


    [SettingName("TAS_HELPER_SWITCH_LOAD_RANGE_HOTKEY")]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.T })]
    public ButtonBinding keyLoadRange { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.T);


    [SettingName("TAS_HELPER_SWITCH_PIXEL_GRID_WIDTH_HOTKEY")]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.F })]
    public ButtonBinding keyPixelGridWidth { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.F);

    [SettingName("TAS_HELPER_PREDICT_ENABLE_HOTKEY")]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.W })]
    public ButtonBinding keyPredictEnable { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.W);

    [SettingName("TAS_HELPER_PREDICT_FUTURE_HOTKEY")]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.P })]
    public ButtonBinding keyPredictFuture { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.P);

    [SettingName("TAS_HELPER_OOO_STEP_HOTKEY")]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.G })]
    public ButtonBinding keyOoO_Step { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.G);

    [SettingName("TAS_HELPER_OOO_FASTFORWARD_HOTKEY")]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.Y })]
    public ButtonBinding keyOoO_Fastforward { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.Y);

    [SettingName("TAS_HELPER_AUTOWATCH_HOTKEY")]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.Q })]
    public ButtonBinding keyAutoWatch { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.Q);


    // should not use a List<Hotkey> var, coz changing KeyPixelGridWidth will cause the hotkey get newed
    public bool SettingsHotkeysPressed() {
        if (Engine.Scene is not Level level) {
            return false;
        }

        bool updateKey = true;
        bool updateButton = true;
        bool InOuiModOption = TASHelperMenu.mainItem?.Container is { } container && container.Visible;
        if (InOuiModOption || (level.Tracker.Entities.TryGetValue(typeof(KeyboardConfigUI), out var list) && list.Count > 0) ||
            (level.Tracker.Entities.TryGetValue(typeof(ModuleSettingsKeyboardConfigUIExt), out var list2) && list2.Count > 0)) {
            updateKey = false;
        }
        if (InOuiModOption || (level.Tracker.Entities.TryGetValue(typeof(ButtonConfigUI), out var list3) && list3.Count > 0)) {
            updateButton = false;
        }

        TH_Hotkeys.Update(updateKey, updateButton);

        OoO_Core.OnHotkeysPressed();
        if (OoO_Core.Applied) {
            return false;
        }

        bool changed = false; // if settings need to be saved

        if (TH_Hotkeys.MainSwitchHotkey.Pressed) {
            changed = true;
            switch (MainSwitch) {
                case MainSwitchModes.Off: {
                        if (!AllowEnableModWithMainSwitch) {
                            changed = false;
                            Refresh("Enabling TAS Helper with Hotkey is disabled!");
                            break;
                        }
                        MainSwitch = MainSwitchThreeStates ? MainSwitchModes.OnlyDefault : MainSwitchModes.AllowAll;
                        break;
                    }
                // it may happen that MainSwitchThreeStates = false but MainSwitch = OnlyDefault... it's ok
                case MainSwitchModes.OnlyDefault: MainSwitch = MainSwitchModes.AllowAll; break;
                case MainSwitchModes.AllowAll: MainSwitch = MainSwitchModes.Off; break;
                    // other HotkeyWatcher refresh are left to the setter of mainSwitch
            }
        }
        else if (TH_Hotkeys.CountDownHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                switch (CountdownMode) {
                    case CountdownModes.Off: {
                            if (CountdownHotkeyCycleOrMod) {
                                CountdownMode = CountdownModes.ExactGroupMod3; Refresh("Hazard Countdown Mode = ExactGroup % 3"); break;
                            }
                            else {
                                CountdownMode = CountdownModes._3fCycle; Refresh("Hazard Countdown Mode = 3f Cycle"); break;
                            }
                        }
                    case CountdownModes._3fCycle: CountdownMode = CountdownModes._15fCycle; Refresh("Hazard Countdown Mode = 15f Cycle"); break;
                    case CountdownModes._15fCycle: CountdownMode = CountdownModes.Off; Refresh("Hazard Countdown Mode = Off"); break;
                    case CountdownModes.ExactGroupMod3: CountdownMode = CountdownModes.ExactGroupMod15; Refresh("Hazard Countdown Mode = ExactGroup % 15"); break;
                    case CountdownModes.ExactGroupMod15: CountdownMode = CountdownModes.Off; Refresh("Hazard Countdown Mode = Off"); break;
                }

            }
            else {
                HotkeyWatcher.RefreshHotkeyDisabled();
            }
        }
        else if (TH_Hotkeys.LoadRangeHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                switch (LoadRangeMode) {
                    case LoadRangeModes.Neither: LoadRangeMode = LoadRangeModes.InViewRange; Refresh("Load Range Mode = InView"); break;
                    case LoadRangeModes.InViewRange: LoadRangeMode = LoadRangeModes.NearPlayerRange; Refresh("Load Range Mode = NearPlayer"); break;
                    case LoadRangeModes.NearPlayerRange: LoadRangeMode = LoadRangeModes.Both; Refresh("Load Range Mode = Both"); break;
                    case LoadRangeModes.Both: LoadRangeMode = LoadRangeModes.Neither; Refresh("Load Range Mode = Neither"); break;
                }
            }
            else {
                HotkeyWatcher.RefreshHotkeyDisabled();
            }
        }
        else if (TH_Hotkeys.PixelGridWidthHotkey.Pressed) {
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
                string str = !EnablePixelGrid || DebugRendered ? "" : ", but DebugRender is not turned on";
                Refresh($"Pixel Grid Width = {PixelGridWidth}{str}");
            }
            else {
                HotkeyWatcher.RefreshHotkeyDisabled();
            }
        }
        else if (TH_Hotkeys.AutoWatchHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                AutoWatchEnable = !AutoWatchEnable;
                Gameplay.AutoWatchEntity.CoreLogic.OnConfigChange();
                Refresh($"Auto Watch Entity = {(AutoWatchEnable ? "ON" : "OFF")}");
            }
            else {
                HotkeyWatcher.RefreshHotkeyDisabled();
            }
        }
        else if (TH_Hotkeys.PredictEnableHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                predictFutureEnabled = !predictFutureEnabled;
                Refresh("Predictor " + (predictFutureEnabled ? "Enabled" : "Disabled"));
            }
            else {
                HotkeyWatcher.RefreshHotkeyDisabled();
            }
        }
        else if (TH_Hotkeys.PredictFutureHotkey.Pressed) {
            if (!Enabled) {
                HotkeyWatcher.RefreshHotkeyDisabled();
            }
            else if (!TasHelperSettings.PredictFutureEnabled) {
                Refresh("Predictor NOT enabled");
            }
            else if (!TasHelperSettings.PredictOnHotkeyPressed) {
                Refresh("Make-a-Prediction hotkey NOT enabled");
            }
            else if (!FrameStep) {
                Refresh("Not frame-stepping, refuse to predict");
            }
            else {
                Predictor.PredictorCore.PredictLater(false);
                Refresh("Predictor Start");
            }
        }
        else if (EnableOpenConsoleInTas && TH_Hotkeys.OpenConsole.Pressed) {
            Gameplay.ConsoleEnhancement.SetOpenConsole();
            // it's completely ok that this feature is not enabled and people press this key, so there's no warning
        }
        else if (!OoO_Core.Applied && (TH_Hotkeys.FrameStepBack.Released || TH_Hotkeys.FrameStepBack.Check && Gameplay.FrameStepBack.CheckOnHotkeyHold())) { // we use release so there's no save/load issue
            FrameStepBack.StepBackOneFrame();
        }

        return changed;

        void Refresh(string text) {
            HotkeyWatcher.Refresh(text);
        }
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

