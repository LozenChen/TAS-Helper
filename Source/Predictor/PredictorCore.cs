using Celeste.Mod.TASHelper.ModInterop;
using Celeste.Mod.TASHelper.OrderOfOperation;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System.Reflection;
using System.Reflection.Emit;
using TAS;
using TAS.Input;
using TAS.Input.Commands;

namespace Celeste.Mod.TASHelper.Predictor;
public static class PredictorCore {

    public static List<RenderData> futures = new();

    // RenderData.index is the absolute index at prediction time (1..N). FuturesStart is how many entries
    // have already been consumed by FutureMoveLeft; the renderer displays (index - FuturesStart) and we trim
    // the consumed prefix only occasionally, so advancing a frame is O(1) instead of rebuilding the list.
    internal static int FuturesStart = 0;

    private static void ClearFutures() {
        futures.Clear();
        FuturesStart = 0;
    }

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

        if (!TasHelperSettings.PredictFutureEnabled || InPredict || OoO_Controller.Active) {
            return;
        }

        TasHelperSettings.Enabled = false;
        // this stops most hooks from working (in particular, SpinnerCalculateHelper.PreSpinnerCalculate)
        try {
            SafePredict(frames);
        }
        catch (System.Exception e) {
            Logger.Log(LogLevel.Error, "TAS Helper", "Predictor failed: " + e);
        }
        finally {
            // never leave the whole mod disabled if the prediction throws
            TasHelperSettings.Enabled = true;
        }
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

        ClearFutures();

        ModifiedAutoMute.Apply();

        InputManager.ReadInputs(frames);

        // The gameTime does not change during a prediction run (we do not tick FNA), so read it once
        // instead of doing a reflective GetValue for every predicted frame.
        GameTime predictedGameTime = (GameTime)GameTimeField.GetValue(Engine.Instance);

        PlayerState previousState = PlayerState.GetState();
        PlayerState currentBuffer = new();
        for (int i = 0; i < frames; i++) {
            InputManager.ExecuteCommands(i); // commands are partially supported
            TAS.InputHelper.FeedInputs(InputManager.Inputs[i]);
            AlmostEngineUpdate(Engine.Instance, predictedGameTime);

            PlayerState currentState = PlayerState.Fill(currentBuffer);
            if (PreventSwitchScene()) {
                break;
            }
            RenderData data = new(i + 1, previousState, currentState);
            futures.Add(data);
            (previousState, currentBuffer) = (currentState, previousState);
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
        // same tail as Engine.Update: FNA's base update (iterates IUpdateable game components)
        try {
            GameBaseUpdate?.Invoke(engine, gameTime);
        }
        catch (System.Exception e) {
            if (!baseUpdateFailed) {
                baseUpdateFailed = true;
                Logger.Log(LogLevel.Warn, "TAS Helper", "Predictor: Game.Update(base) call failed, prediction may differ: " + e);
            }
        }
    }

    [Load]
    public static void Load() {
        // our hook should be inside the hook of TAS.Playback.Core
        if (!TasSpeedrunToolInterop.GetInstalledWhenLoading) {
            return;
        }

        // our injected code must end up INSIDE TAS.Playback.Core's hook (TAS outer):
        // "Before" = our manipulator applies first, so TAS's manipulator (applied later) wraps us.
        using (DetourContextHelper.ILHook("TAS Helper PredictorCore").AppliedBefore(DetourContextHelper.CelesteTAS).Use()) {
            IL.Monocle.Engine.Update += HookHelper.Checked(ILEngineUpdate);
        }
    }

    [Unload]
    public static void Unload() {
        if (!TasSpeedrunToolInterop.GetInstalledWhenLoading) {
            return;
        }

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
        if (!TasSpeedrunToolInterop.Installed) {
            return;
        }

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

    private static readonly FieldInfo GameTimeField =
        typeof(Microsoft.Xna.Framework.Game).GetField("gameTime", BindingFlags.Instance | BindingFlags.NonPublic);

    // Engine.Update ends with "call Microsoft.Xna.Framework.Game::Update" (base.Update). We cannot call
    // the protected base method directly, so emit a tiny dynamic method for it; this keeps prediction
    // faithful when another mod registers IUpdateable components on the Game (normally the list is empty).
    private static bool baseUpdateFailed;
    private static readonly System.Action<Engine, GameTime> GameBaseUpdate = CreateGameBaseUpdate();

    private static System.Action<Engine, GameTime> CreateGameBaseUpdate() {
        try {
            MethodInfo baseUpdate = typeof(Microsoft.Xna.Framework.Game).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            DynamicMethod dynamicMethod = new("TASHelper_GameBaseUpdate", typeof(void), new[] { typeof(Engine), typeof(GameTime) }, typeof(Engine).Module, skipVisibility: true);
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, baseUpdate);
            il.Emit(OpCodes.Ret);
            return (System.Action<Engine, GameTime>)dynamicMethod.CreateDelegate(typeof(System.Action<Engine, GameTime>));
        }
        catch (System.Exception e) {
            Logger.Log(LogLevel.Warn, "TAS Helper", "Predictor: failed to create Game.Update(base) delegate: " + e.Message);
            return null;
        }
    }

    private static void AfterMInputUpdate() {
        // Do not clear the render cache here unconditionally: TAS can render several frames for one
        // physics tick, and only a change of the futures list requires a rebuild. Clearing happens in
        // FutureMoveLeft() / SafePredict() / the explicit clear paths below.
        FreezeTimerBeforeUpdate = Engine.FreezeTimer;
        neverClearStateThisFrame = true;

        if (!Manager_Running) {
            HasCachedFutures = false;
            if (futures.Count > 0) {
                ClearFutures();
                PredictorRenderer.ClearCachedMessage();
            }
            TasSpeedrunToolInterop.ClearState();
            return;
        }

        CacheFutureCountdown--;
        if (!FutureMoveLeft()) {
            HasCachedFutures = false;
            if (futures.Count > 0) {
                ClearFutures();
                PredictorRenderer.ClearCachedMessage();
            }
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
        ClearFutures();
        PredictorRenderer.ClearCachedMessage();
    }

    public static bool FutureMoveLeft() {
        if (futures.Count <= FuturesStart) {
            return false;
        }
        FuturesStart++;
        if (FuturesStart > 256 && FuturesStart * 2 > futures.Count) {
            // Reclaim the consumed prefix in one O(n) pass; amortized this is still O(1) per frame.
            futures.RemoveRange(0, FuturesStart);
            FuturesStart = 0;
        }
        PredictorRenderer.ClearCachedMessage();
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
        ClearFutures();
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
            ClearFutures();
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