
using Celeste.Mod.TASHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class HiresText : THRenderer {

    internal AutoWatchRenderer holder;
    public string content;
    private Vector2 position;
    public Vector2 Position {
        set {
            position = value * 6f;
        }
    }

    public HiresText(string text, Vector2 position, AutoWatchRenderer holder) {
        this.content = text;
        this.position = position;
        this.holder = holder;
    }

    public override void Render() {
        if (DebugRendered && holder.Visible) {
            Message.RenderMessage(content, position, new Vector2(0.5f, 0.5f), new Vector2(TasHelperSettings.HiresFontSize / 10f), TasHelperSettings.HiresFontStroke * 0.4f);
        }
    }

    public void Clear() {
        content = "";
    }

    public void Append(string s) {
        if (content == "") {
            content = s;
        }
        else {
            content += "\n" + s;
        }
    }
}

internal class AutoWatchTextRenderer : AutoWatchRenderer {

    public HiresText text;
    public AutoWatchTextRenderer(RenderMode mode, bool active) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        HiresLevelRenderer.Add(text = new HiresText("", entity.Position, this));
    }

    public override void Removed(Entity entity) {
        base.Removed(entity);
        if (text is not null) {
            HiresLevelRenderer.Remove(text);
        }
    }
    public override void EntityRemoved(Scene scene) {
        base.EntityRemoved(scene);
        if (text is not null) {
            HiresLevelRenderer.Remove(text);
        }
    }

    public void SetVisible() {
        Visible = text.content != "";
    }
}

internal static class InfoParser {
    private static int ToCeilingFrames(float seconds) {
        return (int)Math.Ceiling(seconds / Engine.DeltaTime);
    }

    internal static string ToFrame(this float seconds) {
        return ToCeilingFrames(seconds).ToString();
    }

    internal static string DeltaPositionToSpeed(this Vector2 vector) {
        if (Format.Speed_UsePixelPerSecond) {
            return (vector.Length() / Engine.DeltaTime).ToString("0.00");
        }
        else { // pixel per frame
            return vector.Length().ToString("0.00");
        }
    }

    internal static string SpeedToSpeed(this float speed) { // in case we do have a speed field
        if (Format.Speed_UsePixelPerSecond) {
            return speed.ToString("0.00");
        }
        else { // pixel per frame
            return (speed * Engine.DeltaTime).ToString("0.00");
        }
    }

    internal static string ToDirectedSpeedX(this float f) {
        if (IsTiny(f)) {
            return "0.00";
        }
        string sign = f > 0 ? "+" : "-";
        if (f < 0) {
            f = -f;
        }
        if (Format.Speed_UsePixelPerSecond) {
            return sign + (f / Engine.DeltaTime).ToString("0.00");
        }
        else {
            return sign + f.ToString("0.00");
        }
    }

    internal static string ToDirectedVector2Speed(this Vector2 vector) {
        if (IsTiny(vector.X)) {
            return ToDirectedSpeedX(vector.Y);
        }
        else if (IsTiny(vector.Y)) {
            return ToDirectedSpeedX(vector.X);
        }
        return $"({ToDirectedSpeedX(vector.X)}, {ToDirectedSpeedX(vector.Y)})";
    }

    internal const float epsilon = 1E-6f;

    internal const float Minus_epsilon = -1E-6f;

    private static bool IsTiny(float f) {
        return f < epsilon && f > Minus_epsilon;
    }
}

internal static class CoroutineFinder {
    public static Tuple<Coroutine, System.Collections.IEnumerator> FindCoroutine(this Entity entity, string compiler_generated_class_name) {
        // e.g. nameof Celeste.FallingBlock+<Sequence>d__21 is "<Sequence>d__21"
        foreach (Component c in entity.Components) {
            if (c is not Coroutine coroutine) {
                continue;
            }
            if (coroutine.enumerators.FirstOrDefault(functioncall => functioncall.GetType().Name == compiler_generated_class_name) is System.Collections.IEnumerator func) {
                return Tuple.Create(coroutine, func);
            }
        }
        return null;
    }

    public static System.Collections.IEnumerator FindIEnumrator(this Coroutine coroutine, string compiler_generated_class_name) {
        if (coroutine.enumerators.FirstOrDefault(functioncall => functioncall.GetType().Name == compiler_generated_class_name) is System.Collections.IEnumerator func) {
            return func;
        }
        return null;
    }
}




