using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;

public static class SpinnerColliderHelper {

    public struct SpinnerColliderValue {
        public MTexture Outline;
        public MTexture Inside;
        public MTexture Outline_Dashed1;
        public MTexture Outline_Dashed2;
        public SpinnerColliderValue(MTexture Outline, MTexture Inside, MTexture Dashed1, MTexture Dashed2) {
            this.Outline = Outline;
            this.Inside = Inside;
            this.Outline_Dashed1 = Dashed1;
            this.Outline_Dashed2 = Dashed2;
        }

        public SpinnerColliderValue(string name) {
            this.Outline = GFX.Game[$"TASHelper/SpinnerCollider/{name}_outline"];
            this.Inside = GFX.Game[$"TASHelper/SpinnerCollider/{name}_inside"];
            this.Outline_Dashed1 = GFX.Game[$"TASHelper/SpinnerCollider/{name}_outlineDashed1"];
            this.Outline_Dashed2 = GFX.Game[$"TASHelper/SpinnerCollider/{name}_outlineDashed2"];
        }
    }

    public static string SpinnerColliderKey(string hitboxString, float scale) {
        return hitboxString + "//" + scale.ToString();
    }

    public static string SpinnerColliderKey(string[] hitboxString, float scale) {
        return string.Join("|", hitboxString) + "//" + scale.ToString();
    }

    public static Dictionary<string, SpinnerColliderValue> SpinnerColliderTextures = new();

    public static SpinnerColliderValue Vanilla;

    [Initialize]
    public static void Initialize() {
        // learn from https://github.com/EverestAPI/Resources/wiki/Adding-Sprites#using-a-spritebank-file

        // it's quite foolish, as it cant spot identical expressions, i have to manually add some after i find it not working properly

        SpinnerColliderValue C6 =  new SpinnerColliderValue("C600");
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6", 1f), C6);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0", 1f), C6);

        Vanilla = new SpinnerColliderValue("vanilla");
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,4;-8,-3", 1f), Vanilla);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,4;-8,*1@-4", 1f), Vanilla);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,*4;-8,*-3", 1f), Vanilla);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0|R:16,4;-8,*1@-4", 1f), Vanilla);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0|R:16,*4;-8,*-3", 1f), Vanilla);

        SpinnerColliderValue reverted = new SpinnerColliderValue("reverted");
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,4;-8,-1", 1f), reverted);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,*4;-8,*-1", 1f), reverted);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,4;-8,*-1", 1f), reverted);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0|R:16,*4;-8,*-1", 1f), reverted);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0|R:16,4;-8,*-1", 1f), reverted);

        SpinnerColliderValue C8 = new SpinnerColliderValue("C800");
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:8", 1f), C8);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:8;0,0", 1f), C8);

    }
}
