using Monocle;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.TASHelper.Utils;

public static class DashedLine {
    private static MTexture texture_H;
    private static MTexture texture_V;

    private const int MaxPeriod = 36;
    [Initialize]
    public static void Initialize() {
        texture_H = GFX.Game["TASHelper/DashedLine/dashedLine_H"];
        texture_V = GFX.Game["TASHelper/DashedLine/dashedLine_V"];
    }

    public static void Draw_H(Vector2 position, int length, Color color) {
        if (length > MaxPeriod) {
            texture_H.Draw(position, Vector2.Zero, color, Vector2.One, 0f, new Rectangle(1,MaxPeriod + 2, MaxPeriod, 1));
            Draw_H(position + MaxPeriod * Vector2.UnitX, length - MaxPeriod, color);
        }
        texture_H.Draw(position, Vector2.Zero, color, Vector2.One, 0f, new Rectangle(1, length, length, 1));
    }

    public static void Draw_V(Vector2 position, int length, Color color) {
        if (length > MaxPeriod) {
            texture_V.Draw(position, Vector2.Zero, color, Vector2.One, 0f, new Rectangle(MaxPeriod + 2, 1, 1, MaxPeriod));
            Draw_V(position + MaxPeriod * Vector2.UnitY, length - MaxPeriod, color);
        }
        texture_V.Draw(position, Vector2.Zero, color, Vector2.One, 0f, new Rectangle(length, 1, 1, length));
    }

    public static void DrawRect(Vector2 position, float width, float height, Color color) {
        int w = (int)width;
        int h = (int)height;
        Draw_H(position, w, color);
        Draw_H(position + (h - 1) * Vector2.UnitY , w, color);
        Draw_V(position + Vector2.UnitY, h -2, color);
        Draw_V(position + (w-1) * Vector2.UnitX + Vector2.UnitY, h-2, color);
    }
    public static void DrawRect(Rectangle rect, Color color) {
        Draw_H(new Vector2(rect.X, rect.Y), rect.Width, color);
        Draw_H(new Vector2(rect.X, rect.Y + rect.Height - 1), rect.Width, color);
        Draw_V(new Vector2(rect.X, rect.Y + 1), rect.Height - 2, color);
        Draw_V(new Vector2(rect.X + rect.Width - 1, rect.Y + 1), rect.Height - 2, color);
    }
}
