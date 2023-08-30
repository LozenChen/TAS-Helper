using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using System.Reflection;
using Monocle;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Celeste.Mod.SpeedrunTool.SaveLoad;

namespace Celeste.Mod.TASHelper.Predictor;
public static class Core {

    public static readonly List<RenderData> futures = new ();

    public static bool HasPredict = false;
    public static void Predict(int frames) {
        if (!TasHelperSettings.PredictFuture) {
            return;
        }

        if (!StartPredictCheck()) {
            return;
        }

        if (!ModifiedSaveLoad.SaveState()) {
            return;
        }

        if (HasPredict) {
            futures.Clear();
            HasPredict = false;
        }

        ModifiedAutoMute.StartMute();
        InputManager.ReadInputs(frames);

        //todo: this overrides TAS's savestate
        

        PlayerState PreviousState;
        PlayerState CurrentState = PlayerState.GetState();

        for (int i = 0; i < frames; i++) {
            TAS.InputHelper.FeedInputs(InputManager.P_Inputs[i]);
            if (Engine.FreezeTimer > 0f) {
                Engine.FreezeTimer = Math.Max(Engine.FreezeTimer - Engine.RawDeltaTime, 0f);
            }
            else {
                Engine.Scene.Update();
            }
            PreviousState = CurrentState;
            CurrentState = PlayerState.GetState();
            futures.Add(new RenderData(i + 1, PreviousState, CurrentState));
            if (EarlyStopCheck(CurrentState)) {
                break;
            }
        }
        ModifiedSaveLoad.LoadState();
        ModifiedAutoMute.EndMute();

        HasPredict = true;
    }

    public static void Initialize() {
        typeof(Engine).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).HookAfter(() => {
            if (Engine.Scene is Level level && FrameStep) {
                Predict(TasHelperSettings.FutureLength);
            }
        });

        // todo: refresh predict on tas file change

        typeof(Scene).GetMethod("BeforeUpdate").HookAfter(() => {
            futures.Clear();
            HasPredict = false;
        });
    }

    public static bool StartPredictCheck() {
        if (Engine.Scene is Level level && level.Transitioning) {
            return false;
        }
        return true;
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