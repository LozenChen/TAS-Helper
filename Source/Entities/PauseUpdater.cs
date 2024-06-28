using Celeste.Mod.TASHelper.Utils;
using Monocle;

namespace Celeste.Mod.TASHelper.Entities;

public static class PauseUpdater {
    // call entities updates when it's not called, e.g. by CelesteTAS pause, SkippingCutscene... which can not be set via Entity Tags like Tag.FrozenUpdater

    private static bool updated = false;
    private static int levelPauseTags;

    [Initialize]

    public static void Initialize() {
        try {
            levelPauseTags = (int)Tags.FrozenUpdate | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
        }
        catch {
            // idk, but there's such bug report 
            // https://discord.com/channels/403698615446536203/1175568290303725669
            levelPauseTags = 0;
            Logger.Log(LogLevel.Warn, "TAS Helper", "An error occurred when PauseUpdater initializes!");
        }
    }

    [Load]
    public static void Load() {
        On.Celeste.Level.BeforeRender += OnBeforeRender;
    }


    [Unload]

    public static void Unload() {
        On.Celeste.Level.BeforeRender -= OnBeforeRender;
    }

    public static void Register(Entity entity) {
        entity.Tag |= levelPauseTags;
        entity.Add(new PauseUpdateComponent());
    }

    private static void OnBeforeRender(On.Celeste.Level.orig_BeforeRender orig, Level level) {
        if (!updated) {
            foreach (Entity entity in level.Tracker.GetComponents<PauseUpdateComponent>().Select(comp => comp.Entity)) {
                entity._PreUpdate();
                if (entity.Active) {
                    entity.Update();
                }
                entity._PostUpdate();
            }
        }
        else {
            updated = false;
        }
        orig(level);
    }

    [Tracked]
    private class Detector : Entity {

        public static Detector instance;
        public Detector() {
            base.Tag = levelPauseTags | Tags.Persistent;
            instance = this;
            Depth = -100;
        }

        public override void Update() {
            if (!Predictor.PredictorCore.InPredict) {
                updated = true;
            }
        }

        [LoadLevel]
        public static void AddIfNecessary(Level level) {
            if (instance is null || instance.Scene != level) {
                level.AddImmediately(new Detector());
            }
        }
    }

}

[Tracked]
public class PauseUpdateComponent : Component {
    public PauseUpdateComponent() : base(false, false) {

    }
}