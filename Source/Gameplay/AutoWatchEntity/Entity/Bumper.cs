
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class BumperRenderer : AutoWatchTextRenderer {

    public Bumper bumper;

    public Vector2 lastPos;

    public Vector2 pos;

    public ShakeRenderMode chooseOffsetVelocity;

    private const bool allowZero = true;

    private const bool breakline = true;
    public BumperRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        bumper = entity as Bumper;
        chooseOffsetVelocity = Config.Bumper_ChooseOffsetVelocity;
        text.scale = 0.8f;
    }

    public override void UpdateImpl() {
        text.Position = bumper.Center;
        lastPos = pos;
        pos = bumper.Position;
        text.Clear();

        switch (chooseOffsetVelocity) {
            case ShakeRenderMode.None: break;
            case ShakeRenderMode.Offset: {
                    // it would be a bit weird if it has node
                    text.Append((bumper.Position - bumper.anchor).OffsetToString(allowZero, breakline)); // we can use bumper.sine.Value/ValueOverTwo here, but for mod compatibility, we use this
                    break;
                }
            case ShakeRenderMode.Velocity: {
                    text.Append((pos - lastPos).Positon2ToSignedSpeed(allowZero, breakline));
                    break;
                }
            default: break;
        }

        if (!bumper.fireMode) {
            text.Append(bumper.respawnTimer.ToFrameMinusOne()); // depth 0 so it depends on actualDepth...
        }
        
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
        entity.Add(new BumperRenderer(Mode()).SleepWhileFastForwarding());
    }
}




