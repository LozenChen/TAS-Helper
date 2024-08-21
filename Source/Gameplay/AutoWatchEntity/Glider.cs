﻿using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.AutoWatchEntity;


internal class GliderRenderer : AutoWatchTextRenderer {

    public Glider glider;

    public bool wasCannotHold = false;

    public GliderRenderer(RenderMode mode, bool active = true) : base(mode, active) { }

    public override void Added(Entity entity) {
        base.Added(entity);
        glider = entity as Glider;
        text.justify = new Vector2(0.5f, 1f);
    }

    public override void UpdateImpl() {
        text.Position = glider.TopCenter - Vector2.UnitY * 6f;
        text.Clear();
        text.Append(glider.Speed.Speed2ToSpeed2());

        if (glider.Hold.cannotHoldTimer > 0f) {
            text.Append($"cannotHold: {glider.Hold.cannotHoldTimer.ToFrame()}"); // rely on Depth = -5
            wasCannotHold = true;
        }
        else {
            if (wasCannotHold) {
                text.Append("cannotHold: 0");
            }
            wasCannotHold = false;
        }
        if (glider.Hold.Holder is { } player) {
            if (player.minHoldTimer > 0f) {
                text.Append($"minHoldTimer: {player.minHoldTimer.ToFrameMinusOne()}");
            }
            if (player.StateMachine.State == 8 && player.gliderBoostTimer > 0f && Config.ShowPlayerGliderBoostTimer) {
                // techinically glider boost timer is associated to the player
                // but we show it on the glider so it has enough room to render
                text.Append($"gliderBoostTimer: {player.gliderBoostTimer.ToFrameMinusOne()}");
            }
        }
        SetVisible();
    }
}

internal class GliderFactory : IRendererFactory {
    public Type GetTargetType() => typeof(Glider);


    public bool Inherited() => true;
    public RenderMode Mode() => Config.Glider;
    public void AddComponent(Entity entity) {
        entity.Add(new GliderRenderer(Mode()));
    }
}





