
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class PufferRenderer : AutoWatchText2Renderer {

    public Puffer puffer;

    public Vector2 lastPos;

    public Vector2 pos;

    private const bool allowZero = true;

    private const bool breakline = false;

    public ShakeRenderMode chooseOffsetVelocity;

    public static Vector2 default_offset = Vector2.UnitY * 3f;
    public PufferRenderer(RenderMode mode) : base(mode, active: true, preActive: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        puffer = entity as Puffer;
        text.scale = 1f;
        textBelow.scale = 0.8f;
        offset = default_offset;
        chooseOffsetVelocity = Config.Puffer_ChooseOffsetVelocity;
        // depth -1 if it's not Celeste LevelSet
    }

    public override void PreUpdateImpl() {
        text.Clear();
        textBelow.Clear();

        if (puffer.cannotHitTimer > 0f) {
            textBelow.Append($"cannotHit: {puffer.cannotHitTimer.ToFrameMinusOne()}");
        }
    }

    public override void UpdateImpl() {
        text.Position = puffer.Center;
        textBelow.Position = puffer.BottomCenter + offset;
        lastPos = pos;
        pos = puffer.Position + puffer.movementCounter;

        if (puffer.state == Puffer.States.Gone) {
            text.Append(puffer.goneTimer.ToFrameMinusOne());
            SetVisible();
            return;
        }

        if (puffer.cantExplodeTimer > 0f) {
            textBelow.Append($"cantExplode: {puffer.cantExplodeTimer.ToFrameMinusOne()}");
            // if ProximityExplodeCheck() in puffer's update (the large circle), then this timer is correct
            // if OnPlayer(Player player) in player's update (the little rect), then this timer is a bit off
        }

        switch (puffer.state) {
            case Puffer.States.Idle: {
                    switch (chooseOffsetVelocity) {
                        case ShakeRenderMode.None: break;
                        case ShakeRenderMode.Offset: {
                                textBelow.Append((pos - puffer.anchorPosition).OffsetToString(allowZero, breakline));
                                break;
                            }
                        case ShakeRenderMode.Velocity: {
                                textBelow.Append((pos - lastPos).Positon2ToSignedSpeed(allowZero, breakline));
                                break;
                            }
                        default: break;
                    }
                    break;
                }
            case Puffer.States.Hit: {
                    switch (chooseOffsetVelocity) {
                        case ShakeRenderMode.None: break;
                        case ShakeRenderMode.Offset: {
                                textBelow.Append((pos - lastPos).OffsetToString(allowZero, breakline));
                                break;
                            }
                        case ShakeRenderMode.Velocity: {
                                textBelow.Append((pos - lastPos).Positon2ToSignedSpeed(allowZero, breakline));
                                break;
                            }
                        default: break;
                    }
                    break;
                }
        }

        SetVisible();
    }

    public override void ClearHistoryData() {
        lastPos = pos = puffer.Position;
        chooseOffsetVelocity = Config.Puffer_ChooseOffsetVelocity;
    }
}

internal class PufferFactory : IRendererFactory {
    public Type GetTargetType() => typeof(Puffer);

    public bool Inherited() => true;
    public RenderMode Mode() => Config.Puffer;
    public void AddComponent(Entity entity) {
        entity.Add(new PufferRenderer(Mode()).SleepWhileFastForwarding());
    }
}




