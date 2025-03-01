using Celeste.Mod.TASHelper.Gameplay;
using Celeste.Mod.TASHelper.Module;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.Mod.TASHelper.Predictor.PredictorCore;

namespace Celeste.Mod.TASHelper.Predictor;
public class PredictorRenderer : Entity {

    public static Color ColorEndpoint => CustomColors.Predictor_EndpointColor;

    public static Color ColorFinestScale => CustomColors.Predictor_FinestScaleColor;

    public static Color ColorFineScale => CustomColors.Predictor_FineScaleColor;

    public static Color ColorCoarseScale => CustomColors.Predictor_CoarseScaleColor;

    public static Color ColorKeyframe => CustomColors.Predictor_KeyframeColor;

    private static readonly List<Tuple<RenderData, Color>> processedKeyframeRenderData = new List<Tuple<RenderData, Color>>();

    private static readonly List<Tuple<RenderData, Color>> processedNormalframeRenderData = new List<Tuple<RenderData, Color>>();

    public static bool contentCached = false;

    public static bool UsePolygonalLine => TasHelperSettings.TimelineFinestScale == TASHelperSettings.TimelineFinestStyle.PolygonLine;

    public static bool UseDottedPolygonalLine => TasHelperSettings.TimelineFinestScale == TASHelperSettings.TimelineFinestStyle.DottedPolygonLine;

    public PredictorRenderer() {
        Depth = 1;
    }
    public static void ClearCachedMessage() {
        if (contentCached) {
            PredictorTextRenderer.Clear();
            PolygonalLineRenderer.Clear();
            processedKeyframeRenderData.Clear();
            processedNormalframeRenderData.Clear();
            contentCached = false;
        }
    }

    public override void Render() {
        if (!DebugRendered) {
            RenderCore();
        }
    }
    public override void DebugRender(Camera camera) {
        RenderCore();
    }

    private static void RenderCore() {
        if (!TasHelperSettings.PredictFutureEnabled || !FrameStep) {
            ClearCachedMessage();
            return;
        }

        if (!contentCached) {
            int count = Math.Min(futures.Count, TasHelperSettings.TimelineLength);

            if (UsePolygonalLine || UseDottedPolygonalLine) {
                Dictionary<int, List<Vector2>> lists = new();
                int curr_list = 0;
                lists[0] = new List<Vector2>();
                if (Engine.Scene.GetPlayer() is { } player) {
                    lists[curr_list].Add((player.Center + player.PositionRemainder) * 6f);
                }
                foreach (RenderData data in futures) {
                    if (data.index > count) {
                        continue;
                    }
                    if (data.visible) {
                        lists[curr_list].Add(new Vector2(data.exactX + data.width / 2f, data.exactY + data.height / 2f) * 6f);
                    }
                    else if (lists[curr_list].IsNotNullOrEmpty()) {
                        curr_list++;
                        lists[curr_list] = new List<Vector2>();
                    }
                }
                if (lists[curr_list].IsNullOrEmpty()) {
                    lists.Remove(curr_list);
                }
                if (lists.IsNotNullOrEmpty()) {
                    HiresLevelRenderer.Add(new PolygonalLineRenderer(lists));
                }
            }

            foreach (RenderData data in futures) {
                if (data.index > count || !data.visible) {
                    // those extra cached frames
                    continue;
                }
                if (TasHelperSettings.UseKeyFrame && KeyframeColorGetter(data.Keyframe, out bool addTime) is Color colorKeyframe) {
                    processedKeyframeRenderData.Add(new Tuple<RenderData, Color>(data with { addTime = addTime }, colorKeyframe));
                }
                else if (ColorSelector(data.index, count) is Color color) {
                    processedNormalframeRenderData.Add(new Tuple<RenderData, Color>(data, color));
                }
            }
        }

        foreach (Tuple<RenderData, Color> data in processedNormalframeRenderData) {
            RenderData keyframeData = data.Item1;
            Draw.HollowRect(keyframeData.x, keyframeData.y, keyframeData.width, keyframeData.height, data.Item2);
        }

        // todo: add descriptions to some keyframeData addTime
        foreach (Tuple<RenderData, Color> data in processedKeyframeRenderData) {
            RenderData keyframeData = data.Item1;
            Draw.HollowRect(keyframeData.x, keyframeData.y, keyframeData.width, keyframeData.height, data.Item2);
            if (!contentCached && TasHelperSettings.UseKeyFrameTime && keyframeData.addTime) {
                HiresLevelRenderer.Add(new PredictorTextRenderer(keyframeData.index.ToString(), new Vector2(keyframeData.x + keyframeData.width / 2, keyframeData.y - 1f) * 6f));
            }
        }
        // render keyframes above normal frames
        contentCached = true;
    }

    public static Color? KeyframeColorGetter(KeyframeType keyframe, out bool addTime) {
        addTime = true;
        if (TasHelperSettings.UseFlagDead && keyframe.Has(KeyframeType.GainDead)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGainCrouched && keyframe.Has(KeyframeType.GainDuck)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGainLevelControl && keyframe.Has(KeyframeType.GainLevelControl)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGainOnGround && keyframe.Has(KeyframeType.GainOnGround)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGainPlayerControl && keyframe.Has(KeyframeType.GainPlayerControl)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGainUltra && keyframe.Has(KeyframeType.GainUltra)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagLoseCrouched && keyframe.Has(KeyframeType.LoseDuck)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagLoseLevelControl && keyframe.Has(KeyframeType.LoseLevelControl)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagLoseOnGround && keyframe.Has(KeyframeType.LoseOnGround)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagLosePlayerControl && keyframe.Has(KeyframeType.LosePlayerControl)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagOnBounce && keyframe.Has(KeyframeType.OnBounce)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagOnEntityState && keyframe.Has(KeyframeType.OnEntityState)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagRefillDash && keyframe.Has(KeyframeType.RefillDash)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagRespawnPointChange && keyframe.Has(KeyframeType.RespawnPointChange)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagCanDashInStLaunch && keyframe.Has(KeyframeType.CanDashInStLaunch)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGainFreeze && keyframe.Has(KeyframeType.BeginEngineFreeze)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagLoseFreeze && keyframe.Has(KeyframeType.EndEngineFreeze)) {
            return ColorKeyframe;
        }
        if (TasHelperSettings.UseFlagGetRetained && keyframe.Has(KeyframeType.GetRetained)) {
            return ColorKeyframe;
        }

        addTime = false;
        return null;
    }

    public static Color? ColorSelector(int index, int count) {
        if (index == count) {
            return ColorEndpoint;
        }
        if (TasHelperSettings.TimelineCoarseScale != TASHelperSettings.TimelineScales.NotApplied && index % TASHelperSettings.ToInt(TasHelperSettings.TimelineCoarseScale) == 0) {
            return ColorCoarseScale * (TasHelperSettings.TimelineFadeOut ? (1 - 0.1f * Math.Min((float)index / FadeOutCoraseTillThisFrame, 1f)) : 1f);
        }
        if (TasHelperSettings.TimelineFineScale != TASHelperSettings.TimelineScales.NotApplied && index % TASHelperSettings.ToInt(TasHelperSettings.TimelineFineScale) == 0) {
            return ColorFineScale * (TasHelperSettings.TimelineFadeOut ? (1 - 0.3f * Math.Min((float)index / FadeOutFineTillThisFrame, 1f)) : 1f);
        }
        if (TasHelperSettings.TimelineFinestScale == TASHelperSettings.TimelineFinestStyle.HitboxPerFrame) {
            return ColorFinestScale * (TasHelperSettings.TimelineFadeOut ? (1 - 0.5f * Math.Min((float)index / FadeOutFinestTillThisFrame, 1f)) : 1f);
        }
        return null;
    }

    private const float FadeOutFinestTillThisFrame = 50f;

    private const float FadeOutFineTillThisFrame = 100f;

    private const float FadeOutCoraseTillThisFrame = 500f;

    [LoadLevel]
    private static void OnLoadLevel(Level self) {
        self.Add(new PredictorRenderer());
    }
}


public class PredictorTextRenderer : THRenderer {
    // use for those texts that appear and die quickly

    public string text;
    public Vector2 position;
    public static Vector2 Scale;
    public static float Stroke;

    public PredictorTextRenderer(string text, Vector2 position) {
        this.text = text;
        this.position = position;
    }

    public static void UpdateSettings() {
        Scale = new Vector2(TasHelperSettings.PredictorHiresFontSize / 10f);
        Stroke = TasHelperSettings.PredictorHiresFontStroke * 0.4f;

    }

    public override void Render() {
        TASHelper.Entities.Message.RenderMessage(text, position, new Vector2(0.5f, 0.2f), Scale, Stroke);
    }

    public static void Clear() {
        foreach (PredictorTextRenderer tmp in HiresLevelRenderer.GetRenderers<PredictorTextRenderer>()) {
            HiresLevelRenderer.Remove(tmp);
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