using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using TAS;

namespace Celeste.Mod.TASHelper.Predictor;
public static class Core {

    public static readonly List<RenderData> futures = new();

    public static bool HasPredict = false;

    public static bool InPredict = false;

    public static readonly List<Func<bool>> SkipPredictChecks = new();

    public static readonly List<Func<PlayerState, bool>> EarlyStopChecks = new();

    public static void Predict(int frames) {
        if (!TasHelperSettings.PredictFutureEnabled || InPredict) {
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

        // warn: this overrides SpeedrunTool's (and thus TAS's) savestate
        if (!ModifiedSaveLoad.SaveState()) {
            return;
        }

        SaveForTAS();

        InPredict = true;

        if (HasPredict) {
            futures.Clear();
            HasPredict = false;
        }

        ModifiedAutoMute.StartMute();
        InputManager.ReadInputs(frames);

        PlayerState PreviousState;
        PlayerState CurrentState = PlayerState.GetState();

        for (int i = 0; i < frames; i++) {
            TAS.InputHelper.FeedInputs(InputManager.Inputs[i]);
            // commands are not supported

            AlmostEngineUpdate(Engine.Instance, (GameTime)typeof(Game).GetFieldInfo("gameTime").GetValue(Engine.Instance));

            PreviousState = CurrentState;
            CurrentState = PlayerState.GetState();
            if (PreventSwitchScene()) {
                break;
            }
            futures.Add(new RenderData(i + 1, PreviousState, CurrentState));
            if (EarlyStopCheck(CurrentState)) {
                break;
            }
        }

        ModifiedSaveLoad.LoadState();
        LoadForTAS();
        ModifiedAutoMute.EndMute();

        HasPredict = true;
        InPredict = false;
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
                        (engine.scene as Level).UpdateTime();
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

    public static void Initialize() {
        typeof(Engine).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).HookAfter(() => {
            if (StrictFrameStep && TasHelperSettings.PredictOnFrameStep && Engine.Scene is Level) {
                Predict(TasHelperSettings.TimelineLength);
            }
        });

        typeof(Engine).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).HookBefore(() => {
            FreezeTimerBeforeUpdate = Engine.FreezeTimer;
            if (!InPredict) {
                ModifiedSaveLoad.ClearState();
            }
        });

        typeof(Scene).GetMethod("BeforeUpdate").HookAfter(() => {
            if (!InPredict) {
                futures.Clear();
                HasPredict = false;
            }
        });

        typeof(Level).GetMethod("BeforeRender").HookBefore(DelayedPredict);

        HookHelper.SkipMethod(typeof(Core), nameof(InPredictMethod), typeof(GameInfo).GetMethod("Update", BindingFlags.Public | BindingFlags.Static));

        InitializeChecks();
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
            EarlyStopChecks.Add(_ => Engine.Scene is Level level && level.Transitioning);
        }
        if (TasHelperSettings.StopPredictWhenDeath) {
            EarlyStopChecks.Add(_ => _.Dead);
        }
    }

    private static bool SafeGuard() {
        return Engine.Scene is not Level;
    }

    private static void DelayedPredict() {
        if (hasDelayedPredict && !InPredict) {
            Manager.Controller.RefreshInputs(false);
            GameInfo.Update();
            Predict(TasHelperSettings.TimelineLength);
            hasDelayedPredict = false;
        }
        // we shouldn't do this in half of the render process
    }

    public static bool hasDelayedPredict = false;

    private static bool InPredictMethod() {
        return InPredict;
    }

    private static void SaveForTAS() {
        DashTime = GameInfo.DashTime;
        Frozen = GameInfo.Frozen;
        TransitionFrames = GameInfo.TransitionFrames;
        freezeTimerBeforeUpdateBeforePredictLoops = FreezeTimerBeforeUpdate;
    }

    private static float DashTime;
    private static bool Frozen;
    private static int TransitionFrames;
    private static float freezeTimerBeforeUpdateBeforePredictLoops;
    private static void LoadForTAS() {
        GameInfo.DashTime = DashTime;
        GameInfo.Frozen = Frozen;
        GameInfo.TransitionFrames = TransitionFrames;
        FreezeTimerBeforeUpdate = freezeTimerBeforeUpdateBeforePredictLoops;
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
    public static bool EarlyStopCheck(PlayerState state) {
        foreach (Func<PlayerState, bool> check in EarlyStopChecks) {
            if (check(state)) {
                return true;
            }
        }
        return false;
    }
}
