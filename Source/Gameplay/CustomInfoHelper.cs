using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using TAS.EverestInterop;
using CelesteInput = Celeste.Input;

namespace Celeste.Mod.TASHelper.Gameplay;

public static class CustomInfoHelper {
    // provide some property for Custom Info

    private static string cachedCS = "";
    public static string Cutscene {
        get {
            if (Engine.Scene is not Level level) {
                return cachedCS = "";
            }
            foreach (Entity cs in level.Tracker.SafeGetEntities<CutsceneEntity>()) {
                if (cs is CutsceneEntity cse && cse.Running) {
                    return cachedCS = cse.GetType().Name;
                }
            }
            if (level.SkippingCutscene) {
                // can be wrong if cached CS and turn off, and turn on agagin when skipping another cutscene, but this does not really matter
                // let's just avoid Updates as many as possible
                return cachedCS;
            }
            return cachedCS = "";
        }
    }

    public static MInput.GamePadData GamePadData => MInput.GamePads[CelesteInput.Gamepad];

    public static Microsoft.Xna.Framework.Input.Buttons GamePadPreviousState => GamePadData.PreviousState.Buttons.buttons;

    public static Microsoft.Xna.Framework.Input.Buttons GamePadCurrentState => GamePadData.CurrentState.Buttons.buttons;

    /*
     * crashes due to CelesteTAS renaming, so just hide them
    public static Vector2 MouseState => MouseButtons.Position;
    public static Vector2 MouseCursorPos => Vector2.Transform(new Vector2(MouseState.X, MouseState.Y), Matrix.Invert(Engine.ScreenMatrix));

    public static Vector2 TASMouseCursorPosNaive => MouseCursorPos / 6f;
    public static Vector2 TASMouseCursorPos {
        get {
            // even works when you use CenterCamera
            if (Engine.Scene is not Level level || !TasSettings.CenterCamera || typeof(CenterCamera).GetFieldInfo("savedCameraPosition").GetValue(null) is null) return TASMouseCursorPosNaive;
            Vector2 mouseWorldPosition = level.MouseToWorld(MouseState);
            typeof(CenterCamera).GetMethodInfo("RestoreTheCamera").Invoke(null, parameterless);
            Vector2 mouseScreenPosition = level.WorldToScreen(mouseWorldPosition);
            typeof(CenterCamera).GetMethodInfo("CenterTheCamera").Invoke(null, parameterless);
            return mouseScreenPosition / 6f;
        }
    }
    */

    // sadly, Studio currently only support 320 * 180 (for sync consideration), though we can actually do much more beyond this

    /* 
     * GameInfo.Update(..) is called in Scene.AfterUpdate() (or in FreezeFrames), so string CustomInfo is not dynamically updated like InfoMouse in a single frame.
     *  have to manually update CustomInfo (e.g. Right Click) if you add it to CustomInfo
     */
    

    public static float PlayerIntPositionX {
        get => playerInstance?.X ?? 0;
        set {
            if (playerInstance is not null) { playerInstance.X = value; }
        }
    }
    public static float PlayerIntPositionY {
        get => playerInstance?.Y ?? 0;
        set {
            if (playerInstance is not null) { playerInstance.Y = value; }
        }
    }
    // TAS mod somehow redirects Player.Position, so we provide this

    [Initialize]
    private static void Initialize() {
        LevelExtensions.AddToTracker(typeof(CutsceneEntity), true);
    }
}