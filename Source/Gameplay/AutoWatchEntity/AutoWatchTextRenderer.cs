using Celeste.Mod.TASHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Text.RegularExpressions;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal partial class HiresText : THRenderer {

    internal AutoWatchRenderer holder;
    public string content;
    private Vector2 position;

    public float X {
        get => position.X / 6f;
        set => position.X = value * 6f;
    }

    public float Y {
        get => position.Y / 6f;
        set => position.Y = value * 6f;
    }

    public Vector2 Position {
        get {
            return position / 6f;
        }
        set {
            position = value * 6f;
        }
    }

    public float scale = 1f;

    public Vector2 justify;

    public Color colorInside = Color.White;

    public Color colorOutside = Color.Black;

    public HiresText(string text, Vector2 position, AutoWatchRenderer holder) {
        content = text;
        this.position = position;
        this.holder = holder;
        justify = new Vector2(0.5f, 0.5f);
    }

    public override void Render() {
        if (DebugRendered && holder.Visible) {
            Message.RenderMessage(content, position, justify, Config.HiresFontSize * scale, Config.HiresFontStroke * scale, colorInside, colorOutside);
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

    public void AppendAtFirst(string shortTitle, string longTitle) {
        if (FindLines.Count(content) > 0) {
            content = longTitle + "\n" + content;
        }
        else {
            content = shortTitle + ": " + content;
        }
    }

    public void Newline(int lines = 1) {
        content += new string('\n', lines);
    }

    private static readonly Regex FindLines = new Regex("\n", RegexOptions.Compiled);
}

internal class AutoWatchTextRenderer : AutoWatchRenderer {

    public HiresText text;
    public AutoWatchTextRenderer(RenderMode mode, bool active, bool preActive = false) : base(mode, active, preActive) { }

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

    public override void OnClone() {
        base.OnClone();
        if (text is not null) {
            HiresLevelRenderer.AddIfNotPresent(text);
        }
    }
}

internal class AutoWatchText2Renderer : AutoWatchTextRenderer {
    public HiresText textBelow;

    public Vector2 offset = Vector2.UnitY * 6f;
    public AutoWatchText2Renderer(RenderMode mode, bool active, bool preActive = false) : base(mode, active, preActive) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        HiresLevelRenderer.Add(textBelow = new HiresText("", entity.Position, this));
        textBelow.justify = new Vector2(0.5f, 0f);
    }

    public override void EntityAdded(Scene scene) {
        base.EntityAdded(scene);
        if (textBelow is not null) {
            HiresLevelRenderer.AddIfNotPresent(textBelow);
        }
    }

    public override void Removed(Entity entity) {
        base.Removed(entity);
        if (textBelow is not null) {
            HiresLevelRenderer.Remove(textBelow);
        }
    }
    public override void EntityRemoved(Scene scene) {
        base.EntityRemoved(scene);
        if (textBelow is not null) {
            HiresLevelRenderer.Remove(textBelow);
        }
    }

    public override void OnClone() {
        base.OnClone();
        if (textBelow is not null) {
            HiresLevelRenderer.AddIfNotPresent(textBelow);
        }
    }

    public new void SetVisible() {
        Visible = (text.content != "" || textBelow.content != "");
    }
}