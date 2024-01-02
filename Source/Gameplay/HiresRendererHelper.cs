using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.TASHelper.Gameplay;
public static class HiresLevelRenderer {
    // Hires but works similar with GameplayRenderer
    // Coordinate = 6 * that of Gameplay coordinate

    [Load]
    public static void Load() {
        On.Celeste.Level.Begin += OnLevelBegin;
        On.Celeste.Level.End += OnLevelEnd;
        IL.Celeste.Level.Render += ILLevelRender;
        On.Monocle.Scene.AfterUpdate += OnLevelAfterUpdate;
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.Begin -= OnLevelBegin;
        On.Celeste.Level.End -= OnLevelEnd;
        IL.Celeste.Level.Render -= ILLevelRender;
        On.Monocle.Scene.AfterUpdate -= OnLevelAfterUpdate;
    }

    private static void OnLevelBegin(On.Celeste.Level.orig_Begin orig, Level self) {
        HiresLevelTarget?.Dispose();
        HiresLevelTarget = VirtualContent.CreateRenderTarget("TasHelperHiresLevel", 1920, 1080);
        orig(self);
    }

    private static void OnLevelEnd(On.Celeste.Level.orig_End orig, Level self) {
        HiresLevelTarget?.Dispose();
        list.Clear();
        toAdd.Clear();
        toRemove.Clear();
        orig(self);
    }

    private static void OnLevelAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
        orig(self);
        UpdateLists();
        foreach (THRenderer renderer in list) {
            renderer.Update();
        }
    }

    public static VirtualRenderTarget HiresLevelTarget;

    public static Camera camera = new Camera(1920, 1080);

    internal static readonly List<THRenderer> list = new();

    private static readonly List<THRenderer> toAdd = new();

    private static readonly List<THRenderer> toRemove = new();

    internal static readonly Dictionary<Type, List<THRenderer>> tracker = new();


    public static void Add(THRenderer renderer) {
        toAdd.Add(renderer);
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

        if (cursor.TryGotoNext(
                ins => ins.OpCode == OpCodes.Ldnull,
                ins => ins.OpCode == OpCodes.Callvirt,
                ins => ins.OpCode == OpCodes.Call
            )) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(Render);
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
    }
    private static void Render(Level level) {
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
}

public class THRenderer {
    // TH short for Tas Helper
    public virtual void BeforeRender() { }

    public virtual void Render() { }

    public virtual void AfterRender() { }

    public virtual void Update() { }
}

public class TempTextRenderer : THRenderer {
    // use for those texts that appear and die quickly

    public string text;
    public Vector2 position;
    public string label;

    public TempTextRenderer(string text, Vector2 position, string label) {
        this.text = text;
        this.position = position;
        this.label = label;
    }

    public override void Render() {
        Message.RenderMessage(text, position, new Vector2(0.5f, 0.2f), new Vector2(TasHelperSettings.HiresFontSize / 10f), TasHelperSettings.HiresFontStroke * 0.4f);
    }

    public static void Clear(string label) {
        foreach (TempTextRenderer tmp in HiresLevelRenderer.GetRenderers<TempTextRenderer>()) {
            if (tmp.label == label) {
                HiresLevelRenderer.Remove(tmp);
            }
        }
    }
}

public class PolygonalLineRenderer : THRenderer {
    public Dictionary<int, List<Vector2>> lists = new();

    public PolygonalLineRenderer(Dictionary<int, List<Vector2>> lists) {
        this.lists = lists;
    }

    public override void Render() {
        float lineWidth = TasHelperSettings.PredictorLineWidth;
        float pointWidth = TasHelperSettings.PredictorPointSize;
        if (lineWidth == 0f && pointWidth == 0f) {
            return;
        }
        int count = lists.Keys.Count;
        int index = 0;
        Color colorLine = CustomColors.Predictor_PolygonalLineColor;
        Color colorPoint = CustomColors.Predictor_DotColor;
        bool drawPoint = pointWidth > 0 && TasHelperSettings.TimelineFinestScale == Module.TASHelperSettings.TimelineFinestStyle.DottedPolygonLine;
        for (int i = 0; i < count; i++) {
            List<Vector2> nodes = lists[i];
            for (int j = 0; j < nodes.Count - 1; j++) {
                if (drawPoint) {
                    nodes[j].MonocleDrawPoint(colorPoint, pointWidth);
                }
                if (lineWidth > 0) {
                    Monocle.Draw.Line(nodes[j], nodes[j + 1], colorLine, lineWidth);
                }
                index++;
            }
            if (drawPoint) {
                nodes.Last().MonocleDrawPoint(colorPoint, pointWidth);
            }
        }
    }

    public static void Clear() {
        foreach (PolygonalLineRenderer renderer in HiresLevelRenderer.GetRenderers<PolygonalLineRenderer>()) {
            HiresLevelRenderer.Remove(renderer);
        }
    }

}