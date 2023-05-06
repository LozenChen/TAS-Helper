using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Entities;

internal static class PixelGridHook {
    public static void Load() {
        On.Celeste.Level.LoadLevel += CreatePixelGridAroundPlayer;
    }
    public static void Unload() {
        On.Celeste.Level.LoadLevel -= CreatePixelGridAroundPlayer;
    }
    private static void PixelGridAroundPlayerUpdate(PixelGrid self) {
        if (PlayerHelper.player is Player player) {
            self.Position = player.Position;
            self.Collider.Width = player.Collider.Width;
            self.Collider.Height = player.Collider.Height;
            self.Collider.Left = player.Collider.Left;
            self.Collider.Top = player.Collider.Top;
            // when use self.Collider = player.Collider, and turn off Celeste TAS's ShowHitboxes,
            // if you demodash into wall, then player will stuck in wall
            // don't know why
        }
    }
    private static void CreatePixelGridAroundPlayer(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        orig(self, playerIntro, isFromLoader);
        self.Add(new PixelGrid(() => TasHelperSettings.EnablePixelGrid, () => TasHelperSettings.PixelGridWidth, PixelGridAroundPlayerUpdate, false));
    }

}

[Tracked(false)]
public class PixelGrid : Entity {
    public static Color color1 = Color.White;
    public static Color color2 = Color.Gray;

    public Func<bool> visibleGetter;
    public Func<int> widthGetter;
    public Action<PixelGrid> UpdateBeforeRender;
    public bool fadeOut = false;

    public PixelGrid(Func<bool> visibleGetter, Func<int> widthGetter, Action<PixelGrid> UpdateBeforeRender, bool fadeOut = false) {
        Depth = 8900;
        // lower than BackgroudTiles
        Collidable = false;
        Collider = new Hitbox(0f, 0f);
        this.visibleGetter = visibleGetter;
        this.widthGetter = widthGetter;
        this.UpdateBeforeRender = UpdateBeforeRender;
        this.fadeOut = fadeOut;
    }

#pragma warning disable CS8509
    public static Color GetGridColor(int index, float alpha = 0.5f) {
        return (Math.Abs(index) % 2) switch {
            0 => color1 * alpha,
            1 => color2 * alpha,
        };
    }
#pragma warning restore CS8509

    public Color FadeOutColor(int RelativeX, int RelativeY, float width) {
        return GetGridColor(RelativeX + RelativeY, fadeOut ? (1 - Distance(RelativeX, RelativeY) / width) * TasHelperSettings.PixelGridOpacity * 0.1f : TasHelperSettings.PixelGridOpacity * 0.1f);
    }

    public float Distance(int RelativeX, int RelativeY) {
        float DistX = 0f;
        float DistY = 0f;
        if (RelativeX < Collider.Left - 1) {
            DistX = Collider.Left - 1 - RelativeX;
        }
        else if (RelativeX > Collider.Right) {
            DistX = RelativeX - Collider.Right;
        }
        if (RelativeY < Collider.Top - 1) {
            DistY = Collider.Top - 1 - RelativeY;
        }
        else if (RelativeY > Collider.Bottom) {
            DistY = RelativeY - Collider.Bottom;
        }
        return (float)Math.Sqrt(DistX * DistX + DistY * DistY);
    }

    public override void Update() {
        // do nothing
    }
    public override void Render() {
        if (visibleGetter() && TasSettings.ShowGameplay && DebugRendered) {
            UpdateBeforeRender(this);
            RenderWithoutCondition();
        }
        // we render it either in Render and DebugRender, to have the right depth
    }

    public override void DebugRender(Camera camera) {
        if (!TasSettings.ShowGameplay && visibleGetter()) {
            UpdateBeforeRender(this);
            RenderWithoutCondition();
        }
    }

    public void RenderWithoutCondition() {
        int outerwidth = widthGetter();
        for (int x = (int)(Collider.Left - outerwidth); x < Collider.Right + outerwidth; x++) {
            for (int y = (int)(Collider.Top - outerwidth); y < Collider.Bottom + outerwidth; y++) {
                Draw.Point(new Vector2(Position.X + x, Position.Y + y), FadeOutColor(x, y, outerwidth));
            }
        }
    }
}

