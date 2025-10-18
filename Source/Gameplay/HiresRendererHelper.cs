using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.TASHelper.Gameplay;
public static class HiresLevelRenderer {
    // Hires but works similar with GameplayRenderer
    // Coordinate = 6 * that of Gameplay coordinate

    private const string renderTargetName = "TasHelperHiresLevel";

    [Load]
    public static void Load() {
        On.Celeste.Level.Begin += OnLevelBegin;
        On.Celeste.Level.End += OnLevelEnd;
        IL.Celeste.Level.Render += ILLevelRender;
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.Begin -= OnLevelBegin;
        On.Celeste.Level.End -= OnLevelEnd;
        IL.Celeste.Level.Render -= ILLevelRender;
    }

    private static void OnLevelBegin(On.Celeste.Level.orig_Begin orig, Level self) {
        SafeDispose();
        HiresLevelTarget = VirtualContent.CreateRenderTarget(renderTargetName, 1920, 1080);
        ClearAll();
        orig(self);
    }

    private static void OnLevelEnd(On.Celeste.Level.orig_End orig, Level self) {
        SafeDispose();
        ClearAll();
        orig(self);
    }

    private static void ClearAll() {
        list.Clear();
        toAdd.Clear();
        toRemove.Clear();
        tracker.Clear();
    }

    private static void SafeDispose() {
        if (HiresLevelTarget is not null && HiresLevelTarget.Name == renderTargetName) {
            // there's bug report that StarJumpBlock.orig_Render crashes, coz StarJumpController.BlockFill get nulled, which is a VirtualRenderTarget
            // the original code was "HiresLevelTarget?.Dispose()";
            // maybe the un-initialized HiresLevelTarget somehow points to StarJumpController.BlockFill?
            // though this does never happen to me
            // ...
            // oh this happens to me in TAS Helper 2.2.3
            // https://discord.com/channels/403698615446536203/1400781370497695815
            // the bug is not here
            // if 
            // (1) Celeste.Mod.TASHelper.Gameplay.Spinner.SimplifiedSpinner.Initialize has an il hook on Level.BeforeRender (even if it's an empty hook)
            // (2) Celeste.Mod.TASHelper.Entities.PauseUpdater.Detector.AddIfNecessary calls level.AddImmediately(new Detector()) instead of Level.Add
            // when both (1), (2) exists, bug occurs
            // --- Wartori tells me that:
            // hooking can cause other methods to get inlined in the hooked method
            // A calls B which can be inlined, so you hook A first, A is compiled and B inlined in it, then you hook B and issues arise, 
            // it works both ways
            // 
            // i fix it by no longer using AddImmediately
            HiresLevelTarget.Dispose();
        }
    }

    [SceneAfterUpdate]
    private static void OnLevelAfterUpdate(Scene self) {
        if (self is not Level) {
            return;
        }
        UpdateLists();
        foreach (THRenderer renderer in list) {
            renderer.Update();
        }
    }

    public static VirtualRenderTarget HiresLevelTarget = null;

    public static Camera camera = new Camera(1920, 1080);

    internal static readonly List<THRenderer> list = new();

    private static readonly List<THRenderer> toAdd = new();

    private static readonly List<THRenderer> toRemove = new();

    internal static readonly Dictionary<Type, List<THRenderer>> tracker = new();

    public static int Count => list.Count;

    public static void Add(THRenderer renderer) {
        toAdd.Add(renderer);
    }

    public static void AddIfNotPresent(THRenderer renderer) {
        if (!Contains(renderer) && !toAdd.Contains(renderer)) {
            toAdd.Add(renderer);
        }
    }

    public static void AddIfNotPresent<T>(T renderer) where T : THRenderer {
        if ((!tracker.TryGetValue(typeof(T), out List<THRenderer> list) || !list.Contains(renderer)) && (!toAdd.Contains(renderer))) {
            toAdd.Add(renderer);
        }
    }

    public static void Remove(THRenderer renderer) {
        toRemove.Add(renderer);
    }

    public static bool Contains(THRenderer renderer) {
        return list.Contains(renderer);
    }

    public static List<T> GetRenderers<T>() where T : THRenderer {
        if (tracker.TryGetValue(typeof(T), out List<THRenderer> list)) {
            return list.Select(x => (T)x).ToList();
        }
        return new List<T>();
    }

    public static void RemoveRenderers<T>() where T : THRenderer {
        if (tracker.TryGetValue(typeof(T), out List<THRenderer> removingType)) {
            foreach (THRenderer renderer in removingType) {
                list.Remove(renderer);
            }
            removingType.Clear();
        }
    }

    public static void UpdateLists() {
        foreach (THRenderer renderer in toAdd) {
            list.Add(renderer);
            if (tracker.TryGetValue(renderer.GetType(), out List<THRenderer> list_of_this_type)) {
                list_of_this_type.Add(renderer);
            }
            else {
                tracker.Add(renderer.GetType(), new List<THRenderer>() { renderer });
            }
        }

        foreach (THRenderer renderer in toRemove) {
            list.Remove(renderer);
            if (tracker.TryGetValue(renderer.GetType(), out List<THRenderer> list_of_this_type)) {
                list_of_this_type.Remove(renderer);
            }
            else {
                // how
            }
        }
        toAdd.Clear();
        toRemove.Clear();
    }

    private static void ILLevelRender(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        // render HiresLevelRenderer before SetRenderTarget(null)

        bool success = true;

        if (cursor.TryGotoNext(
                ins => ins.OpCode == OpCodes.Ldnull,
                ins => ins.OpCode == OpCodes.Callvirt,
                ins => ins.OpCode == OpCodes.Call
            )) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(Render);
        }
        else {
            success = false;
        }

        // and map it to null render target after subhud renderer, to have right depth
        // since CelesteTAS will debug render in subhud renderer instead (to render stuff out of camera) if CenterCamera.LevelZoomOut && TasSettings.CenterCamera
        int i = cursor.Index;
        if (cursor.TryGotoNext(
                ins => ins.OpCode == OpCodes.Ldfld && ins.Operand.ToString().Contains("SubHudRenderer")
            )) {
            cursor.Index += 3;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(MapToScreen);
            cursor.Index = i;
        }
        else {
            success = false;
        }

        if (!success) {
            throw new Exception($"TASHelper {nameof(HiresLevelRenderer)} failed to hook.");
        }
    }
    private static void Render(Level level) {
        if (HiresLevelTarget is null) {
            return;
        }

        UpdateLists();
        foreach (THRenderer renderer in list) {
            renderer.BeforeRender();
        }

        Engine.Instance.GraphicsDevice.SetRenderTarget(HiresLevelTarget);
        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);
        camera.Position = level.Camera.Position * 6f;
        camera.Origin = level.Camera.Origin * 6f;
        camera.Angle = level.Camera.Angle;
        camera.Zoom = level.Camera.Zoom;

        UpdateCameraData();
        /* Level.Camera = Level.GameplayRenderer.Camera in Celeste.LoadingThread()
         * and GameplayRenderer use Camera.Matrix in Draw.SpriteBatch.Begin(...)
         * i guess we never need to call HiresLevelRender.Before/./AfterUpdate, so needn't add it to Level.RendererList, so we do not hook Celeste.LoadingThread
        */
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, camera.Matrix);
        foreach (THRenderer renderer in list) {
            renderer.Render();
        }
        Draw.SpriteBatch.End();

        foreach (THRenderer renderer in list) {
            renderer.AfterRender();
        }
    }
    public static void MapToScreen(Level self) {
        if (HiresLevelTarget is null) {
            return;
        }

        float scale = self.Zoom * ((320f - self.ScreenPadding * 2f) / 320f);
        Vector2 vector = new Vector2(320f, 180f);
        Vector2 vector2 = vector / self.ZoomTarget;
        Vector2 vector3 = self.ZoomTarget != 1f ? (self.ZoomFocusPoint - vector2 / 2f) / (vector - vector2) * 6f * vector : Vector2.Zero;
        Vector2 vector4 = new Vector2(self.ScreenPadding, self.ScreenPadding * 9f / 16f) * 6f;
        SpriteEffects spriteEffects = SpriteEffects.None;
        if (SaveData.Instance?.Assists.MirrorMode ?? false) {
            vector4.X = -vector4.X;
            vector3.X = 1920f - vector3.X;
            spriteEffects |= SpriteEffects.FlipHorizontally;
        }
        if (ModUtils.UpsideDown) {
            spriteEffects |= SpriteEffects.FlipVertically;
        }
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Engine.ScreenMatrix);
        Draw.SpriteBatch.Draw((RenderTarget2D)HiresLevelTarget, vector3 + vector4, HiresLevelTarget.Bounds, Color.White, 0f, vector3, scale, spriteEffects, 0f);
        Draw.SpriteBatch.End();
    }

    private static void UpdateCameraData() {
        LeftWithPadding = camera.Left - Padding;
        RightWithPadding = camera.Right + Padding;
        TopWithPadding = camera.Top - Padding;
        BottomWithPadding = camera.Bottom + Padding;
    }

    internal static bool InBound(float left, float right, float top, float bottom) {
        return left < RightWithPadding && right > LeftWithPadding && bottom > TopWithPadding && top < BottomWithPadding;
    }

    internal static bool InBound(Vector2 vec) {
        // for small stuff
        return vec.X < RightWithPadding && vec.X > LeftWithPadding && vec.Y > TopWithPadding && vec.Y < BottomWithPadding;
    }

    private static float LeftWithPadding;

    private static float RightWithPadding;

    private static float TopWithPadding;

    private static float BottomWithPadding;

    private const float Padding = 10f;
}

public class THRenderer {
    // TH short for Tas Helper
    public virtual void BeforeRender() { }

    public virtual void Render() { }

    public virtual void AfterRender() { }

    public virtual void Update() { }
}
