using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.TASHelper.Entities;

[Tracked(false)]
public class PixelGrid : Entity {
    public static Color color1 = Color.White;
    public static Color color2 = Color.Gray;

    public Func<bool> visibleGetter;
    public int width = 0;
    public Action<PixelGrid> UpdateBeforeRender;

    private static MTexture texture;

    private const int TextureSize = 24 - 2;

    public PixelGrid(Func<bool> visibleGetter, Action<PixelGrid> UpdateBeforeRender) {
        Depth = 8900;
        // lower than BackgroudTiles
        Collidable = false;
        Collider = new Hitbox(0f, 0f);
        this.visibleGetter = visibleGetter;
        this.UpdateBeforeRender = UpdateBeforeRender;
    }

    [LoadLevel]
    private static void CreatePixelGridAroundPlayer(Level self) {
        self.Add(new PixelGrid(() => TasHelperSettings.EnablePixelGrid && player is not null, PixelGridAroundPlayerUpdate));
    }
    private static void PixelGridAroundPlayerUpdate(PixelGrid self) {
        if (player is not null) {
            self.Position = player.Position;
            self.Collider.Width = player.Collider.Width;
            self.Collider.Height = player.Collider.Height;
            self.Collider.Left = player.Collider.Left;
            self.Collider.Top = player.Collider.Top;
            self.width = TasHelperSettings.PixelGridWidth;
        }
    }

    [Initialize]
    public static void Initialize() {
        texture = GFX.Game["TASHelper/PixelGrid/grid"];
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
        int left = (int)(Collider.Left - width);
        int top = (int)(Collider.Top - width);
        int w = (int)Collider.Width + 2 * width;
        int h = (int)Collider.Height + 2 * width;
        Color c1, c2;
        float alpha = TasHelperSettings.PixelGridOpacity * 0.1f;
        if ((left + top) % 2 == 0) {
            c1 = color1 * alpha;
            c2 = color2 * alpha;
        }
        else {
            c2 = color1 * alpha;
            c1 = color2 * alpha;
        }
        Draw(this.Position + new Vector2(left, top), w, h, c1, c2);

    }

    private static void Draw(Vector2 Position, int width, int height, Color color1, Color color2) {
        if (width <= TextureSize && height <= TextureSize) {
            texture.Draw(Position, Vector2.Zero, color1, Vector2.One, 0f, new Rectangle(0, 0, width, height));
            texture.Draw(Position, Vector2.Zero, color2, Vector2.One, 0f, new Rectangle(1, 0, width, height));
            return;
        }
        if (height <= TextureSize) {
            while (width > 0) {
                int w = Math.Min(width, TextureSize);
                Draw(Position, w, height, color1, color2);
                Position += Vector2.UnitX * w;
                width -= w;
            }
            return;
        }
        while (height > 0) {
            int h = Math.Min(height, TextureSize);
            Draw(Position, width, h, color1, color2);
            Position += Vector2.UnitY * h;
            height -= h;
        }
        return;
    }
}

