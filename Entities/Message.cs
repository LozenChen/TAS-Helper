using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mod.TASHelper.Entities;

public static class Messenger {
    public static void Load() {
    }

    public static void Unload() {
    }

    public static void Initialize() {
        if (ModUtils.PandorasBoxInstalled) {
            PandorasBoxPatch();
        }
    }

    private static Type? EntityActivatorType = typeof(Entity);

    private static void HelloWorld(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader = false) { 
        orig(level, playerIntro, isFromLoader);
        level.Add(new Message("Hello\nWorld",Vector2.Zero));
    }

    private static void PandorasBoxPatch() {
        EntityActivatorType = ModUtils.GetType("PandorasBox", "Celeste.Mod.PandorasBox.EntityActivator");
        EntityActivatorType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[]{ typeof(EntityData), typeof(Vector2) }, null).IlHook((cursor, _) => {
            if (cursor.TryGotoNext(MoveType.Before,ins => ins.OpCode == OpCodes.Ret)) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Entity>>(CreateEntityActivatorWarner);
            }
        });
    }

    private static void CreateEntityActivatorWarner(Entity activator) {
           PlayerHelper.scene.Add(new EntityActivatorWarner(activator));
    }

    public class EntityActivatorWarner: Message {

        public static float lifetime = 5f;

        public float lifetimer = lifetime;
        public EntityActivatorWarner(Entity activator) : base("", new Vector2(960f, 20f)) {
            // hud renderer range: [0, 1920] * [0, 1080]
            Visible = TasHelperSettings.EntityActivatorReminder;
            this.Depth = -20000;
            HashSet<Type> Targets = (HashSet<Type>)EntityActivatorType.GetFieldInfo("Targets").GetValue(activator);
            if (Targets.Count != 0) {
                text = "EntityActivator Targets: ";
                foreach (Type type in Targets) {
                    text += type.ToString() + "; ";
                }
            }
            else {
                Visible = false;
            }
        }

        public override void Update() {
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
}

public class Message : Entity {
    private static readonly Language english = Dialog.Languages["english"];

    private static readonly PixelFont Font = Fonts.Get(english.FontFace);

    private static readonly float BaseSize = english.FontFaceSize;

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
        Vector2 position = (base.Scene as Level).Camera.Position;
        Vector2 vector = position + new Vector2(160f, 90f);
        Vector2 position2 = (Position - position + (Position - vector) * 0.2f) * 6f;
        RenderAt(position2);
    }

    public void RenderAt(Vector2 Position) {
        Font.Draw(BaseSize, text, Position, new Vector2(0.5f, 0.5f), Vector2.One * 0.5f, Color.White * alpha, 0f, Color.Transparent, 0f, Color.Transparent);
    }
}