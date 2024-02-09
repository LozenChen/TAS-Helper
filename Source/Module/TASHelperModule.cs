using Celeste.Mod.TASHelper.Module.Menu;
using FMOD.Studio;

namespace Celeste.Mod.TASHelper.Module;

public class TASHelperModule : EverestModule {

    public static TASHelperModule Instance;

    public static TASHelperSettings Settings => TASHelperSettings.Instance;
    public TASHelperModule() {
        Instance = this;
        AttributeUtils.CollectMethods<LoadAttribute>();
        AttributeUtils.CollectMethods<UnloadAttribute>();
        AttributeUtils.CollectMethods<LoadContentAttribute>();
        AttributeUtils.CollectMethods<InitializeAttribute>();
        AttributeUtils.CollectMethods<TasDisableRunAttribute>();
        AttributeUtils.CollectMethods<TasEnableRunAttribute>();
        AttributeUtils.CollectMethods<ReloadAttribute>();
        AttributeUtils.CollectMethods<EventOnHookAttribute>();
    }

    public override Type SettingsType => typeof(TASHelperSettings);
    public override void Load() {
        Loader.Load();
    }

    public override void Unload() {
        Loader.Unload();
    }

    public override void Initialize() {
        Loader.Initialize();
    }

    public override void LoadContent(bool firstLoad) {
        if (firstLoad) {
            Loader.LoadContent();
        }
    }

    public override void LoadSettings() {
        base.LoadSettings();
        TasHelperSettings.OnLoadSettings();
    }

    public override void OnInputInitialize() {
        base.OnInputInitialize();
        TH_Hotkeys.HotkeyInitialize();
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
        CreateModMenuSectionHeader(menu, inGame, snapshot);
        TASHelperMenu.CreateMenu(this, menu, inGame);
    }
}