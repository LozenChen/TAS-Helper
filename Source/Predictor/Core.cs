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
    public static void Predict(int frames) {
        if (!TasHelperSettings.PredictFuture || InPredict) {
            return;
        }

        TasHelperSettings.Enabled = false;
        // stop most hooks from working (in particular, SpinnerCalculateHelper.PreSpinnerCalculate)
        SafePredict(frames);
        TasHelperSettings.Enabled = true;

    }

    private static void SafePredict(int frames) {
        if (!StartPredictCheck()) {
            return;
        }

        //todo: this overrides TAS's savestate
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
                Predict(TasHelperSettings.FutureLength);
            }
        });


        typeof(Engine).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).HookBefore(() => {
            if (!InPredict) {
                ModifiedSaveLoad.ClearState();
            }
        });


        // todo: refresh predict on tas file change

        typeof(Scene).GetMethod("BeforeUpdate").HookAfter(() => {
            if (!InPredict) {
                futures.Clear();
                HasPredict = false;
            }
        });

        typeof(Level).GetMethod("BeforeRender").HookBefore(DelayedPredict);

        HookHelper.SkipMethod(typeof(Core), nameof(InPredictMethod), typeof(GameInfo).GetMethod("Update", BindingFlags.Public | BindingFlags.Static));
    }

    private static void DelayedPredict() {
        if (hasDelayedPredict && !InPredict) {
            Manager.Controller.RefreshInputs(false);
            GameInfo.Update();
            Predict(TasHelperSettings.FutureLength);
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
    }

    private static float DashTime;
    private static bool Frozen;
    private static int TransitionFrames;
    private static void LoadForTAS() {
        GameInfo.DashTime = DashTime;
        GameInfo.Frozen = Frozen;
        GameInfo.TransitionFrames = TransitionFrames;
    }

    public static bool StartPredictCheck() {
        if (Engine.Scene is Level level && level.Transitioning) {
            return false;
        }
        return true;
    }

    public static bool PreventSwitchScene() {
        if (Engine.Instance.scene != Engine.Instance.nextScene) {
            Engine.Instance.nextScene = Engine.Instance.scene;
            return true;
        }

        return false;
    }
    public static bool EarlyStopCheck(PlayerState state) {
        if (Engine.Scene is Level level && level.Transitioning) {
            return true;
        }
        return false;
    }
}

public class PlayerState {

    public bool HasPlayer;
    public float x;
    public float y;
    public float width;
    public float height;

    public PlayerState() {

    }

    public static PlayerState GetState() {
        PlayerState state = new();
        state.HasPlayer = player is not null;
        if (state.HasPlayer) {
            state.x = player.Collider.Left + player.X;
            state.y = player.Collider.Top + player.Y;
            state.width = player.Collider.Width;
            state.height = player.Collider.Height;
        }
        return state;
    }
}

public struct RenderData {
    public int index;
    public bool visible;
    public float x;
    public float y;
    public float width;
    public float height;
    public Color? KeyframeColor;

    public RenderData(int index, PlayerState PreviousState, PlayerState CurrentState) {
        this.index = index;
        if (CurrentState.HasPlayer) {
            x = CurrentState.x;
            y = CurrentState.y;
            width = CurrentState.width;
            height = CurrentState.height;
            visible = true;
        }
        else {
            x = 0f; y = 0f; width = 0f; height = 0f; visible = false;
        }
        KeyframeColor = null;
    }
}