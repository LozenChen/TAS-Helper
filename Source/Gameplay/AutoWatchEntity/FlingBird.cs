using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class FlingBirdRenderer : AutoWatchTextRenderer {

    public FlingBird bird;

    public Coroutine coroutine;

    public bool wasWaiting = false;

    public float waitTimer => coroutine.waitTimer;

    public static bool useFallBack = false;

    private const float accelBeforeFlinging = 6000f;
    public FlingBirdRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        bird = entity as FlingBird;
        bool found = false;
        coroutine = null;
        foreach (Component c in bird.Components) {
            if (c is not Coroutine cor) {
                continue;
            }
            if (cor.enumerators.FirstOrDefault(x => x.GetType().Name.StartsWith("<DoFlingRoutine>d__")) is not null) {
                found = true;
                coroutine = cor;
                break;
            }
        }
    }

    public override void UpdateImpl() {
        Visible = false;
        if (bird.state != FlingBird.States.Fling || playerInstance is not Player player || player.flingBird != bird) {
            return;
        }
        if (coroutine is null || coroutine.Entity != bird) { // the coroutine may be added several times
            if (player.StateMachine.State != 24) {
                return;
            }
            bool found = false;
            foreach (Component c in bird.Components) {
                if (c is not Coroutine cor) {
                    continue;
                }
                if (cor.enumerators.FirstOrDefault(x => x.GetType().Name.StartsWith("<DoFlingRoutine>d__")) is not null) {
                    found = true;
                    coroutine = cor;
                    break;
                }
            }
            if (!found) {
                return;
            }
        }


        bool flag = false;
        text.Position = bird.Center;
        text.Clear();

        if (player.StateMachine.State != 24) {
            // the "last" frame of flinging
            // player.FinishFlingBird() is called but the coroutine is still not finished, so actually waitTimer > 0f here
            coroutine = null;
            text.Append("0");
        }
        else {
            if (coroutine.Active) {
                if (waitTimer > 0f) {
                    flag = true;
                    if (bird.flingAccel == accelBeforeFlinging) {
                        text.Append((waitTimer.ToFrameData() + 1).ToString());
                    }
                    else {
                        text.Append(waitTimer.ToFrame());
                    }
                }
                else if (!flag && wasWaiting) {
                    if (bird.flingAccel == accelBeforeFlinging) {
                        text.Append("1");
                    }
                    else {

                        text.Append("0");
                    }
                }
                else if (bird.flingAccel > 0f) {
                    // the first stage, slowing down
                    float deltaTime = Engine.RawDeltaTime * Engine.TimeRate * Engine.TimeRateB * Engine.GetTimeRateComponentMultiplier(bird.Scene); // the first frame use a different time rate so it sucks
                    text.Append((
                            (int)Math.Ceiling(
                                (bird.flingTargetSpeed - bird.flingSpeed).Length() / (bird.flingAccel * deltaTime)
                            )
                        ).ToString()
                    );
                }
            }
        }

        wasWaiting = flag;

        SetVisible();
    }
}

internal class FlingBirdFactory : IRendererFactory {
    public Type GetTargetType() => typeof(FlingBird);

    public bool Inherited() => false; // it's not compatible with DJMapHelper's FlingBirdReversed
    public RenderMode Mode() => Config.FlingBird;
    public void AddComponent(Entity entity) {
        entity.Add(new FlingBirdRenderer(Mode()).SleepWhenUltraFastforward());
    }
}





