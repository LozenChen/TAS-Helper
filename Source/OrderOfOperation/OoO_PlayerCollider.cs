using Monocle;

namespace Celeste.Mod.TASHelper.OrderOfOperation;

// For-each breakpoints inside the PlayerCollider check loop of Player.orig_Update.
internal static class OoO_PlayerCollider {
    public const string Each = "Each";
    public const string Auto = "Auto";

    private static readonly HashSet<string> targets = new();
    private static readonly HashSet<string> removedFromEach = new();
    private static bool checkEach;
    private static bool autoStop = true;

    private static string lastUID;
    private static OoO_PlayerSnapshot lastSnapshot;
    private static bool hasSnapshot;

    internal static void OnLoopHead(PlayerCollider pc) {
        if (!OoO_Controller.Active) {
            return;
        }
        Entity entity = pc?.Entity;
        if (entity is null) {
            return;
        }
        string type = entity.GetType().Name;
        string uid = OoO_ForEach.GetUID(entity);

        Player player = OoO_PlayerSnapshot.GetPlayer();
        if (hasSnapshot && autoStop && !OoO_PlayerSnapshot.Capture(player).Equals(lastSnapshot)) {
            OoO_Controller.BreakExternal(string.Concat(lastUID, "'s PlayerCollider check end"));
        }

        bool excluded = removedFromEach.Contains(type) || removedFromEach.Contains(uid);
        if ((checkEach && !excluded) || targets.Contains(type) || targets.Contains(uid)) {
            OoO_Highlight.TrackedCollider = pc;
            OoO_Controller.BreakExternal(string.Concat(uid, "'s PlayerCollider check"));
        }

        lastUID = uid;
        lastSnapshot = OoO_PlayerSnapshot.Capture(player);
        hasSnapshot = true;
    }

    public static void ResetRun() {
        hasSnapshot = false;
        lastUID = null;
    }

    [Command_StringParameter("ooo_add_target_pc", "Add the entity as a for-each breakpoint in PlayerCollider checks (TAS Helper OoO Stepping)")]
    public static void AddTarget(string uid) {
        if (uid == Auto) {
            autoStop = true;
            return;
        }
        if (uid == Each) {
            checkEach = true;
            return;
        }
        if (removedFromEach.Remove(uid)) {
            return;
        }
        targets.Add(uid);
    }

    [Command_StringParameter("ooo_remove_target_pc", "Remove a for-each breakpoint in PlayerCollider checks (TAS Helper OoO Stepping)")]
    public static void RemoveTarget(string uid) {
        if (uid == Auto) {
            autoStop = false;
            return;
        }
        if (uid == Each) {
            checkEach = false;
            removedFromEach.Clear();
            return;
        }
        if (!targets.Remove(uid) && checkEach) {
            removedFromEach.Add(uid);
        }
    }

    [Command("ooo_show_target_pc", "Show all for-each breakpoints in PlayerCollider checks (TAS Helper OoO Stepping)")]
    public static void ShowTarget() {
        foreach (string target in targets) {
            Celeste.Commands.Log(target);
        }
        if (checkEach) {
            Celeste.Commands.Log(Each);
        }
        if (autoStop) {
            Celeste.Commands.Log(Auto);
        }
    }
}
