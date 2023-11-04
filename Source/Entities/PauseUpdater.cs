using Monocle;

namespace Celeste.Mod.TASHelper.Entities;

public static class PauseUpdater {
    // call entities updates when it's not called, e.g. by CelesteTAS pause, SkippingCutscene... which can not be set via Entity Tags like Tag.FrozenUpdater
    internal static List<Entity> entities = new();
    private static readonly List<Entity> toRemove = new();
    private static bool updated = false;
    private static int levelPauseTags = Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate;

    [Load]
    public static void Load() {
        On.Celeste.Level.LoadLevel += OnLoadLevel;
        On.Celeste.Level.BeforeRender += OnBeforeRender;
    }

    [Unload]

    public static void Unload() {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
        On.Celeste.Level.BeforeRender -= OnBeforeRender;
    }

    public static void Register(Entity entity) {
        entity.Tag |= levelPauseTags;
        entities.Add(entity);
    }

    public static void Remove(Entity entity) {
        entities.Remove(entity);
    }

    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        entities.Clear();
        orig(level, playerIntro, isFromLoader);
        Detector.AddIfNecessary(level);
    }

    private static void OnBeforeRender(On.Celeste.Level.orig_BeforeRender orig, Level level) {
        if (!updated) {
            foreach (Entity entity in entities) {
                if (entity.Scene != level) {
                    toRemove.Add(entity);
                }
            }
            foreach (Entity entity in toRemove) {
                entities.Remove(entity);
            }
            toRemove.Clear();
            foreach (Entity entity in entities) {
                if (entity.Active) {
                    entity.Update();
                }
            }
        }
        else {
            updated = false;
        }
        orig(level);
    }

    private class Detector : Entity {

        public static Detector instance;
        public Detector() {
            base.Tag = levelPauseTags | Tags.Persistent;
            instance = this;
            Depth = -100;
        }

        public override void Update() {
            if (!Predictor.Core.InPredict) {
                updated = true;
            }
        }

        public static void AddIfNecessary(Scene scene) {
            if (instance is null || instance.Scene != scene) {
                scene.Add(new Detector());
            }
        }
    }

}
