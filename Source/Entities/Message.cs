using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System.Reflection;
using static Celeste.Mod.TASHelper.Module.TASHelperSettings;

namespace Celeste.Mod.TASHelper.Entities;

public static class Messenger {

    [Load]
    public static void Load() {
        On.Celeste.Level.LoadLevel += OnLoadLevel;
    }

    [Unload]

    public static void Unload() {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
    }

    [Initialize]

    public static void Initialize() {
        if (ModUtils.PandorasBoxInstalled) {
            PandorasBoxPatch();
        }
    }

    private static void HelloWorld(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        orig(level, playerIntro, isFromLoader);
        level.Add(new Message("Hello\nWorld", Vector2.Zero));
    }

    private static void PandorasBoxPatch() {
        Type EntityActivatorType = ModUtils.GetType("PandorasBox", "Celeste.Mod.PandorasBox.EntityActivator");
        EntityActivatorType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(EntityData), typeof(Vector2) }, null).IlHook((cursor, _) => {
            if (cursor.TryGotoNext(MoveType.Before, ins => ins.OpCode == OpCodes.Ret)) {
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate(WatchEntityActivator);
            }
        });
    }

    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        EntityActivatorWarner.MessageCount = 0;
        orig(level, playerIntro, isFromLoader);
        level.Add(new HotkeyWatcher());
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
public class EntityActivatorWarner : Message {

    public static int MessageCount = 0;

    public static float lifetime = 5f;

    public float lifetimer = lifetime;
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
            RemoveSelf();
            return;
        }
        if (lifetimer / lifetime < 0.1f) {
            alpha = 10 * lifetimer / lifetime;
        }
        base.Update();
    }
    public override void Render() {
        RenderAt(Position);
    }
}

[Tracked(false)]
public class HotkeyWatcher : Message {

    public static HotkeyWatcher instance;

    public static float lifetime = 3f;

    public float lifetimer = 0f;
    public HotkeyWatcher() : base("", new Vector2(10f, 1060f)) {
        this.Depth = -20000;
        this.Visible = TasHelperSettings.HotkeyStateVisualize;
        base.Tag |= Tags.Global;
        instance = this;
        PauseUpdater.Register(this);
    }

    public void RefreshHotkeyDisabled() {
        RestoreAlpha(this.text.Equals(text));
        text = "TAS Helper Disabled!";
        lifetimer = lifetime;
        Active = true;
        Visible = TasHelperSettings.HotkeyStateVisualize;
    }

    public void RefreshMainSwitch() {
        RestoreAlpha(false);
#pragma warning disable CS8524
        text = "TAS Helper Main Switch Mode " + (TasHelperSettings.MainSwitchThreeStates ? "[Off - Default - All]" : "[Off - All]") + " = " + (TasHelperSettings.MainSwitch switch { MainSwitchModes.Off => "Off", MainSwitchModes.OnlyDefault => "Default", MainSwitchModes.AllowAll => "All" });
#pragma warning restore CS8524
        lifetimer = lifetime;
        Active = true;
        Visible = TasHelperSettings.HotkeyStateVisualize;
    }

    public void Refresh(string text) {
        RestoreAlpha(this.text.Equals(text));
        this.text = text;
        lifetimer = lifetime;
        Active = true;
        Visible = TasHelperSettings.HotkeyStateVisualize;
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
            alpha = alpha - 0.1f;
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
        Font.Draw(BaseSize, text, Position, new Vector2(0f, 0.5f), Vector2.One * 0.5f, Color.White * alpha, 0f, Color.Transparent, 1f, Color.Black);
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
        RenderAt(Position);
    }

    public void RenderAt(Vector2 Position) {
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
}