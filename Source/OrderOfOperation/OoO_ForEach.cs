using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.Mod.TASHelper.OrderOfOperation.OoO_Controller;

namespace Celeste.Mod.TASHelper.OrderOfOperation;

// For-each breakpoints inside EntityList.Update: stop before/after individual entities.
// The actual "stop" is OoO_Controller.BreakExternal; this class only decides when.
internal static class OoO_ForEach {
    public const string Each = "Each";
    public const string Auto = "Auto";

    private static readonly HashSet<string> targets = new();
    private static readonly HashSet<string> removedFromEach = new();
    private static bool checkEach;
    private static bool autoStop = true;

    private static string lastUID;
    private static OoO_PlayerSnapshot lastSnapshot;
    private static bool hasSnapshot;

    public static string GetUID(Entity entity) {
        return string.Concat(entity.GetType().Name, "[", entity.SourceId.ToString(), "]");
    }

    internal static void OnEntityLoopHead(Entity entity) {
        if (!Active) {
            return;
        }
        string type = entity.GetType().Name;
        string uid = GetUID(entity);
        bool contain = targets.Contains(type) || targets.Contains(uid);

        Player player = OoO_PlayerSnapshot.GetPlayer();
        // Auto: the previous entity's update changed the player -> stop right after it (we notice here, one entity later).
        if (hasSnapshot && autoStop && !OoO_PlayerSnapshot.Capture(player).Equals(lastSnapshot)) {
            BreakExternal(string.Concat(lastUID, " Update"));
        }

        bool excluded = removedFromEach.Contains(type) || removedFromEach.Contains(uid);
        if ((checkEach && !excluded) || contain) {
            OoO_Highlight.TrackedEntity = entity;
            BreakExternal(string.Concat(uid, " Update begin"));
        }

        lastUID = uid;
        lastSnapshot = OoO_PlayerSnapshot.Capture(player);
        hasSnapshot = true;
    }

    [Load]
    public static void Load() {
        targets.Add("Player");
    }

    public static void ResetRun() {
        hasSnapshot = false;
        lastUID = null;
    }

    [Command_StringParameter("ooo_add_target", "Add the entity as a for-each breakpoint in EntityList.Update() (TAS Helper OoO Stepping)")]
    public static void CmdAddTarget(string uid) {
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
        if (uid.StartsWith("FloatySpaceBlock")) {
            OoO_MoonBlock.AddTarget(uid);
            return;
        }
        targets.Add(uid);
    }

    [Command_StringParameter("ooo_remove_target", "Remove a for-each breakpoint in EntityList.Update() (TAS Helper OoO Stepping)")]
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
        if (uid.StartsWith("FloatySpaceBlock")) {
            OoO_MoonBlock.RemoveTarget(uid);
            return;
        }
        bool removed = targets.Remove(uid);
        if (!removed && checkEach) {
            removedFromEach.Add(uid);
        }
    }

    [Command("ooo_show_target", "Show all for-each breakpoints in EntityList.Update() (TAS Helper OoO Stepping)")]
    public static void ShowTarget() {
        foreach (string target in targets) {
            Celeste.Commands.Log(target);
        }
        if (checkEach) {
            Celeste.Commands.Log(Each);
            foreach (string removed in removedFromEach) {
                Celeste.Commands.Log("  except " + removed);
            }
        }
        if (autoStop) {
            Celeste.Commands.Log(Auto);
        }
        OoO_MoonBlock.ShowTargets();
    }

}
