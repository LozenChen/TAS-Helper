using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using System.Reflection;
using Monocle;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Celeste.Mod.SpeedrunTool.RoomTimer;
using Microsoft.Xna.Framework.Input;
using System;

namespace Celeste.Mod.TASHelper.Predictor;
public static class Core {

    public static readonly List<RenderData> futures = new ();

    public static bool HasPredict = false;

    public static bool InPredict = false;
    public static void Predict(int frames) {
        if (!TasHelperSettings.PredictFuture || InPredict) {
            return;
        }

        SafePredict(frames);
        // we make it nested in case SpeedrunTool is not installed
    }

    private static void SafePredict(int frames) {
        if (!StartPredictCheck()) {
            return;
        }

        
        if (!P_StateManager.Instance.SaveState()) {
            return;
        }
        //SpeedrunTool.SaveLoad.StateManager.Instance.SaveState();

        InPredict = true;

        if (HasPredict) {
            futures.Clear();
            HasPredict = false;
        }

        ModifiedAutoMute.StartMute();
        InputManager.ReadInputs(frames);

        //todo: this overrides TAS's savestate


        PlayerState PreviousState;
        PlayerState CurrentState = PlayerState.GetState();

        StoreEngineState();
        for (int i = 0; i < frames; i++) {
            TAS.InputHelper.FeedInputs(InputManager.P_Inputs[i]);
            // commands are not supported

            AlmostEngineUpdate(Engine.Instance, (GameTime) typeof(Game).GetFieldInfo("gameTime").GetValue(Engine.Instance));

            PreviousState = CurrentState;
            CurrentState = PlayerState.GetState();
            if (TooEarlyStopCheck(CurrentState)) {
                break;
            }
            futures.Add(new RenderData(i + 1, PreviousState, CurrentState));
            if (EarlyStopCheck(CurrentState)) {
                break;
            }
        }

        //SpeedrunTool.SaveLoad.StateManager.Instance.LoadState();
        P_StateManager.Instance.LoadState();

        RestoreEngineState();
        ModifiedAutoMute.EndMute();

        HasPredict = true;
        InPredict = false;
    }

    private static float rawDeltaTime;
    private static float deltaTime;
    private static ulong frameCounter;
    private static bool dashAssistFreezePress;
    private static float freezeTimer;
    private static void StoreEngineState() {
        rawDeltaTime = Engine.RawDeltaTime;
        deltaTime = Engine.DeltaTime;
        frameCounter = Engine.FrameCounter;
        dashAssistFreezePress = Engine.DashAssistFreezePress;
        freezeTimer = Engine.FreezeTimer;
    }

    private static void RestoreEngineState() {
        Engine.RawDeltaTime = rawDeltaTime;
        Engine.DeltaTime = deltaTime;
        Engine.FrameCounter = frameCounter;
        Engine.DashAssistFreezePress = dashAssistFreezePress;
        Engine.FreezeTimer = freezeTimer;
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
        
        /* dont do this, leave it to EarlyStopCheck
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
            if (Engine.Scene is Level level && FrameStep) {
                Predict(TasHelperSettings.FutureLength);
            }
        });

        typeof(Engine).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).HookBefore(() => {
            P_StateManager.Instance.HasCached = false;
            P_StateManager.Instance.ClearState();
        });

        // todo: refresh predict on tas file change

        typeof(Scene).GetMethod("BeforeUpdate").HookAfter(() => {
            if (!InPredict) {
                futures.Clear();
                HasPredict = false;
            }
        });
    }

    public static bool StartPredictCheck() {
        if (Engine.Scene is Level level && level.Transitioning) {
            return false;
        }
        return true;
    }

    public static bool TooEarlyStopCheck(PlayerState state) {
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