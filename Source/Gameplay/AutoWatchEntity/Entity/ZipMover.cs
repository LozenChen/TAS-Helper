
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class ZipMoverRenderer : AutoWatchTextRenderer {

    public ZipMover zipMover;

    public Vector2 lastPos;

    public Vector2 pos;


    public Coroutine coroutine;

    public bool wasWaiting = false;

    public float waitTimer => coroutine.waitTimer;

    public static bool useFallBack = true;
    public ZipMoverRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        zipMover = entity as ZipMover;
        bool found = false;
        bool hasCoroutine = false;
        foreach (Component c in zipMover.Components) {
            if (c is not Coroutine cor) {
                continue;
            }
            coroutine = cor;
            hasCoroutine = true;
            if (cor.enumerators.FirstOrDefault(x => x.GetType().Name.StartsWith("<Sequence>d__")) is not null) {
                // great if it matches well
                found = true;
                break;
            }
        }
        if (!found && hasCoroutine && useFallBack) {
            found = true;
        }

        if (!found) {
            // a zipmover should always have a coroutine when loaded, so we remove it immediately if nothing is found
            RemoveSelf();
        }
    }

    public override void UpdateImpl() {
        text.Position = zipMover.Center;
        lastPos = pos;
        pos = zipMover.Position + zipMover.movementCounter;
        bool flag = false;
        text.Clear();
        if (pos != lastPos) {
            text.Append((pos - lastPos).PositionToAbsoluteSpeed());
        }
        else if (coroutine is not null) {
            if (coroutine.Active) {
                if (waitTimer > 0f) {
                    text.Append(waitTimer.ToFrame());
                    flag = true;
                }
                else if (coroutine.Current.GetType().FullName == "Monocle.Tween+<Wait>d__45" && coroutine.Current.GetFieldValue("<>4__this") is Tween tween) {
                    text.Append((tween.TimeLeft.ToFrameData() + 1).ToString());
                    flag = true;
                }
                else if (!wasWaiting) {
                    text.Append("~");
                }
            }

            if (!flag && wasWaiting) {
                text.Append("0");
            }
        }

        wasWaiting = flag;

        SetVisible();
    }

    public override void ClearHistoryData() {
        lastPos = pos = zipMover.Position;
    }
}

internal class ZipMoverFactory : IRendererFactory {
    public Type GetTargetType() => typeof(ZipMover);

    public bool Inherited() => true;
    public RenderMode Mode() => Config.ZipMover;
    public void AddComponent(Entity entity) {
        entity.Add(new ZipMoverRenderer(Mode()).SleepWhileFastForwarding());
    }
}




