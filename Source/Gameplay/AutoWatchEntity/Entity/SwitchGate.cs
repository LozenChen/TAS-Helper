
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class SwitchGateRenderer : AutoWatchTextRenderer {

    public SwitchGate gate;

    public bool Initialized = false;

    public List<SwitchLinker> SwitchLinkers;

    private static readonly List<SwitchLinker> toRemove = new();
    public class SwitchLinker : THRenderer {
        public Switch sw;

        public SwitchGateRenderer holder;

        public Vector2 from;

        public SwitchLinker(Switch sw, SwitchGateRenderer holder) {
            this.sw = sw;
            this.holder = holder;
            from = holder.gate.Center;
        }

        public void OnRemove() {
            HiresLevelRenderer.Remove(this);
        }

        public override void Render() {
            if (DebugRendered && holder.Visible) {
                Monocle.Draw.Line(from * 6f, sw.Entity.Position * 6f, LineColor, Thickness);
            }
        }

        public static Color LineColor = Color.Lime;

        public static float Thickness = 2f;
    }

    public Vector2 lastPos;

    public Vector2 pos;

    public Coroutine coroutine;
    public SwitchGateRenderer(RenderMode mode) : base(mode, true, false) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        gate = entity as SwitchGate;
        if (Switch.CheckLevelFlag(gate.SceneAs<Level>())) {
            RemoveSelf();
            return;
        }
        Initialized = false;
    }

    private void Initialize() {
        // we must wait until switches in last room get unloaded
        // so this cannot be in Added (or say, LoadLevel)
        SwitchLinkers = gate.Scene.Tracker.GetComponents<Switch>().Cast<Switch>().Where(x => !x.Activated).Select(x => new SwitchLinker(x, this)).ToList();
        foreach (SwitchLinker link in SwitchLinkers) {
            HiresLevelRenderer.AddIfNotPresent(link);
        }
        lastPos = pos = gate.Position;
        if (gate.FindCoroutineComponent("Celeste.SwitchGate+<Sequence>d__16", out Tuple<Coroutine, IEnumerator> tuple)) {
            coroutine = tuple.Item1;
        }
        else {
            coroutine = null;
        }
        Initialized = true;
    }
    private static bool InPosition(SwitchGate b) {
        return (b.Position + b.movementCounter) == b.node;
    }
    public override void UpdateImpl() {
        if (!Initialized) {
            Initialize();
        }
        foreach (SwitchLinker link in SwitchLinkers) {
            if (link.sw.Activated) {
                toRemove.Add(link);
            }
        }
        foreach (SwitchLinker link in toRemove) {
            SwitchLinkers.Remove(link);
            link.OnRemove();
        }
        toRemove.Clear();

        text.Position = gate.Center;
        lastPos = pos;
        pos = gate.Position + gate.movementCounter;
        text.Clear();
        if (pos != lastPos) {
            text.Append((pos - lastPos).PositionToAbsoluteSpeed());
        }
        else if (SwitchLinkers.IsNullOrEmpty()) {
            if (InPosition(gate)) {
                RemoveSelf();
                return;
            }
            else if (coroutine is not null) {
                float w = coroutine.waitTimer;
                if (w > 0f) {
                    if (w > 1.7f) {
                        text.Append("~");
                    }
                    else {
                        text.Append(coroutine.waitTimer.ToFrame());
                        wasWaiting = true;
                    }
                }
                else if (wasWaiting) {
                    text.Append("0");
                    wasWaiting = false;
                }
                else {
                    text.Append($"{gate.icon.Rate:0.00}/1.00");
                }
            }
        }
    }

    private bool wasWaiting = false;

    public override void Removed(Entity entity) {
        base.Removed(entity);
        if (SwitchLinkers.IsNotNullOrEmpty()) {
            foreach (SwitchLinker link in SwitchLinkers) {
                link.OnRemove();
            }
            SwitchLinkers = null;
        }
    }
    public override void EntityRemoved(Scene scene) {
        base.EntityRemoved(scene);
        if (SwitchLinkers.IsNotNullOrEmpty()) {
            foreach (SwitchLinker link in SwitchLinkers) {
                link.OnRemove();
            }
            SwitchLinkers = null;
        }
    }

    public override void ClearHistoryData() {
        base.ClearHistoryData();
        Initialized = false;
        if (SwitchLinkers.IsNotNullOrEmpty()) {
            foreach (SwitchLinker link in SwitchLinkers) {
                link.OnRemove();
            }
            SwitchLinkers = null;
        }
    }
}

internal class SwitchGateFactory : IRendererFactory {
    public Type GetTargetType() => typeof(SwitchGate);

    public bool Inherited() => true;
    public RenderMode Mode() => Config.SwitchGate;
    public void AddComponent(Entity entity) {
        entity.Add(new SwitchGateRenderer(Mode()).SleepWhileFastForwarding());
    }
}




