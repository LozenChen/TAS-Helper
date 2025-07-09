
using Celeste.Mod.TASHelper.Utils;
using Monocle;
using TAS.InfoHUD;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal static class CoreLogic {

    public static List<IRendererFactory> Factorys = new List<IRendererFactory>();

    public static List<AutoWatchRenderer> WhenWatchedRenderers = new List<AutoWatchRenderer>();

    [Initialize]
    private static void Initialize() {
        Factorys.AddRange(
            typeof(CoreLogic).Assembly.GetTypesSafe().Where(
                type => type.GetInterface(nameof(IRendererFactory)) is not null
            ).Select(
                type => (IRendererFactory)type.GetConstructorInfo().Invoke(parameterless)
            ).Where(
                factory => factory.GetTargetType() is not null
            )
        );
        foreach (IRendererFactory factory in Factorys) {
            Tracker.AddTypeToTracker(factory.GetTargetType(), null, factory.Inherited());
            // Logger.Log(LogLevel.Debug, "TAS Helper", $"{factory.GetTargetType()}, inherited: {factory.Inherited()}");
        }

        InfoWatchEntity.StartWatching += OnSingleWatchEntityChange;
        InfoWatchEntity.StopWatching += OnSingleWatchEntityChange;
        InfoWatchEntity.ClearWatching += OnRemoveAllWatchEntity;
    }

    [Unload]
    private static void Unload() {
        InfoWatchEntity.StartWatching -= OnSingleWatchEntityChange;
        InfoWatchEntity.StopWatching -= OnSingleWatchEntityChange;
        InfoWatchEntity.ClearWatching -= OnRemoveAllWatchEntity;
    }


    public static bool IsWatching(Entity entity) => InfoWatchEntity.IsWatching(entity);

    public static void OnSingleWatchEntityChange(Entity entity) {
        if (entity.Components.FirstOrDefault(c => c is AutoWatchRenderer) is AutoWatchRenderer renderer && renderer.mode == RenderMode.WhenWatched) {
            renderer.WhenWatched_UpdateOnListener();
        }
    }

    public static void OnRemoveAllWatchEntity() {
        foreach (AutoWatchRenderer renderer in WhenWatchedRenderers) {
            renderer.WhenWatched_UpdateOnListener();
        }
    }

    public static void AddRenderersToLevel(Level level) {
        foreach (IRendererFactory factory in Factorys) {
            if (factory.Mode() == RenderMode.Never) {
                if (level.Tracker.Entities.TryGetValue(factory.GetTargetType(), out List<Entity> entities2)) {
                    foreach (Entity entity in entities2) {
                        if (entity.Components.FirstOrDefault(c => c is AutoWatchRenderer) is { } component) {
                            entity.Remove(component);
                        }
                    }
                }
            }
            else if (level.Tracker.Entities.TryGetValue(factory.GetTargetType(), out List<Entity> entities)) {
                Type type = factory.GetTargetType();
                bool inherited = factory.Inherited();
                RenderMode mode = factory.Mode();

                foreach (Entity entity in entities.Where(x => inherited || x.GetType() == type)) {
                    // if not inherited, then we nned to filter out those entities which use "TrackedAs" (e.g. HonlyHelper.RisingBlock)
                    if (entity.Components.FirstOrDefault(c => c is AutoWatchRenderer) is AutoWatchRenderer renderer) {
                        renderer.mode = mode;
                        renderer.UpdateOn_ConfigChange_Or_StopUltraforwarding_Or_Clone();
                    }
                    else {
                        factory.AddComponent(entity);
                    }
                }
            }
        }
    }

    [LoadLevel]
    public static void OnLoadLevel(Level level) {
        if (Config.MainEnabled) {
            List<AutoWatchRenderer> longliveRenderers = new List<AutoWatchRenderer>();
            foreach (AutoWatchRenderer renderer in WhenWatchedRenderers) {
                if (renderer.Entity?.Scene == level) {
                    longliveRenderers.Add(renderer);
                }
            }
            WhenWatchedRenderers.Clear();
            WhenWatchedRenderers.AddRange(longliveRenderers);
            FakeGetInfo(level); // invoke getinfo so the data in RequireWatchEntities go into WatchingEntities, and our Visiblity can be set properly
            AddRenderersToLevel(level);
        }
        else {
            ClearRenderers(level);
        }
    }


    private static void FakeGetInfo(Level level) {
        // basically same as InfoWatchEntity.GetInfo
        // but remove some restrictions, so it updates even when we close the in-game info hud

        // todo: try refactor? currently it's a bit hacky
        // we need this, when studio & info panel are both disabled
    }

    public static void OnConfigChange() {
        InfoWatchEntity.ForceUpdateInfo = Config.MainEnabled;
        // when this gets removed, check that:
        // 1) works when both disabled
        // 2) if we can click the hidden triggers

        if (Engine.Scene is not Level level) {
            return;
        }
        if (Config.MainEnabled) {
            AddRenderersToLevel(level);
        }
        else {
            ClearRenderers(level);
        }
    }
    public static void ClearRenderers(Level level) {
        List<Entity> list = level.Tracker.GetComponents<AutoWatchRenderer>().Select(x => x.Entity).ToList(); // to avoid CollectionModification when enumerating
        foreach (Entity entity in list) {
            if (entity.Components.FirstOrDefault(c => c is AutoWatchRenderer) is { } component) {
                entity.Remove(component);
            }
        }
        WhenWatchedRenderers.Clear();
    }


    private static bool wasFastForwarding = false;

    [SceneAfterUpdate]
    private static void PatchAfterUpdate(Scene self) {
        if (self is Level) {
            if (FastForwarding) {
                wasFastForwarding = true;
            }
            else {
                if (wasFastForwarding) {
                    WakeUpAllAutoWatchRenderer();
                }
                wasFastForwarding = false;
            }
        }
    }

    private static void WakeUpAllAutoWatchRenderer() {
        if (Engine.Scene is { } self) {
            foreach (AutoWatchRenderer renderer in self.Tracker.GetComponents<AutoWatchRenderer>()) {
                renderer.UpdateOn_ConfigChange_Or_StopUltraforwarding_Or_Clone();
            }
        }
    }

    internal static void EverythingOnClone() {
        HiresLevelRenderer.RemoveRenderers<SwitchGateRenderer.SwitchLinker>();
        if (Config.MainEnabled) {
            WakeUpAllAutoWatchRenderer();
        }
        else if (Engine.Scene is Level level) {
            ClearRenderers(level);
        }
    }
}

[Tracked(true)]
internal class AutoWatchRenderer : Component {

    public RenderMode mode;

    public bool hasUpdate;

    public bool hasPreUpdate;

    public bool PreActive;

    public bool PostActive;

    public new bool Active { // hide the original "Active" field
        get {
            throw new Exception("Use Pre/PostActive Instead!");
        }
        private set {
            throw new Exception("Use Pre/PostActive Instead!");
        }
    }
    public AutoWatchRenderer(RenderMode mode, bool hasUpdate = true, bool hasPreUpdate = false) : base(false, visible: true) {
        // the component itself doesn't update (so active = false), but pass its "update" to entity.Pre/PostUpdate (to avoid some OoO issue)
        this.mode = mode;
        PostActive = this.hasUpdate = hasUpdate;
        PreActive = this.hasPreUpdate = hasPreUpdate;
    }

    public void WhenWatched_UpdateOnListener() {
        Visible = CoreLogic.IsWatching(this.Entity);
        PostActive = hasUpdate && Visible;
        PreActive = hasPreUpdate && Visible;
        ClearHistoryData();
        if (PreActive) {
            PreUpdateImpl();
        }
        if (PostActive) {
            UpdateImpl();
        }
    }

    public void UpdateOn_ConfigChange_Or_StopUltraforwarding_Or_Clone() {
        Visible = mode == RenderMode.Always || CoreLogic.IsWatching(this.Entity);
        PostActive = hasUpdate && Visible;
        PreActive = hasPreUpdate && Visible;
        ClearHistoryData();
        OnClone();
    }

    public override void Added(Entity entity) {
        base.Added(entity);
        if (mode == RenderMode.WhenWatched) {
            // if it's not watched, the renderer is still there, but just hidden and inactive
            CoreLogic.WhenWatchedRenderers.Add(this);
            Visible = CoreLogic.IsWatching(entity);
            PostActive = hasUpdate && Visible;
            PreActive = hasPreUpdate && Visible;
        }
        if (hasUpdate) {
            entity.PostUpdate += this.UpdateWrapper;
            // move it here so we don't need to worry about OoO
            // updates even if the entity itself is not active
            // though this will not be called when level is transitioning, frozen, or paused
        }
        if (hasPreUpdate) {
            entity.PreUpdate += this.PreUpdateWrapper;
        }
    }

    public override void Removed(Entity entity) {
        if (hasUpdate) {
            entity.PostUpdate -= this.UpdateWrapper;
        }
        if (hasPreUpdate) {
            entity.PreUpdate -= this.PreUpdateWrapper;
        }
        base.Removed(entity);
        if (mode == RenderMode.WhenWatched) {
            CoreLogic.WhenWatchedRenderers.Remove(this);
        }
    }

    public override void EntityRemoved(Scene scene) {
        base.EntityRemoved(scene);
        if (mode == RenderMode.WhenWatched) {
            CoreLogic.WhenWatchedRenderers.Remove(this);
        }
    }

    private void UpdateWrapper(Entity entity) {
        if (PostActive) {
            UpdateImpl();
        }
    }

    public override sealed void Update() {
        // do nothing
    }

    public virtual void UpdateImpl() {
        // do real logic here
        // should atmost depend on the current state and the last-frame state of entity
        // will be a bit inaccurate when you just clicked the entity if we use the data of two frames
    }

    private void PreUpdateWrapper(Entity entity) {
        if (PreActive) {
            PreUpdateImpl();
        }
    }
    public virtual void PreUpdateImpl() { }

    public virtual void ClearHistoryData() { }

    public override void DebugRender(Camera camera) {
        if (Visible) {
            DebugRenderImpl();
        }
    }

    public virtual void DebugRenderImpl() { }

    public virtual void OnClone() { }

    public virtual void DelayedUpdatePosition() { }

    public AutoWatchRenderer SleepWhileFastForwarding() {
        if (FastForwarding) {
            Visible = false;
            PostActive = PreActive = false;
        }
        return this;
    }
}

internal interface IRendererFactory {
    public Type GetTargetType(); // make it null if it's from an unloaded mod

    public bool Inherited(); // if the entity does not have a "Tracked" attribute, then we can assign arbitrary bool value here
    // but if the entity has a "Tracked(false)", then we must assign false here. so we don't change game logic and thus avoid tas desync
    public RenderMode Mode();
    public void AddComponent(Entity entity);
}

public enum RenderMode {
    Never,
    WhenWatched,
    Always
}