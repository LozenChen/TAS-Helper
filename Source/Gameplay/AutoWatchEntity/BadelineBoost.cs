
using Celeste.Mod.TASHelper.Utils;
using Monocle;
using System.Collections;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class BadelineBoostRenderer : AutoWatchTextRenderer {

    public BadelineBoost badeline;

    public Coroutine coroutine;

    public IEnumerator boostRoutine;

    public bool wasWaiting = false;

    public bool waitingForCoroutine = true;

    public float waitTimer => coroutine.waitTimer;

    public float loopTimer => boostRoutine?.GetFieldValue<float>("<p>5__9") ?? -9999f;

    public static bool useFallBack = true;
    public BadelineBoostRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        badeline = entity as BadelineBoost;
        bool found = false;
        boostRoutine = null;
        waitingForCoroutine = true;
        foreach (Component c in badeline.Components) {
            if (c is not Coroutine cor) {
                continue;
            }
            coroutine = cor;
            waitingForCoroutine = false;
            if (cor.enumerators.FirstOrDefault(x => x.GetType().Name.StartsWith("<BoostRoutine>d__")) is { } func) {
                // great if it matches well
                if (func.GetType().FullName == "Celeste.BadelineBoost+<BoostRoutine>d__22") {
                    boostRoutine = func;
                }
                found = true;
                break;
            }
        }
        if (!found && !waitingForCoroutine) {
            if (useFallBack) {
                found = true;
            }
            else {
                // if it has coroutine but that does not match (and we don't support fallback), remove it
                // the boost routine is added when it's on player, so it's likely that the badeline boost does not have a coroutine when loaded
                RemoveSelf();
                return;
            }
        }
    }

    public override void UpdateImpl() {
        if (badeline.holding is null) {
            Visible = false;
            return;
        }
        if (waitingForCoroutine || coroutine.Entity != badeline) { // the coroutine may be added several times
            waitingForCoroutine = true;
            boostRoutine = null;
            Visible = false;
            bool found = false;
            foreach (Component c in badeline.Components) {
                if (c is not Coroutine cor) {
                    continue;
                }
                coroutine = cor;
                waitingForCoroutine = false;
                if (cor.enumerators.FirstOrDefault(x => x.GetType().Name.StartsWith("<BoostRoutine>d__")) is { } func) {
                    if (func.GetType().FullName == "Celeste.BadelineBoost+<BoostRoutine>d__22") {
                        boostRoutine = func;
                    }
                    found = true;
                    break;
                }
            }
            if (!found && !waitingForCoroutine) {
                if (useFallBack) {
                    found = true;
                }
                else {
                    waitingForCoroutine = true;
                    coroutine = null;
                }
            }
            if (!found) {
                return;
            }
        }

        text.Position = badeline.Center;
        bool flag = false;
        text.Clear();
        if (coroutine.Active) {
            if (waitTimer > 0f) {
                text.Append(waitTimer.ToFrame());
                flag = true;
            }
            else if (loopTimer >= 0f && loopTimer < 1f) {
                int remainLoop = (int)Math.Ceiling((1f - loopTimer) / (Engine.DeltaTime / 0.2f)) - 1;
                text.Append(remainLoop.ToString());
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

        wasWaiting = flag;

        SetVisible();
    }
}

internal class BadelineBoostFactory : IRendererFactory {
    public Type GetTargetType() => typeof(BadelineBoost);

    public bool Inherited() => true;
    public RenderMode Mode() => Config.BadelineBoost;
    public void AddComponent(Entity entity) {
        entity.Add(new BadelineBoostRenderer(Mode()));
    }
}





