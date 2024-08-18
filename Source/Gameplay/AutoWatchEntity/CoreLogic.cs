
using Celeste.Mod.TASHelper.Utils;
using Monocle;
using System.Reflection;
using TAS.EverestInterop;
using TAS.EverestInterop.InfoHUD;

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
            )
        );
        foreach (IRendererFactory factory in Factorys) {
            LevelExtensions.AddToTracker(factory.GetTargetType(), factory.Inherited());
            // Logger.Log(LogLevel.Debug, "TAS Helper", $"{factory.GetTargetType()}, inherited: {factory.Inherited()}");
        }

        typeof(InfoWatchEntity).GetMethodInfo("AddOrRemoveWatching").HookAfter<Entity>(OnSingleWatchEntityChange);
        typeof(InfoWatchEntity).GetMethodInfo("ClearWatchEntities").HookAfter(OnRemoveAllWatchEntity);
    }


    public static bool IsWatched(Entity entity) {
        return InfoWatchEntity.WatchingEntities.Contains(entity) || (entity.GetEntityData() is EntityData entityData && InfoWatchEntity.RequireWatchUniqueEntityIds.Contains(new UniqueEntityId(entity, entityData)));
    }

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
                foreach (Entity entity in entities) {
                    if (entity.Components.FirstOrDefault(c => c is AutoWatchRenderer) is null) {
                        factory.TryAddComponent(entity);
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
        if (InfoWatchEntity.RequireWatchEntities.IsNotEmpty()) {
            InfoWatchEntity.RequireWatchEntities.Where(reference => reference.IsAlive).ToList().ForEach(
                reference => {
                    Entity entity = (Entity)reference.Target;
                    InfoWatchEntity.WatchingEntities.Add(entity);
                }
            );
        }

        if (InfoWatchEntity.RequireWatchUniqueEntityIds.IsNotEmpty()) {
            Dictionary<UniqueEntityId, Entity> matchEntities = InfoWatchEntity.GetMatchEntities(level);
            if (matchEntities.IsNotEmpty()) {
                matchEntities.Values.ToList().ForEach(entity => {
                    InfoWatchEntity.WatchingEntities.Add(entity);
                });
            }
        }
    }

    public static void OnConfigChange() {
        if (Engine.Scene is not Level level) {
            return;
        }
        if (TasHelperSettings.AutoWatchEnable) {
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
}

[Tracked(true)]
internal class AutoWatchRenderer : Component {

    public RenderMode mode;

    public bool hasUpdate;

    public bool PostActive;
    public AutoWatchRenderer(RenderMode mode, bool hasUpdate = false) : base(false, visible: true) {
        this.mode = mode;
        PostActive = this.hasUpdate = hasUpdate;
    }

    public void WhenWatched_UpdateOnListener() {
        Visible = CoreLogic.IsWatched(this.Entity);
        PostActive = hasUpdate && Visible;
        if (PostActive) {
            ClearHistoryData();
            UpdateImpl();
        }
    }

    public override void Added(Entity entity) {
        base.Added(entity);
        if (mode == RenderMode.WhenWatched) {
            CoreLogic.WhenWatchedRenderers.Add(this);
            Visible = CoreLogic.IsWatched(entity);
            PostActive = hasUpdate && Visible;
        }
        entity.PostUpdate += this.UpdateWrapper;
        // move it here so we don't need to worry about OoO
    }

    public override void Removed(Entity entity) {
        entity.PostUpdate -= this.UpdateWrapper;
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

    public virtual void UpdateOnTransition() { } // for some persistent entity. in case some field just get lost

    public virtual void ClearHistoryData() { }

    public override void DebugRender(Camera camera) {
        DebugRenderImpl();
    }

    public virtual void DebugRenderImpl() { }
}

internal interface IRendererFactory {
    public Type GetTargetType();

    public bool Inherited();
    public RenderMode Mode();
    public bool TryAddComponent(Entity entity);

}

public enum RenderMode {
    Never,
    WhenWatched,
    Always
}





