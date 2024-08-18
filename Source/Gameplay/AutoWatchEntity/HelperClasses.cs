
using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using TAS.EverestInterop;

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

    public float scale = 1f;

    public Vector2 justify;

    public HiresText(string text, Vector2 position, AutoWatchRenderer holder) {
        this.content = text;
        this.position = position;
        this.holder = holder;
        justify = new Vector2(0.5f, 0.5f);
    }

    public override void Render() {
        if (DebugRendered && holder.Visible) {
            Message.RenderMessage(content, position, justify, Config.HiresFontSize * scale, Config.HiresFontStroke * scale);
        }
    }

    public void Clear() {
        content = "";
    }

    public void Append(string s) {
        if (s == "") {
            return;
        }
        if (content == "") {
            content = s;
        }
        else {
            content += "\n" + s;
        }
    }

    public void Newline(int lines = 1) {
        content += new string('\n', lines);
    }
}

internal class AutoWatchTextRenderer : AutoWatchRenderer {

    public HiresText text;
    public AutoWatchTextRenderer(RenderMode mode, bool active) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        HiresLevelRenderer.Add(text = new HiresText("", entity.Position, this));
    }

    public override void EntityAdded(Scene scene) {
        base.EntityAdded(scene);
        if (text is not null) {
            HiresLevelRenderer.AddIfNotPresent(text); // without this, PlayerRender may get lost after EventTrigger "ch9_goto_the_future" (first two sides of Farewell)
        }
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

    private static string SignedString(this float f) {
        return f.ToString("+0.00;-0.00;0.00");
    }
    private static int ToCeilingFrames(float seconds) {
        return (int)Math.Ceiling(seconds / Engine.DeltaTime);
    }


    internal static int ToFrameData(this float seconds) {
        return ToCeilingFrames(seconds);
    }
    internal static string ToFrame(this float seconds) {
        if (seconds <= 0) {
            return "";
        }
        return ToCeilingFrames(seconds).ToString();
    }

    internal static string ToFrame(this int frames) {
        return frames.ToString();
    }

    internal static string PositionToAbsoluteSpeed(this Vector2 vector) {
        if (Format.Speed_UsePixelPerSecond) {
            return (vector.Length() / Engine.DeltaTime).ToString("0.00");
        }
        else { // pixel per frame
            return vector.Length().ToString("0.00");
        }
    }

    internal static string Speed2ToSpeed2(this Vector2 speed) {
        if (IsTiny(speed.X)) {
            if (IsTiny(speed.Y)) {
                return "";
            }
            return speed.Y.SpeedToSpeed();
        }
        else if (IsTiny(speed.Y)) {
            return speed.X.SpeedToSpeed();
        }
        return $"({speed.X.SpeedToSpeed()}, {speed.Y.SpeedToSpeed()})";
    }

    internal static string Speed2ToSpeed2ButBreakline(this Vector2 speed) {
        if (IsTiny(speed.X)) {
            if (IsTiny(speed.Y)) {
                return "";
            }
            return speed.Y.SpeedToSpeed();
        }
        else if (IsTiny(speed.Y)) {
            return speed.X.SpeedToSpeed();
        }
        return $"({speed.X.SpeedToSpeed()},\n {speed.Y.SpeedToSpeed()})";
    }

    internal static string SpeedToSpeed(this float speed) { // in case we do have a speed field
        if (Format.Speed_UsePixelPerSecond) {
            return speed.SignedString();
        }
        else { // pixel per frame
            return (speed * Engine.DeltaTime).SignedString();
        }
    }

    private static string PositionToSignedSpeedX(this float f) {
        if (Format.Speed_UsePixelPerSecond) {
            return (f / Engine.DeltaTime).SignedString();
        }
        else {
            return f.SignedString();
        }
    }

    internal static string Positon2ToSignedSpeed(this Vector2 deltaPosition) {
        if (IsTiny(deltaPosition.X)) {
            if (IsTiny(deltaPosition.Y)) {
                return "";
            }
            return PositionToSignedSpeedX(deltaPosition.Y);
        }
        else if (IsTiny(deltaPosition.Y)) {
            return PositionToSignedSpeedX(deltaPosition.X);
        }
        return $"({PositionToSignedSpeedX(deltaPosition.X)}, {PositionToSignedSpeedX(deltaPosition.Y)})";
    }

    internal const float epsilon = 1E-6f;

    internal const float Minus_epsilon = -1E-6f;

    private static bool IsTiny(float f) {
        return f < epsilon && f > Minus_epsilon;
    }
}

internal static class CoroutineFinder {
    public static bool FindCoroutineComponent(this Entity entity, string compiler_generated_class_name, out Tuple<Coroutine, System.Collections.IEnumerator> pair) {
        // e.g. nameof Celeste.FallingBlock+<Sequence>d__21 is "<Sequence>d__21"
        foreach (Component c in entity.Components) {
            if (c is not Coroutine coroutine) {
                continue;
            }
            if (coroutine.enumerators.FirstOrDefault(functioncall => functioncall.GetType().Name == compiler_generated_class_name) is System.Collections.IEnumerator func) {
                pair = Tuple.Create(coroutine, func);
                return true;
            }
        }
        Logger.Log(LogLevel.Debug, "TASHelper", $"AutoWatchEntity: can't find {compiler_generated_class_name} of {entity.GetEntityId()}");
        pair = null;
        return false;
    }

    public static System.Collections.IEnumerator FindIEnumrator(this Coroutine coroutine, string compiler_generated_class_name) {
        if (coroutine.enumerators.FirstOrDefault(functioncall => functioncall.GetType().Name == compiler_generated_class_name) is System.Collections.IEnumerator func) {
            return func;
        }
        return null;
    }
}




