using Celeste.Mod.TASHelper.ModInterop;
using Celeste.Mod.TASHelper.OrderOfOperation;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using TAS;
using TAS.Input;
using TAS.Input.Commands;

namespace Celeste.Mod.TASHelper.Predictor;
public static class PredictorCore {

    public static List<RenderData> futures = new();

    public static bool HasCachedFutures {
        get => CacheFutureCountdown > 0;
        set {
            if (value) {
                CacheFutureCountdown = CacheFuturePeriod;
            }
            else {
                CacheFutureCountdown = 0;
            }
        }
    }

    public static bool InPredict = false;

    public static readonly List<Func<bool>> SkipPredictChecks = new();

    public static readonly List<Func<RenderData, bool>> EarlyStopChecks = new();

    public static int CacheFuturePeriod { get; private set; } = 60;

    public static int CacheFutureCountdown { get; private set; } = 0;

    public static void Predict(int frames, bool mustRedo = true) {
        if (!mustRedo && HasCachedFutures) {
            return;
        }

        if (!TasHelperSettings.PredictFutureEnabled || InPredict || OoO_Core.Applied) {
            return;
        }

        TasHelperSettings.Enabled = false;
        // this stops most hooks from working (in particular, SpinnerCalculateHelper.PreSpinnerCalculate)
        SafePredict(frames);
        TasHelperSettings.Enabled = true;
    }

    private static void SafePredict(int frames) {
        if (SkipPredictCheck()) {
            return;
        }

        if (!TasSpeedrunToolInterop.SaveState()) {
            return;
        }

        // Celeste.Commands.Log($"An actual Prediction in frame: {Manager.Controller.CurrentFrameInTas}");

        InPredict = true;

        futures.Clear();

        ModifiedAutoMute.Apply();

        InputManager.ReadInputs(frames);

        PlayerState PreviousState;
        PlayerState CurrentState = PlayerState.GetState();

        for (int i = 0; i < frames; i++) {
            InputManager.ExecuteCommands(i); // commands are partially supported
            TAS.InputHelper.FeedInputs(InputManager.Inputs[i]);
            AlmostEngineUpdate(Engine.Instance, (GameTime)typeof(Game).GetFieldInfo("gameTime").GetValue(Engine.Instance));

            PreviousState = CurrentState;
            CurrentState = PlayerState.GetState();
            if (PreventSwitchScene()) {
                break;
            }
            RenderData data = new RenderData(i + 1, PreviousState, CurrentState);
            futures.Add(data);
            if (EarlyStopCheck(data)) {
                break;
            }
        }
        ModifiedAutoMute.OnPredictorUpdateEnd();

        ModifiedAutoMute.Undo();
        TasSpeedrunToolInterop.LoadState();

        HasCachedFutures = true;
        InPredict = false;
        CacheFutureCountdown = CacheFuturePeriod;

        // i'm not sure, but we can not move it to before LoadState i guess (maybe they're restored?)
        // otherwise hotkey predict will have bug that it does not render in first frame
        PredictorRenderer.ClearCachedMessage();
    }

    private static void AlmostEngineUpdate(Engine engine, GameTime gameTime) {
        Engine.RawDeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Engine.DeltaTime = Engine.RawDeltaTime * Engine.TimeRate * Engine.TimeRateB * Engine.GetTimeRateComponentMultiplier(engine.scene);
        Engine.FrameCounter++;
        FreezeTimerBeforeUpdate = Engine.FreezeTimer;

        if (Engine.DashAssistFreeze) {
            if (Input.Dash.Check || !Engine.DashAssistFreezePress) {
                if (Input.Dash.Check) {
                    Engine.DashAssistFreezePress = true;
                }
                if (engine.scene != null) {
                    engine.scene.Tracker.GetEntity<PlayerDashAssist>()?.Update();
                    if (engine.scene is Level) {
#pragma warning disable CS8602
                        (engine.scene as Level).UpdateTime();
#pragma warning restore CS8602
                    }
                    engine.scene.Entities.UpdateLists();
                }
            }
            else {
                Engine.DashAssistFreeze = false;
            }
        }
        if (!Engine.DashAssistFreeze) {
            if (Engine.FreezeTimer > 0f) {
                SJ_CassetteHookFreeze?.Invoke(null, parameterless);
                Engine.FreezeTimer = Math.Max(Engine.FreezeTimer - Engine.RawDeltaTime, 0f);
            }
            else if (engine.scene != null) {
                engine.scene.BeforeUpdate();
                engine.scene.Update();
                engine.scene.AfterUpdate();
            }
        }

        /* dont do this, leave it to PreventSwitchScene
        if (engine.scene != engine.nextScene) {
            Scene from = engine.scene;
            if (engine.scene != null) {
                engine.scene.End();
            }
            engine.scene = engine.nextScene;
            engine.OnSceneTransition(from, engine.nextScene);
            if (engine.scene != null) {
                engine.scene.Begin();
            }
        }
        */
        // base.Update(gameTime); i don't know how to call this correctly... bugs always occur
    }

    [Load]
    public static void Load() {
        // our hook should be inside the hook of TAS.Playback.Core
        using (new DetourContext { Before = new List<string> { "CelesteTAS" }, ID = "TAS Helper PredictorCore" }) {
            IL.Monocle.Engine.Update += ILEngineUpdate;
        }
    }

    [Unload]
    public static void Unload() {
        IL.Monocle.Engine.Update -= ILEngineUpdate;
    }

    private static void ILEngineUpdate(ILContext context) {
        ILCursor cursor = new ILCursor(context);
        if (cursor.TryGotoNext(MoveType.After, ins => ins.MatchCall(typeof(MInput), "Update"))) {
            cursor.EmitDelegate(AfterMInputUpdate);
        }
    }

    [Initialize]
    public static void Initialize() {
        typeof(Engine).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).HookAfter(() => {
            if (StrictFrameStep && TasHelperSettings.PredictOnFrameStep && Engine.Scene is Level) {
                Predict(TasHelperSettings.TimelineLength + CacheFuturePeriod, false);
            }
        });

        typeof(Level).GetMethod("BeforeRender").HookBefore(DelayedActions);
#pragma warning disable CS8601
        SJ_CassetteHookFreeze = ModUtils.GetType("StrawberryJam2021", "Celeste.Mod.StrawberryJam2021.Entities.WonkyCassetteBlockController")?.GetMethod("FreezeUpdate", BindingFlags.NonPublic | BindingFlags.Static);
#pragma warning restore CS8601

        InitializeChecks();
        InitializeCachePeriod();
    }

    private static MethodInfo SJ_CassetteHookFreeze;

    private static void AfterMInputUpdate() {
        PredictorRenderer.ClearCachedMessage();
        FreezeTimerBeforeUpdate = Engine.FreezeTimer;
        neverClearStateThisFrame = true;

        if (!Manager.Running) {
            HasCachedFutures = false;
            futures.Clear();
            TasSpeedrunToolInterop.ClearState();
            return;
        }

        CacheFutureCountdown--;
        if (!FutureMoveLeft()) {
            HasCachedFutures = false;
            futures.Clear();
            TasSpeedrunToolInterop.ClearState();
            return;
        }
        if (!HasCachedFutures) {
            TasSpeedrunToolInterop.ClearState();
        }
    }

    public static float FreezeTimerBeforeUpdate = 0f; // include those predicted frames

    public static bool ThisPredictedFrameFreezed => FreezeTimerBeforeUpdate > 0f;
    public static void InitializeChecks() {
        SkipPredictChecks.Clear();
        EarlyStopChecks.Clear();
        SkipPredictChecks.Add(SafeGuard);
        if (!TasHelperSettings.StartPredictWhenTransition) {
            SkipPredictChecks.Add(() => Engine.Scene is Level level && level.Transitioning);
        }
        if (TasHelperSettings.StopPredictWhenTransition) {
            EarlyStopChecks.Add(data => data.Keyframe.Has(KeyframeType.BeginTransition));
        }
        if (TasHelperSettings.StopPredictWhenDeath) {
            EarlyStopChecks.Add(data => data.Keyframe.Has(KeyframeType.GainDead));
        }
        if (TasHelperSettings.StopPredictWhenKeyframe) {
            EarlyStopChecks.Add(data => PredictorRenderer.KeyframeColorGetter(data.Keyframe, out _) is not null);
        }
    }

    public static void InitializeCachePeriod() {
        if (TasHelperSettings.TimelineLength > 500) {
            CacheFuturePeriod = 120;
        }
        else {
            CacheFuturePeriod = 60;
        }
        TasSpeedrunToolInterop.ClearState();
        HasCachedFutures = false;
        futures.Clear();
        PredictorRenderer.ClearCachedMessage();
    }

    public static bool FutureMoveLeft() {
        if (futures.Count <= 0) {
            return false;
        }
        futures.RemoveAt(0);
        futures = futures.Select(future => future with { index = future.index - 1 }).ToList();
        return true;
    }

    private static bool SafeGuard() {
        return Engine.Scene is not Level;
    }

    internal static bool delayedClearState = false;

    internal static bool delayedClearFutures = false;

    private static bool neverClearStateThisFrame = true;

    private static bool delayedMustRedo;

    private static bool hasDelayedPredict = false;
    public static void PredictLater(bool mustRedo) {
        hasDelayedPredict = true;
        delayedMustRedo = mustRedo;
    }

    [EnableRun]
    private static void OnTasRerun() {
        futures.Clear();
        PredictorRenderer.ClearCachedMessage();
        HasCachedFutures = false;
    }
    private static void DelayedActions() {
        DelayedClearFutures();
        DelayedClearState();
        DelayedPredict();
    }

    private static void DelayedClearFutures() {
        if (delayedClearFutures && !InPredict) {
            futures.Clear();
            PredictorRenderer.ClearCachedMessage();
            delayedClearFutures = false;
        }
    }
    private static void DelayedClearState() {
        if (delayedClearState && neverClearStateThisFrame && !InPredict) {
            neverClearStateThisFrame = false;
            delayedClearState = false;
            TasSpeedrunToolInterop.ClearState();
        }
    }
    private static void DelayedPredict() {
        if (hasDelayedPredict && !InPredict) {
            preventSendStateToStudio = true;
            RefreshInputs();
            GameInfo.Update();
            Predict(TasHelperSettings.TimelineLength + CacheFuturePeriod, delayedMustRedo);
            hasDelayedPredict = false;
            preventSendStateToStudio = false;
        }
        // we shouldn't do this in half of the render process

        void RefreshInputs() {

            InputController c = Manager.Controller;
            c.NeedsReload = true;

            int lastChecksum = c.Checksum;
            bool firstRun = c.UsedFiles.IsEmpty();

            c.Clear();
            if (c.ReadFile(c.FilePath)) {
                if (Manager.NextState == Manager.State.Disabled) {
                    // The TAS contains something invalid
                    c.Clear();
                    Manager.DisableRun();
                }
                else {
                    c.NeedsReload = false;
                    c.StartWatchers();
                    TAS.Utils.AttributeUtils.Invoke<ParseFileEndAttribute>();
                    // it's collected by TAS's AttributeUtils instead of ours, so we shouldn't use our own AttributeUtils

                    if (!firstRun && lastChecksum != c.Checksum) {
                        MetadataCommands.UpdateRecordCount(c);
                    }
                }
            }
            else {
                // Something failed while trying to parse
                c.Clear();
            }

            c.CurrentFrameInTas = Math.Min(c.Inputs.Count, c.CurrentFrameInTas);

        }
    }

    private static bool preventSendStateToStudio {
        get => Manager.PreventSendStudioState;
        set {
            Manager.PreventSendStudioState = value;
        }
    }


    public static bool SkipPredictCheck() {
        foreach (Func<bool> check in SkipPredictChecks) {
            if (check()) {
                return true;
            }
        }
        return false;
    }

    public static bool PreventSwitchScene() {
        if (Engine.Instance.scene != Engine.Instance.nextScene) {
            Engine.Instance.nextScene = Engine.Instance.scene;
            return true;
        }

        return false;
    }
    public static bool EarlyStopCheck(RenderData data) {
        foreach (Func<RenderData, bool> check in EarlyStopChecks) {
            if (check(data)) {
                return true;
            }
        }
        return false;
    }
}