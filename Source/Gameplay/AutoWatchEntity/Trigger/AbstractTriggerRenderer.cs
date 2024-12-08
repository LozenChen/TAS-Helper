
using Microsoft.Xna.Framework;
using Monocle;
using System.Text;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal abstract class AbstractTriggerRenderer : AutoWatchTextRenderer {

    internal static Color textcolorWhenInside = new Color(0f, 0f, 0f, 0.5f);

    internal static Color innerRegionDefault = Color.White * 0.15f;

    internal static Color innerRegionTransparent = innerRegionDefault * 0.3f;

    internal static float defaultTextScale = 0.8f;

    public Entity abstractTrigger;

    public Hitbox hitbox;

    public Level level;

    public string staticInfo;

    private Color innerRegion;

    public Hitbox nearPlayerDetector;

    private bool positionInitialized = false;

    public bool orig_Visible;

    public Vector2 measure;

    public bool hasDynamicInfo = false;
    public float TextTop => text.Position.Y - measure.Y / 2f;

    public float TextBottom => text.Position.Y + measure.Y / 2f;

    public float TextLeft => text.Position.X - measure.X / 2f;

    public float TextRight => text.Position.X + measure.X / 2f;

    public AbstractTriggerRenderer(RenderMode mode) : base(mode, active: true) { }

    public abstract string Name();
    public abstract string GetStaticInfo();

    public abstract bool HasDynamicInfo();
    public abstract string GetDynamicInfo();

    public override void Added(Entity entity) {
        base.Added(entity);
        orig_Visible = Visible;
        Visible = false;
        abstractTrigger = entity;
        hitbox = abstractTrigger.Collider as Hitbox;
        level = abstractTrigger.Scene as Level;

        text.scale = defaultTextScale;
        SetAlphaText(false);
        SetAlphaRegion(false);

        text.content = NameSplitter(Name(), entity.Width / text.scale * 6f);
        text.Append(GetStaticInfo()); // we'll not split info
        staticInfo = text.content;
        hasDynamicInfo = HasDynamicInfo();
        if (hasDynamicInfo) {
            text.Append(GetDynamicInfo());
        }

        measure = Measure(text.content) * text.scale / 6f;
        nearPlayerDetector = new Hitbox(measure.X + 6f, measure.Y + 6f, 0f, 0f);
        // even if the dynamicInfo change later, we will still not change the near player detector

        text.Position = abstractTrigger.Center;
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

            // split at capital letters
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
        Visible = orig_Visible && !SimplifiedTrigger.IsUnimportantTrigger(abstractTrigger);
        if (Visible) {
            if (hasDynamicInfo) {
                text.content = staticInfo;
                text.Append(GetDynamicInfo());
            }

            if (!positionInitialized) {
                positionInitialized = true;
                SetVerticallyClampedCenter();
                SetNearPlayerDetector();
            }
            bool flag1 = false;
            bool flag2 = false;
            if (playerInstance is { } player) {
                flag1 = abstractTrigger.CollideCheck(player);
                flag2 = mode == RenderMode.Always && nearPlayerDetector.Collide(player.collider) && !CoreLogic.IsWatched(abstractTrigger); // if watched, then do not set transparent text
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
        if (hitbox is not null) {
            Draw.Rect(hitbox.AbsoluteX, hitbox.AbsoluteY, hitbox.Width, hitbox.Height, innerRegion);
        }
        // Draw.HollowRect(nearPlayerDetector.AbsoluteX, nearPlayerDetector.AbsoluteY, nearPlayerDetector.Width, nearPlayerDetector.Height, Color.Pink);
    }

    public void SetVerticallyClampedCenter() {
        if (abstractTrigger.Top > level.Camera.Top && abstractTrigger.Bottom < level.Camera.Bottom) {
            // the trigger is completely inside the screen, we can do nothing better than just set the text in the center
            return;
        }
        if (level.Camera.Top > abstractTrigger.Bottom) {
            // if the trigger is completely outside of screen, we put the text inside the trigger + as near the screen as possible
            text.Y = abstractTrigger.Bottom;
            text.justify.Y = 1f;
            return;
        }
        if (level.Camera.Bottom < abstractTrigger.Top) {
            text.Y = abstractTrigger.Top;
            text.justify.Y = 0f;
            return;
        }
        float top = Math.Max(abstractTrigger.Top, level.Camera.Top);
        float bottom = Math.Min(abstractTrigger.Bottom, level.Camera.Bottom);

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





