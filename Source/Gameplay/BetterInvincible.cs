using Celeste.Mod.TASHelper.Utils;
using MonoMod.Cil;
using TAS;
using TAS.Input.Commands;

namespace Celeste.Mod.TASHelper.Gameplay;

#if NOT_MERGED
internal static class BetterInvincible {
    // make you invincible while still make tas sync
    // it will not persist after SL, and that's what we want!

    // if it (before savepoint) gets deleted, then tas file changes, so it should be detected and disable run will be invoked, and savestate will be cleared
    // if it (after savepoint) gets deleted, .... yeah it just gets deleted, when restart from savestate, Invincible = false will be loaded (as it's saved as such)

    // note that if you use RESTART hotkey ("=" by default), then LoadState will be invoked (if it's saved), but TasDisableRun won't!!

    public static bool Invincible = false;

    [Initialize]

    private static void Initialize() {
        typeof(Player).GetMethod("orig_Die").IlHook(il => {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, ins => ins.MatchLdfld<Assists>("Invincible"))) {
                cursor.EmitDelegate(ModifyInvincible);
            }
        });

        // todo: try remove hook on tas
        typeof(SetCommand).GetMethod("SetGameSetting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).OnHook(HookSetCommand);
    }


    [DisableRun]
    private static void OnDisableRun() {
        Invincible = false;
    }

    private static bool ModifyInvincible(bool origValue) {
        // Manager.Running may be redundant..
        return origValue || (Invincible && Manager.Running && TasHelperSettings.BetterInvincible); // safe guard, in case that disable run thing doesn't work somehow
    }

    private static void HookSetCommand(Action<string, string[]> orig, string settingName, string[] args) {
        if (SaveData.Instance is null || !Manager.Running || !TasHelperSettings.BetterInvincible) {
            orig(settingName, args);
            return;
        }

        bool beforeInvincible = SaveData.Instance.Assists.Invincible;
        orig(settingName, args);
        if (beforeInvincible != SaveData.Instance.Assists.Invincible) {
            if (!beforeInvincible) {
                SaveData.Instance.Assists.Invincible = false;
            }
            Invincible = !beforeInvincible;
        }
        // if originally invincible = true, but set to false, then betterInv = false
        // if originally inv = false, but set to true, then inv = false, and betterInv = true
    }
}
#endif