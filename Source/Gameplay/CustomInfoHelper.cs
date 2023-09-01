using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using TAS.EverestInterop;

namespace Celeste.Mod.TASHelper.Gameplay;

public static class CustomInfoHelper {
    // provide some property for Custom Info

    public static Vector2 MouseState => MouseButtons.Position;
    public static Vector2 MouseCursorPos => Vector2.Transform(new Vector2(MouseState.X, MouseState.Y), Matrix.Invert(Engine.ScreenMatrix));

    public static Vector2 TASMouseCursorPosNaive => MouseCursorPos / 6f;
    public static Vector2 TASMouseCursorPos {
        get {
            // even works when you use CenterCamera
            if (Engine.Scene is not Level level || !TasSettings.CenterCamera || typeof(CenterCamera).GetFieldInfo("savedCameraPosition").GetValue(null) is null) return TASMouseCursorPosNaive;
            Vector2 mouseWorldPosition = level.MouseToWorld(MouseState);
            object[] Parameterless = { };
            typeof(CenterCamera).GetMethodInfo("RestoreTheCamera").Invoke(null, Parameterless);
            Vector2 mouseScreenPosition = level.WorldToScreen(mouseWorldPosition);
            typeof(CenterCamera).GetMethodInfo("CenterTheCamera").Invoke(null, Parameterless);
            return mouseScreenPosition / 6f;
        }
    }
    // sadly, Studio currently only support 320 * 180 (for sync consideration), though we can actually do much more beyond this

    /* 
     * GameInfo.Update(..) is called in Scene.AfterUpdate() (or in FreezeFrames), so string CustomInfo is not dynamically updated like InfoMouse in a single frame.
     *  have to manually update CustomInfo (e.g. Right Click) if you add it to CustomInfo
     */


    public static float PlayerIntPositionX {
        get => player?.X ?? 0;
        set {
            if (player is not null) { player.X = value; }
        }
    }
    public static float PlayerIntPositionY {
        get => player?.Y ?? 0;
        set {
            if (player is not null) { player.Y = value; }
        }
    }
    // TAS mod somehow hides Player.Position, so we provide this
}