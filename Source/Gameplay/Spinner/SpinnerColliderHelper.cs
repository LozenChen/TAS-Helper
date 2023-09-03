using Monocle;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;

public static class SpinnerColliderHelper {

    public struct SpinnerColliderValue {
        public MTexture Outline;
        public MTexture Inside;
        public SpinnerColliderValue(MTexture Outline, MTexture Inside) {
            this.Outline = Outline;
            this.Inside = Inside;
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

        MTexture C6_o = GFX.Game["TASHelper/SpinnerCollider/C600_outline"];
        MTexture C6_i = GFX.Game["TASHelper/SpinnerCollider/C600_inside"];
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6", 1f), new SpinnerColliderValue(C6_o, C6_i));
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0", 1f), new SpinnerColliderValue(C6_o, C6_i));

        MTexture vanilla_o = GFX.Game["TASHelper/SpinnerCollider/vanilla_outline"];
        MTexture vanilla_i = GFX.Game["TASHelper/SpinnerCollider/vanilla_inside"];
        Vanilla = new SpinnerColliderValue(vanilla_o, vanilla_i);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,4;-8,-3", 1f), Vanilla);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,4;-8,*1@-4", 1f), Vanilla);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,*4;-8,*-3", 1f), Vanilla);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0|R:16,4;-8,*1@-4", 1f), Vanilla);
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0|R:16,*4;-8,*-3", 1f), Vanilla);

        MTexture reverted_o = GFX.Game["TASHelper/SpinnerCollider/reverted_outline"];
        MTexture reverted_i = GFX.Game["TASHelper/SpinnerCollider/reverted_inside"];
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,4;-8,-1", 1f), new SpinnerColliderValue(reverted_o, reverted_i));
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,*4;-8,*-1", 1f), new SpinnerColliderValue(reverted_o, reverted_i));
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6|R:16,4;-8,*-1", 1f), new SpinnerColliderValue(reverted_o, reverted_i));
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0|R:16,*4;-8,*-1", 1f), new SpinnerColliderValue(reverted_o, reverted_i));
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:6;0,0|R:16,4;-8,*-1", 1f), new SpinnerColliderValue(reverted_o, reverted_i));

        MTexture C800_o = GFX.Game["TASHelper/SpinnerCollider/C800_outline"];
        MTexture C800_i = GFX.Game["TASHelper/SpinnerCollider/C800_inside"];
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:8", 1f), new SpinnerColliderValue(C800_o, C800_i));
        SpinnerColliderTextures.Add(SpinnerColliderKey("C:8;0,0", 1f), new SpinnerColliderValue(C800_o, C800_i));
    }
}
