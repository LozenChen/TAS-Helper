
using Microsoft.Xna.Framework;
using Monocle;
using System.Text;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class TriggerRenderer : AutoWatchTextRenderer {

    private static Color textcolorWhenInside = new Color(0f, 0f, 0f, 0.5f);

    private static Color innerRegionDefault = Color.White * 0.15f;

    private static Color innerRegionTransparent = innerRegionDefault * 0.3f;

    public Trigger trigger;

    public Hitbox hitbox;

    public Level level;

    public string static_info;

    private Color innerRegion;

    public Hitbox nearPlayerDetector;

    public bool position_initialized = false;

    public bool orig_Visible;

    public Vector2 measure;
    public float TextTop => text.Position.Y - measure.Y / 2f;

    public float TextBottom => text.Position.Y + measure.Y / 2f;

    public float TextLeft => text.Position.X - measure.X / 2f;

    public float TextRight => text.Position.X + measure.X / 2f;

    public TriggerRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        orig_Visible = Visible;
        Visible = false;
        trigger = entity as Trigger;
        hitbox = trigger.collider as Hitbox;
        level = trigger.Scene as Level;

        text.scale = 0.8f;
        SetAlphaText(false);
        SetAlphaRegion(false);

        string name_raw = trigger.GetType().Name;
        text.content = NameSplitter(name_raw, hitbox.Width / text.scale * 6f);
        text.Append(TriggerHelper.GetStaticInfo(trigger));
        static_info = text.content;
        text.Append(TriggerHelper.GetDynamicInfo(trigger));

        measure = Measure(text.content) * text.scale / 6f;
        nearPlayerDetector = new Hitbox(measure.X + 6f, measure.Y + 6f, 0f, 0f);

        text.Position = trigger.Center;
        SetNearPlayerDetector();
    }

    public void SetAlphaRegion(bool transparent = false) {
        if (transparent) {
            innerRegion = innerRegionTransparent;
        }
        else {
            innerRegion = innerRegionDefault;
        }
    }

    public void SetAlphaText(bool transparent = false) {
        if (transparent) {
            text.colorInside = textcolorWhenInside;
            text.colorOutside = Color.Transparent;
        }
        else {
            text.colorInside = Color.White;
            text.colorOutside = Color.Black;
        }
    }

    public void SetNearPlayerDetector() {
        nearPlayerDetector.Center = text.Position;
    }

    public static Vector2 Measure(string str) => TASHelper.Entities.Message.Measure(str);
    public static float MeasureX(string str) => TASHelper.Entities.Message.Measure(str).X;

    public static string NameSplitter(string name, float width) {
        if (name.Length < 6) {
            return name;
        }
        if (width < 120f) {
            width = 120f;
        }
        float x = MeasureX(name);
        if (x < width) {
            return name;
        }
        else {
            name = string.Concat(name.Substring(0, 1).ToUpperInvariant(), name.AsSpan(1)); // just in case the first char is not upper case

            float averageWidth = x / name.Length;
            int numbersWithinLine = Math.Max((int)Math.Floor(width / averageWidth), 6);

            int expectedLines = ((name.Length - 1) / numbersWithinLine) + 1;
            int start = 0;

            // split at word
            StringBuilder sb = new();
            int startOfThisLine = 0;
            int lastCapital = 0;
            int lines = 0;
            for (int i = 1; i <= name.Length; i++) {
                if (i == name.Length || Char.IsUpper(name[i])) {
                    if (i - startOfThisLine > numbersWithinLine) {
                        if (lastCapital == startOfThisLine) {
                            lines++;
                            sb.Append("\n");
                            sb.Append(name.Substring(startOfThisLine, i - startOfThisLine));
                            startOfThisLine = lastCapital = i;
                        }
                        else {
                            lines++;
                            sb.Append("\n");
                            sb.Append(name.Substring(startOfThisLine, lastCapital - startOfThisLine));
                            startOfThisLine = lastCapital;
                            lastCapital = i;
                        }
                    }
                    else {
                        lastCapital = i;
                    }
                }
            }
            if (startOfThisLine < name.Length - 1) {
                lines++;
                sb.Append("\n");
                sb.Append(name.Substring(startOfThisLine, name.Length - startOfThisLine));
            }
            if (lines <= 2 * expectedLines) {
                return sb.ToString().Trim();
            }

            // naive split

            start = 0;
            int length = Math.Min(name.Length - start, numbersWithinLine);
            List<string> results = new List<string>();
            while (length > 0) {
                results.Add(name.Substring(start, length));
                start += length;
                length = Math.Min(name.Length - start, numbersWithinLine);
            }
            return string.Join("\n", results);
        }
    }

    public override void UpdateImpl() {
        Visible = orig_Visible && !SimplifiedTrigger.IsUnimportantTrigger(trigger);
        if (Visible) {
            text.content = static_info;
            text.Append(TriggerHelper.GetDynamicInfo(trigger));

            if (!position_initialized) {
                position_initialized = true;
                SetVerticallyClampedCenter();
                SetNearPlayerDetector();
            }
            bool flag1 = false;
            bool flag2 = false;
            if (playerInstance is { } player) {
                flag1 = trigger.CollideCheck(player);
                flag2 = !CoreLogic.IsWatched(trigger) && nearPlayerDetector.Collide(player.collider);
            }
            SetAlphaRegion(flag1);
            SetAlphaText(flag2);
        }
    }

    public override void ClearHistoryData() {
        orig_Visible = Visible;
    }

    public override void DebugRenderImpl() {
        base.DebugRenderImpl();
        Draw.Rect(hitbox.AbsoluteX, hitbox.AbsoluteY, hitbox.Width, hitbox.Height, innerRegion);
        // Draw.HollowRect(nearPlayerDetector.AbsoluteX, nearPlayerDetector.AbsoluteY, nearPlayerDetector.Width, nearPlayerDetector.Height, Color.Pink);
    }

    public void SetVerticallyClampedCenter() {
        if (trigger.Top > level.Camera.Top && trigger.Bottom < level.Camera.Bottom) {
            // the trigger is completely inside the screen, we can do nothing better than just set the text in the center
            return;
        }
        if (level.Camera.Top > trigger.Bottom) {
            // if the trigger is completely outside of screen, we put the text inside the trigger + as near the screen as possible
            text.Y = trigger.Bottom;
            text.justify.Y = 1f;
            return;
        }
        if (level.Camera.Bottom < trigger.Top) {
            text.Y = trigger.Top;
            text.justify.Y = 0f;
            return;
        }
        float top = Math.Max(trigger.Top, level.Camera.Top);
        float bottom = Math.Min(trigger.Bottom, level.Camera.Bottom);

        text.Y = (top + bottom) / 2f;
        if (measure.Y <= bottom - top) {
            // the text is inside the trigger + screen
            return;
        }
        if (top == level.Camera.Top) {
            // make it extend to the top of the screen
            text.Y = bottom;
            text.justify.Y = 1f;
            return;
        }
        if (bottom == level.Camera.Bottom) {
            // make it extend to the bottom of the screen
            text.Y = top;
            text.justify.Y = 0f;
            return;
        }
        // the screen is completely contained in the trigger (vertically), and the text is too high
        // make it extend to the bottom of the screen
        text.Y = top;
        text.justify.Y = 0f;
        return;
    }
}

internal class TriggerFactory : IRendererFactory {
    public Type GetTargetType() => typeof(Trigger);


    public bool Inherited() => true;
    public RenderMode Mode() => Config.Trigger;
    public void AddComponent(Entity entity) {
        entity.Add(new TriggerRenderer(Mode()).SleepWhenUltraFastforward());
    }
}





