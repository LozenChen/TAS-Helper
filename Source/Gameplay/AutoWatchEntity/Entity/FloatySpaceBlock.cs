
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;

internal class FloatySpaceBlockRenderer : AutoWatchText2Renderer {

    private static bool ShowDetailedInfo => Config.FloatySpaceBlock_ShowDetails;

    public FloatySpaceBlock block;

    public Vector2 lastPos;

    public Vector2 pos;

    public bool useOffsetInsteadOfVelocity = true;

    private const bool allowZero = false;

    private const bool breakline = true;

    private static Vector2 textBelowOffset = Vector2.UnitY * 12f;
    public FloatySpaceBlockRenderer(RenderMode mode) : base(mode, active: true) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        lastPos = pos = entity.Position;
        block = entity as FloatySpaceBlock;
        useOffsetInsteadOfVelocity = Config.FloatySpaceBlock_UseOffsetInsteadOfVelocity;
        textBelow.scale = 0.5f;
    }

    public override void UpdateImpl() {
        lastPos = pos;
        pos = block.Position + block.movementCounter;
        if (block.MasterOfGroup || mode == RenderMode.WhenWatched) { // if RenderMode = Always, then we only render the master one
            if (useOffsetInsteadOfVelocity) {
                if (block.MasterOfGroup) {
                    text.content = (pos - block.Moves[block]).OffsetToString(allowZero, breakline);
                }
                else {
                    text.content = (pos - block.master.Moves[block]).OffsetToString(allowZero, breakline);
                }
            }
            else {
                text.content = (pos - lastPos).Positon2ToSignedSpeed(allowZero, breakline);
            }
            Visible = true;
            text.Position = block.Center;
            if (ShowDetailedInfo) {
                if (block.Height > 32f) {
                    textBelow.Position = text.Position + textBelowOffset;
                }
                else {
                    textBelow.Position = block.BottomCenter;
                }
                FloatySpaceBlock master = block.master ?? block;
                Vector2 dash = Calc.YoYo(Ease.QuadIn(master.dashEase)) * master.dashDirection * 8f;
                textBelow.content = $"sineWave_Y: {(4f * MathF.Sin(master.sineWave)).SignedFloatToString()}\nsink_Y: {12f * Ease.SineInOut(master.yLerp):0.00}\ndash_X: {(dash.X).SignedFloatToString()}\ndash_Y: {(dash.Y).SignedFloatToString()}";
            }
            else {
                textBelow.content = "";
            }
        }
        else {
            Visible = false;
        }
    }

    public override void ClearHistoryData() {
        lastPos = pos = block.Position;
        useOffsetInsteadOfVelocity = Config.FloatySpaceBlock_UseOffsetInsteadOfVelocity;
    }
}

internal class FloatySpaceBlockFactory : IRendererFactory {
    public Type GetTargetType() => typeof(FloatySpaceBlock);

    public bool Inherited() => false;
    public RenderMode Mode() => Config.FloatySpaceBlock;
    public void AddComponent(Entity entity) {
        entity.Add(new FloatySpaceBlockRenderer(Mode()).SleepWhileFastForwarding());
    }
}




