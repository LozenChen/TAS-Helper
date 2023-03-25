using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using VivEntities = VivHelper.Entities;

namespace Celeste.Mod.TASHelper.Utils;

// basically i want something like MTexture
// however i do not know how to create a MTexture as desired
// so i do these stuff manually
public static class ColliderListHelper {
    public struct ColliderListKey {
        public static float eps = 0.001f;
        public Vector2 offset;
        public string[] hitboxString;
        public float scale;
        public ColliderListKey(Vector2 offset, string[] hitboxString, float scale) {
            this.offset = offset;
            this.hitboxString = hitboxString;
            this.scale = scale;
        }
        public static bool operator ==(ColliderListKey key1, ColliderListKey key2) {
            return Math.Abs(key1.offset.X - key2.offset.X) < eps && Math.Abs(key1.offset.Y - key2.offset.Y) < eps && Math.Abs(key1.scale - key2.scale) < eps && Enumerable.SequenceEqual(key1.hitboxString,key2.hitboxString);
        }
        public static bool operator !=(ColliderListKey key1, ColliderListKey key2) {
            return Math.Abs(key1.offset.X - key2.offset.X) >= eps || Math.Abs(key1.offset.Y - key2.offset.Y) >= eps || Math.Abs(key1.scale - key2.scale) >= eps || !Enumerable.SequenceEqual(key1.hitboxString, key2.hitboxString);
        }
    }

    public static Dictionary<ColliderListKey, Tuple<List<Rectangle>, List<Rectangle>>> CachedRectangle = new();
    public static void Render(VivEntities.CustomSpinner self, Color outlineColor, Color insideColor) {
        Vector2 roundPosition = DrawRound(self.Position);
        Vector2 offset = self.Position - roundPosition;
        string[] hitboxString = SpinnerHelper.VivHitboxStringGetter.GetValue(self) as string[];
        float scale = self.scale;
        ColliderListKey key = new(offset, hitboxString, scale);
        int roundX = (int)roundPosition.X;
        int roundY = (int)roundPosition.Y;
        if (false && CachedRectangle.TryGetValue(key, out Tuple<List<Rectangle>, List<Rectangle>> rectangles)) {
            foreach (Rectangle rect in rectangles.Item1) {
                Rectangle rectInPosition = new() {
                    X = rect.X + roundX,
                    Y = rect.Y + roundY,
                    Width = rect.Width,
                    Height = rect.Height
                };
                Monocle.Draw.Rect(rectInPosition, outlineColor);
                Logger.Log("rendered!");
            }
            foreach (Rectangle rect in rectangles.Item2) {
                Rectangle rectInPosition = new() {
                    X = rect.X + roundX,
                    Y = rect.Y + roundY,
                    Width = rect.Width,
                    Height = rect.Height
                };
                Monocle.Draw.Rect(rectInPosition, insideColor);
            }
        }
        else {
            BeginStoreDraw(self);
            Collider[] list = (self.Collider as ColliderList).colliders;
            foreach (Collider collider in list) {
                if (collider is Hitbox hitbox) {
                    Canvas.DrawHitbox(hitbox);
                    Logger.Log(hitbox);
                }
            }
            foreach (Collider collider in list) {
                if (collider is Circle circle) {
                    Canvas.DrawCircle(circle);
                    Logger.Log(circle);
                }
            }
            
            //EndStoreDraw(key);

            //Render(self, outlineColor, insideColor);

            EasyTexture.Rectangles rect1 = Canvas.Texture.rectangles;
            EasyTexture.Rectangles rect2 = Canvas.Texture.insideRectangles;
            foreach (Rectangle rect in rect1.wrap_rectangles) {
                Rectangle rectInPosition = new() {
                    X = rect.X + roundX,
                    Y = rect.Y + roundY,
                    Width = rect.Width,
                    Height = rect.Height
                };
                Monocle.Draw.Rect(rectInPosition, outlineColor);
            }
            foreach (Rectangle rect in rect2.wrap_rectangles) {
                Rectangle rectInPosition = new() {
                    X = rect.X + roundX,
                    Y = rect.Y + roundY,
                    Width = rect.Width,
                    Height = rect.Height
                };
                Monocle.Draw.Rect(rectInPosition, insideColor);
            }
        }
    }

    public static Vector2 DrawRound(this Vector2 vec) {
        return new Vector2((float)Math.Round(vec.X), (float)Math.Round(vec.Y));
        // when rendering a point, it's same to render point(x,y) and render point(round x, round y)
        // when rendering a hitbox, the TopLeft point(x,y) is same as (float x, float y) (coz CelesteTAS.HitboxFixer)
        // anyway, it does not matter we choose round or floor here
    }
    public static void BeginStoreDraw(Entity entity) {
        Canvas.Texture = new();
        Canvas.offset = entity.Position - DrawRound(entity.Position);
    }

    public static void EndStoreDraw(ColliderListKey key) {
        //CachedRectangle.Add(key, new(Canvas.Texture.rectangles, Canvas.Texture.insideRectangles));
        return;
    }
}
public static class Canvas {
    public static EasyTexture Texture = new();
    public static Vector2 offset;

    public static void DrawHitbox(Hitbox hitbox) {
        DrawHollowRect(hitbox.Left + offset.X , hitbox.Top + offset.Y, hitbox.Width, hitbox.Height);
    }

    public static void DrawCircle(Circle circle) {
        DrawCircle(circle.Position + offset, circle.Radius);
    }
    public static void DrawHollowRect(float x, float y, float width, float height) {
        DrawLayer.BeginDraw();
        DrawLayer.DrawHollowRect(x, y, width, height);
        Logger.Log("BeforeMix", ":" , "\n");
        Logger.Log(Texture.rectangles.wrap_rectangles);
        DrawLayer.EndDraw().MixLayer();
        Logger.Log("AfterMix", ":", "\n");
        Logger.Log(Texture.rectangles.wrap_rectangles);
    }

    public static void DrawCircle(Vector2 center, float radius) {
        DrawLayer.BeginDraw();
        DrawLayer.DrawCircle(center, radius);
        Logger.Log("BeforeMixCir", ":", "\n");
        Logger.Log(Texture.rectangles.wrap_rectangles);
        DrawLayer.EndDraw().MixLayer();
        Logger.Log("AfterMixCir", ":", "\n");
        Logger.Log(Texture.rectangles.wrap_rectangles);
    }

    public static void MixLayer(this EasyTexture upper) {
        Logger.Log("Upper");
        Logger.Log(upper.rectangles.wrap_rectangles);
        EasyTexture copy = upper;
        for (int y = 0; y<= 2*EasyTexture.size; y++) {
            for (int x = 0; x <= 2 * EasyTexture.size; x++) {
                if (copy.outside.Get(x,y)) {
                    copy.filled.Set(x, y, Texture.filled.Get(x,y));
                }
            }
        }
        copy.outside = new();
        copy.UpdateAfterInputFinished();
        Logger.Log("copy", ":", "\n");
        Logger.Log(copy.rectangles.wrap_rectangles);
        Texture = copy;
        Logger.Log("Texture", ":", "\n");
        Logger.Log(Texture.rectangles.wrap_rectangles);
        return;
    }

}
public static class DrawLayer {
    public static EasyTexture Layer = new();

    public static void BeginDraw() {
        Layer = new();
    }

    public static EasyTexture EndDraw() {
        return Layer;
    }

    public static void DrawRect(float x, float y, float width, float height) {
        int ix = (int)Math.Floor(x);
        int iy = (int)Math.Floor(y);
        int iw = (int)Math.Ceiling(width + x - ix);
        int ih = (int)Math.Ceiling(height + y - iy);
        Layer.DrawRect(ix, iy, iw, ih);
    }

    public static void DrawHollowRect(float x, float y, float width, float height) {
        int ix = (int)Math.Floor(x);
        int iy = (int)Math.Floor(y);
        int iw = (int)Math.Ceiling(width + x - ix);
        int ih = (int)Math.Ceiling(height + y - iy);
        Layer.DrawRect(ix, iy, iw, 1);
        Layer.DrawRect(ix, iy + ih - 1, iw, 1);
        Layer.DrawRect(ix, iy + 1, 1, ih-2);
        Layer.DrawRect(ix + iw -1, iy + 1, 1, ih - 2);
    }

    // taken from CelesteTAS.HitboxFixer

    public static void DrawCircle(Vector2 center, float radius) {
        CircleOctant(center, radius, 1, 1, false);
        CircleOctant(center, radius, 1, -1, false);
        CircleOctant(center, radius, -1, 1, false);
        CircleOctant(center, radius, -1, -1, false);
        CircleOctant(center, radius, 1, 1, true);
        CircleOctant(center, radius, 1, -1, true);
        CircleOctant(center, radius, -1, 1, true);
        CircleOctant(center, radius, -1, -1, true);
    }
    private static void DrawLine(int x, int y0, int y1, bool interchangeXy) {
        int length = y1 - y0;
        if (interchangeXy) {
            DrawRect(y0, x, length, 1);
        }
        else {
            DrawRect(x, y0, 1, length);
        }
    }
    private static void CircleOctant(Vector2 center, float radius, float flipX, float flipY, bool interchangeXy) {
        // when flipX = flipY = 1 and interchangeXY = false, we are drawing the [0, pi/4] octant.
        float cx, cy;
        if (interchangeXy) {
            cx = center.Y;
            cy = center.X;
        }
        else {
            cx = center.X;
            cy = center.Y;
        }

        float x, y;
        if (flipX > 0) {
            x = (float)Math.Ceiling(cx + radius - 1);
        }
        else {
            x = (float)Math.Floor(cx - radius + 1);
        }

        if (flipY > 0) {
            y = (float)Math.Floor(cy);
        }
        else {
            y = (float)Math.Ceiling(cy);
        }

        float starty = y;
        float e = (x - cx) * (x - cx) + (y - cy) * (y - cy) - radius * radius;
        float yc = flipY * 2 * (y - cy) + 1;
        float xc = flipX * -2 * (x - cx) + 1;
        while (flipY * (y - cy) <= flipX * (x - cx)) {
            e += yc;
            y += flipY;
            yc += 2;
            if (e >= 0) {
                DrawLine((int)x + (flipX < 0 ? -1 : 0), (int)starty, (int)y, interchangeXy);
                starty = y;
                e += xc;
                x -= flipX;
                xc += 2;
            }
        }
        DrawLine((int)x + (flipX < 0 ? -1 : 0), (int)starty, (int)y, interchangeXy);
    }
}
public struct EasyTexture {

    public const int size = 10;
    internal struct Filled {
        private bool[,] filledInverted = new bool[1+2*size, 1+2*size];

        public Filled() { }
        public bool Get(int x, int y){
            return filledInverted[y, x];
        }
        public void Set(int x, int y, bool b) {
            filledInverted[y, x] = b;
        }

        public bool GetH(int left, int right, int y) {
            for (int x = left; x <= right; x++) {
                if (!Get(x, y)) return false;
            }
            return true;
        }
    }

    public struct Rectangles {
        public List<Rectangle> wrap_rectangles = new List<Rectangle>();

        public Rectangles() { }

    }

    internal Filled filled = new();

    internal Filled outside = new();

    internal Filled inside = new();

    public Rectangles rectangles = new();

    public Rectangles insideRectangles = new();
    public EasyTexture() {}

    public void SetH(int x, int y, int length, bool fill) {
        for (int i = x; i < x + length; i++) {
            filled.Set(x,y, fill);
        }
    }

    public void SetV(int x, int y, int length, bool fill) {
        for (int i = y; i < y + length; i++) {
            filled.Set(x,y,fill);
        }
    }

    public void SetRect(int left, int right, int top, int bottom, bool fill) {
        for (int y = top; y<= bottom; y++) {
            SetH(left, y , right - left + 1 , fill);
        }
    }

    public void DrawHLine(int x, int y, int length) {
        SetH(x+size,y+size, length, true);
    }
    public void DrawVLine(int x, int y, int length) {
        SetV(x + size, y+size, length, true);
    }
    public void DrawRect(int x, int y, int width, int height) {
        SetRect(x + size, x +size + width -1 ,  y + size, y +size + height - 1 , true);
    }

    public void UpdateAfterInputFinished() {
        UpdateOutside();
        UpdateInside();
        UpdateRectangles();
        UpdateInsideRectangles();
        Logger.Log("UpdateAfter", ":", "\n");
        Logger.Log(rectangles.wrap_rectangles);
    }

    private EasyTexture IterateToRectangles() {
        for (int y = 0;  y <= 2*size; y++) {
            for (int x = 0; x <= 2 * size; x++) {
                if (filled.Get(x, y)) {
                    int k = x;
                    while (filled.Get(k+1, y )) { 
                        k++;
                    }
                    int l = y;
                    while (filled.GetH(x, k, l+1)) {
                        l++;
                    }
                    this.SetRect(x, k, y, l, false);
                    this.rectangles.wrap_rectangles.Add(new Rectangle(x-size, y-size, k - x + 1, l - y + 1));
                    return this.IterateToRectangles();
                }
            }
        }
        return this;
    }

    private void UpdateRectangles() {
        EasyTexture copy = (EasyTexture)this.MemberwiseClone();
        copy.IterateToRectangles();
        this.rectangles = copy.rectangles;
    }

    private EasyTexture UpdateOutside() {
        outside.Set(0, 0, true);
        for (int y = 0; y <= 2*size;y++) {
            for (int x = 0; x <= 2*size; x++) {
                if (x+1 <= 2*size) {
                    if (!outside.Get(x+1, y) && !filled.Get(x+1,y)) {
                        outside.Set(x + 1, y , true);
                        return this.UpdateOutside();
                    }
                }
                if (x-1 >= 0) {
                    if (!outside.Get(x-1,y) && !filled.Get(x - 1, y)) {
                        outside.Set(x - 1, y, true);
                        return this.UpdateOutside();
                    }
                }
                if (y+1 <= 2*size) {
                    if (!outside.Get(x,y+1) && !filled.Get(x, y + 1)) {
                        outside.Set(x, y + 1, true);
                        return this.UpdateOutside();
                    }
                }
                if (y-1 >= 0) {
                    if (!outside.Get(x,y-1) && !filled.Get(x, y - 1)) {
                        outside.Set(x,y-1, true);
                        return this.UpdateOutside();
                    }
                }
            }
        }
        return this;
    }

    private void UpdateInside() {
        for (int y = 0; y <= 2*size; y++) {
            for (int x = 0; x<= 2*size; x++) {
                inside.Set(x, y, !outside.Get(x,y) && !filled.Get(x,y));
            }
        }
    }

    private void UpdateInsideRectangles() {
        EasyTexture copy = new();
        copy.filled = this.inside;
        copy.IterateToRectangles();
        this.insideRectangles = copy.rectangles;
    }
}