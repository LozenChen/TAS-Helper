using Celeste.Mod.TASHelper.Utils;
using FMOD.Studio;

namespace Celeste.Mod.TASHelper.Module;

public class TASHelperModule : EverestModule {

    public static TASHelperModule Instance;
    public TASHelperModule() {
        Instance = this;
    }

    public override Type SettingsType => typeof(TASHelperSettings);
    public override void Load() {
        On.Celeste.Level.Render += HotkeysPressed;
        Loader.HelperLoad();
        Loader.EntityLoad();
    }

    public override void Unload() {
        On.Celeste.Level.Render -= HotkeysPressed;
        Loader.HelperUnload();
        Loader.EntityUnload();
    }

    public override void Initialize() {
        Loader.HelperInitialize();
    }

    public override void LoadContent(bool firstLoad) {
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
        CreateModMenuSectionHeader(menu, inGame, snapshot);
        TASHelperMenu.CreateMenu(this, menu, inGame);
    }


    private static void HotkeysPressed(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);
        TasHelperSettings.SettingsHotkeysPressed();
        TasHelperSettings.UpdateAuxiliaryVariable();
        // if you call Instance.SaveSettings() here, then the game will crash if you open Menu-Mod Options in a Level and close the menu.
        // i don't know why, but just never do this.
    }
}










