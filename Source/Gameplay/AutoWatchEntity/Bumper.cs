
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class BumperRenderer : AutoWatchTextRenderer {

    public Bumper bumper;

    public Vector2 lastPos;

    public Vector2 pos;

    public SlowMovingRenderMode chooseOffsetVelocity;
    public BumperRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        bumper = entity as Bumper;
        chooseOffsetVelocity = Config.Bumper_ChooseOffsetVelocity;
    }

    public override void UpdateImpl() {
        text.Position = bumper.Center;
        lastPos = pos;
        pos = bumper.Position;
        text.Clear();

        switch (chooseOffsetVelocity) {
            case SlowMovingRenderMode.None: break;
            case SlowMovingRenderMode.Offset: {
                    text.Append(new Vector2((float)(bumper.sine.Value * 3.0), (float)(bumper.sine.ValueOverTwo * 2.0)).OffsetToStringAllowZero());
                    break;
                }
            case SlowMovingRenderMode.Velocity: {
                    text.Append((pos - lastPos).Positon2ToSignedSpeedAllowZero());
                    break;
                }
            default: break;
        };

        text.Append(bumper.respawnTimer.ToFrameMinusOne()); // depth 0 so it depends on actualDepth...

        SetVisible();
    }

    public override void ClearHistoryData() {
        lastPos = pos = bumper.Position;
        chooseOffsetVelocity = Config.Bumper_ChooseOffsetVelocity;
    }
}

internal class BumperFactory : IRendererFactory {
    public Type GetTargetType() => typeof(Bumper);

    public bool Inherited() => true;
    public RenderMode Mode() => Config.Bumper;
    public void AddComponent(Entity entity) {
        entity.Add(new BumperRenderer(Mode()));
    }
}




