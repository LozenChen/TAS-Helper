using Celeste.Mod.TASHelper.Utils;
using MonoMod.Cil;
using TAS;
using TAS.Input.Commands;

namespace Celeste.Mod.TASHelper.Gameplay;
internal static class BetterInvincible {
    // make you invincible while still make tas sync

    public static bool Invincible = false;

    [Initialize]

    private static void Initialize() {
        typeof(Player).GetMethod("orig_Die").IlHook(il => {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, ins => ins.MatchLdfld<Assists>("Invincible"))) {
                cursor.EmitDelegate(ModifyInvincible);
            }
        });

        typeof(SetCommand).GetMethod("SetGameSetting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).OnHook(HookSetCommand);
    }

    [TasDisableRun]

    private static void OnDisableRun() {
        Invincible = false;
    }

    private static bool ModifyInvincible(bool origValue) {
        // Manager.Running may be redundant..
        return origValue || (Invincible && Manager.Running);
    }

    private static void HookSetCommand(Action<string[]> orig, string[] args) {
        if (SaveData.Instance is null || !Manager.Running || !TasHelperSettings.BetterInvincible) {
            orig(args);
            return;
        }

        bool beforeInvincible = SaveData.Instance.Assists.Invincible;
        orig(args);
        if (beforeInvincible != SaveData.Instance.Assists.Invincible) {
            if (!beforeInvincible) {
                SaveData.Instance.Assists.Invincible = false;
            }
            Invincible = !beforeInvincible;
        }
    }
}