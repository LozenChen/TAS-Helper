using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System.Reflection;
using static Celeste.Mod.TASHelper.Module.TASHelperSettings;

namespace Celeste.Mod.TASHelper.Entities;

public static class Messenger {

    [Initialize]

    public static void Initialize() {
        if (ModUtils.PandorasBoxInstalled) {
            Type entityActivatorType = ModUtils.GetType("PandorasBox", "Celeste.Mod.PandorasBox.EntityActivator");
            entityActivatorType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(EntityData), typeof(Vector2) }, null).IlHook((cursor, _) => {
                if (cursor.TryGotoNext(MoveType.Before, ins => ins.OpCode == OpCodes.Ret)) {
                    cursor.Emit(OpCodes.Ldarg_1);
                    cursor.EmitDelegate(WatchEntityActivator);
                }
            });
        }
    }

    private static void WatchEntityActivator(EntityData data) {
        if (Engine.Scene is Level level) {
            // ctor of EntityActivator can only be called in Level.LoadLevel, so the scene must be a level
            EntityActivatorWarner watcher = new EntityActivatorWarner();
            level.Add(watcher);
            watcher.Watch(data);
        }
    }

}

[Tracked(false)]

public class WindSpeedRenderer : Message {

    public static WindSpeedRenderer Instance;
    public WindSpeedRenderer() : base("", new Vector2(10f, 10f)) {
        this.Depth = -20000;
        this.Visible = true;
        this.Active = true;
        base.Tag |= Tags.Global;
    }

    [LoadLevel]
    public static void AddIfNecessary(Level level) {
        if (Instance is null || !level.Entities.Contains(Instance)) {
            Instance = new();
            level.Add(Instance);
        }
    }

    public override void Update() {
        if (Engine.Scene is Level level && level.Wind != Vector2.Zero) {
            lifetimer = maxLifetimer;
            alpha = 1f;
        }
        else if (lifetimer > 0) {
            lifetimer--;
            if (lifetimer <= 0.5 * maxLifetimer) {
                alpha = (float)lifetimer / (0.5f * (float)maxLifetimer);
            }
        }
    }

    public override void Render() {
        if (TasHelperSettings.ShowWindSpeed && (DebugRendered || TasSettings.SimplifiedGraphics && TasSettings.SimplifiedBackdrop) && Engine.Scene is Level level && lifetimer > 0) {
            // when level.Transitioning, WindController stops working, but we render its speed anyway
            this.text = WindToString(level.Wind);

            float scale = 0.6f;
            Vector2 Size = FontSize.Measure(text) * scale;
            Monocle.Draw.Rect(Position - 10f * Vector2.UnitX, Size.X + 15f + Size.Y, Size.Y, Color.Black * alpha * 0.5f);

            RenderAtTopLeft(Position);

            if (level.Wind != Vector2.Zero) {
                Vector2 direction = level.Wind / level.Wind.Length();
                float unitLength = Size.Y * 0.4f * MathHelper.Clamp(level.Wind.Length() / 400f, 0.7f, 1f);
                Vector2 squareCenter = Position + (5f + Size.X) * Vector2.UnitX + Size.Y * 0.5f * Vector2.One;
                Vector2 head = squareCenter - direction * unitLength;
                Vector2 tail = squareCenter + direction * unitLength;
                Monocle.Draw.Line(head, tail, Color.White, 3f);
                Monocle.Draw.Line(tail, tail - direction.Rotate(ArrowAngle) * unitLength, Color.White, 2f);
                Monocle.Draw.Line(tail, tail - direction.Rotate(-ArrowAngle) * unitLength, Color.White, 2f);
                // alpha here must be 1, so no need to multiply it by alpha
            }
        }
    }

    private static float ArrowAngle = (float)Math.PI / 6f;

    private static int lifetimer = 0;

    private const int maxLifetimer = 10;

    public static string WindToString(Vector2 windMove) {
        return $"Wind Speed: {(windMove * 0.1f).ToDynamicFormattedString(2)} px/s";
    }

}

[Tracked(false)]
public class EntityActivatorWarner : Message {

    public static int MessageCount = 0;

    public static float lifetime = 5f;

    public float lifetimer = lifetime;

    [LoadLevel(true)]
    private static void OnLoadLevel() {
        MessageCount = 0;
    }
    public EntityActivatorWarner() : base("", new Vector2(960f, 20f)) {
        // hud renderer range: [0, 1920] * [0, 1080]
        this.Depth = -20000;
        this.Visible = false;
        this.Active = false;
        PauseUpdater.Register(this);
    }

    public void Watch(EntityData data) {
        // it seems there is some bug, if use Hashset<Types> targets = (hooked) EntityActivator.Targets, foreach (Type type in targets), text += type.ToString().
        // it seems type.ToString() may throw NullReferenceException, in SJ beginner lobby
        // wtf? does some type override ToString() in a bad way??

        // anyway, now that we use hook, why not just directly use EntityData

        string targets = data.Attr("targets", "").Replace(" ", "").Replace(",", "; ");
        if (string.IsNullOrWhiteSpace(targets)) {
            Visible = false;
            Active = false;
        }
        else {
            text = "EntityActivator Targets: " + targets;
            Visible = TasHelperSettings.EntityActivatorReminder;
            Active = true;
            lifetimer = lifetime;
            this.Position.Y += 30f * MessageCount;
            MessageCount++;
        }
    }

    public override void Removed(Scene scene) {
        MessageCount--;
        base.Removed(scene);
    }

    public override void Update() {
        Visible = TasHelperSettings.EntityActivatorReminder;
        lifetimer -= Engine.RawDeltaTime;
        if (lifetimer < 0) {
            Visible = false;
            RemoveSelf();
            return;
        }
        if (lifetimer / lifetime < 0.1f) {
            alpha = 10 * lifetimer / lifetime;
        }
        base.Update();
    }
}

[Tracked(false)]
public class HotkeyWatcher : Message {

    public static HotkeyWatcher Instance => Engine.Scene is Level level ? level.Tracker.GetEntity<HotkeyWatcher>() : null;

    public static float lifetime = 3f;

    public float lifetimer = 0f;
    public HotkeyWatcher() : base("", new Vector2(10f, 1060f)) {
        this.Depth = -20000;
        this.Visible = TasHelperSettings.HotkeyStateVisualize;
        base.Tag |= Tags.Global;
        PauseUpdater.Register(this);
    }

    public static bool AddIfNecessary() {
        if (Engine.Scene is not Level level) {
            return false;
        }
        if (level.Tracker.GetEntity<HotkeyWatcher>() is null) {
            level.Add(new HotkeyWatcher());
        }
        return true;
    }

    private void RefreshHotkeyDisabledImpl() {
        RestoreAlpha(this.text.Equals(text));
        text = "TAS Helper Disabled!";
        lifetimer = lifetime;
        Active = true;
        Visible = TasHelperSettings.HotkeyStateVisualize;
    }

    public static void RefreshHotkeyDisabled() {
        if (AddIfNecessary()) {
            Instance?.RefreshHotkeyDisabledImpl();
        }
    }

    private void RefreshMainSwitchImpl() {
        RestoreAlpha(false);
#pragma warning disable CS8524
        text = "TAS Helper Main Switch Mode " + (TasHelperSettings.MainSwitchThreeStates ? "[Off - Default - All]" : "[Off - All]") + " = " + (TasHelperSettings.MainSwitch switch { MainSwitchModes.Off => "Off", MainSwitchModes.OnlyDefault => "Default", MainSwitchModes.AllowAll => "All" });
#pragma warning restore CS8524
        lifetimer = lifetime;
        Active = true;
        Visible = TasHelperSettings.HotkeyStateVisualize;
    }

    public static void RefreshMainSwitch() {
        if (AddIfNecessary()) {
            Instance?.RefreshMainSwitchImpl();
        }
    }

    private void RefreshImpl(string text) {
        RestoreAlpha(this.text.Equals(text));
        this.text = text;
        lifetimer = lifetime;
        Active = true;
        Visible = TasHelperSettings.HotkeyStateVisualize;
    }

    public static void Refresh(string text) {
        if (AddIfNecessary()) {
            Instance?.RefreshImpl(text);
        }
    }

    private void RestoreAlpha(bool sameText) {
        if (sameText) {
            FallAndRise = true;
        }
        else {
            alpha = 1f;
        }
    }

    private bool FallAndRise = false;
    public override void Update() {
        if (FallAndRise) {
            alpha -= 0.1f;
            if (alpha < 0f) {
                alpha = 1f;
                FallAndRise = false;
            }
        }
        else {
            if (lifetimer / lifetime < 0.1f) {
                alpha = 10 * lifetimer / lifetime;
            }
            lifetimer -= Engine.RawDeltaTime;
            if (lifetimer < 0f) {
                lifetimer = 0f;
                Active = Visible = false;
            }
        }

        base.Update();
    }

    public override void Render() {
        float scale = 0.6f;
        Vector2 Size = FontSize.Measure(text) * scale;
        Monocle.Draw.Rect(Position - 0.5f * Size.Y * Vector2.UnitY - 10f * Vector2.UnitX, Size.X + 20f, Size.Y + 10f, Color.Black * alpha * 0.5f);
        Font.Draw(BaseSize, text, Position, new Vector2(0f, 0.5f), Vector2.One * scale, Color.White * alpha, 0f, Color.Transparent, 1f, Color.Black);
    }

}

[Tracked(false)]
public class Message : Entity {
    internal static readonly Language english = Dialog.Languages["english"];

    internal static readonly PixelFont Font = Fonts.Get(english.FontFace);

    internal static readonly float BaseSize = english.FontFaceSize;

    public static readonly PixelFontSize FontSize = Font.Get(BaseSize);

    public string text;

    public float alpha;

    public Message(string text, Vector2 Position) : base(Position) {
        base.Tag = Tags.HUD;
        this.text = text;
        alpha = 1f;
    }
    public override void Update() {
        base.Update();
    }

    public override void Render() {
        RenderAtCenter(Position);
    }

    public void RenderAtTopLeft(Vector2 Position) {
        Font.Draw(BaseSize, text, Position, new Vector2(0f, 0f), Vector2.One * 0.6f, Color.White * alpha, 0f, Color.Transparent, 1f, Color.Black);
    }

    public void RenderAtCenter(Vector2 Position) {
        Font.Draw(BaseSize, text, Position, new Vector2(0.5f, 0.5f), Vector2.One * 0.5f, Color.White * alpha, 0f, Color.Transparent, 1f, Color.Black);
    }

    public static void RenderMessage(string str, Vector2 Position, Vector2 scale) {
        RenderMessage(str, Position, Vector2.One * 0.5f, scale);
    }

    public static void RenderMessage(string str, Vector2 Position, Vector2 justify, Vector2 scale) {
        Font.DrawOutline(BaseSize, str, Position, justify, scale, Color.White, 2f, Color.Black);
    }
    public static void RenderMessage(string str, Vector2 Position, Vector2 justify, Vector2 scale, float stroke) {
        Font.DrawOutline(BaseSize, str, Position, justify, scale, Color.White, stroke, Color.Black);
    }

    public static void RenderMessage(string str, Vector2 Position, Vector2 justify, Vector2 scale, float stroke, Color colorInside, Color colorOutline) {
        Font.DrawOutline(BaseSize, str, Position, justify, scale, colorInside, stroke, colorOutline);
    }

    public static void RenderMessageJetBrainsMono(string str, Vector2 Position, Vector2 justify, Vector2 scale, float stroke, Color colorInside, Color colorOutline) {
        TAS.EverestInterop.InfoHUD.JetBrainsMonoFont.DrawOutline(str, Position, justify, scale, colorInside, stroke, colorOutline);
    }

    public static Vector2 Measure(string str) {
        return Font.Get(BaseSize).Measure(str);
    }
}