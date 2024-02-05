using Monocle;

namespace Celeste.Mod.TASHelper.Utils;

public static class TagUtils {
    // why we need this:
    // we are to handle a large amount of entities with some specific types/characteristics
    // if that's to add something new, then we can just collect these entities, then do things in Scene.Before/After-Update/Render
    // or if that is to be executed when update, then we can just add these events to entity.pre/post update (e.g. we add these events when calling EntityList.UpdateLists, if its Update/Awake function is not overriden (so we can't find a good location to hook these events onto it))
    // still some worst case: we are going to modify the whole logic, instead of adding something new, and there is no overriden function for us to hook (so we can only hook the parent one... if the characteristics is a bit complex, then we may lose too much time on finding these entities, and we have to do this every game loop!!!), and things happen in (debug)render, so we can not use pre/post update
    // luckily we still have Entity.Tag
    // we put our results on this tag, so results are stored, so we can find these special entities quickly
    // or similarly we can add a component (specifically invented for our use here), we can store everything into component, however it's expansive than Tag, but i guess it's still cheaper in most cases when comparing to computing some heavy stuff each frame


    public static void SafeAdd(string name, out BitTag tag) {
        if (BitTag.byName.TryGetValue(name, out tag)) {
            return;
        }
        if (BitTag.TotalTags >= 32) {
            string existingTag = string.Join(", ", BitTag.byName.Keys);
            throw new Exception($"[TAS Helper] Monocle.BitTag contains too many instances, fail to add a new BitTag. Existing Tags: {existingTag}");
        }
        tag = new BitTag(name);
        return;
    }
}