using Celeste.Mod.TASHelper.Utils;
using FMOD.Studio;
using CMCore = Celeste.Mod.Core;

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
        Loader.Initialize();
    }

    public override void LoadContent(bool firstLoad) {
        if (firstLoad) {
            Loader.LoadContent();
        }
    }

    public override void LoadSettings() {
        if (SettingsType == null) {
            return;
        }

        _Settings = (EverestModuleSettings)SettingsType.GetConstructor(Everest._EmptyTypeArray).Invoke(Everest._EmptyObjectArray);
        string path = UserIO.GetSaveFilePath("modsettings-" + Metadata.Name);
        if (!File.Exists(path)) {
            path = Path.Combine(Everest.PathEverest, "ModSettings-OBSOLETE", Metadata.Name + ".yaml");
        }

        if (!File.Exists(path)) {
            return;
        }

        try {
            using Stream stream = File.OpenRead(path);
            using StreamReader input = new StreamReader(stream);
            THYamlHelper.DeserializerUsing(_Settings).Deserialize(input, SettingsType);
        }
        catch (Exception e) {
            Logger.Log(LogLevel.Warn, "EverestModule", "Failed to load the settings of " + Metadata.Name + "!");
            Logger.LogDetailed(e);
        }

        if (_Settings == null) {
            _Settings = (EverestModuleSettings)SettingsType.GetConstructor(Everest._EmptyTypeArray).Invoke(Everest._EmptyObjectArray);
        }

        TasHelperSettings.OnLoadSettings();
    }

    public override void SaveSettings() {
        int n = this.GetFieldValue<int>("ForceSaveDataFlush");
        bool flag = n > 0;
        if (flag) {
            this.SetFieldValue("ForceSaveDataFlush",n-1);
        }
        if (SettingsType == null || _Settings == null) {
            return;
        }
        string saveFilePath = UserIO.GetSaveFilePath("modsettings-" + Metadata.Name);
        if (File.Exists(saveFilePath)) {
            File.Delete(saveFilePath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(saveFilePath));
        try {
            using FileStream fileStream = File.OpenWrite(saveFilePath);
            using StreamWriter writer = new StreamWriter(fileStream);
            THYamlHelper.Serializer.Serialize(writer, _Settings, SettingsType);
            if (flag || ((CMCore.CoreModule.Settings.SaveDataFlush ?? true) && !MainThreadHelper.IsMainThread)) {
                fileStream.Flush(flushToDisk: true);
            }
        }
        catch (Exception e) {
            Logger.Log(LogLevel.Warn, "EverestModule", "Failed to save the settings of " + Metadata.Name + "!");
            Logger.LogDetailed(e);
        }
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
        CreateModMenuSectionHeader(menu, inGame, snapshot);
        TASHelperMenu.CreateMenu(this, menu, inGame);
    }

    private static void HotkeysPressed(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);
        if (TasHelperSettings.SettingsHotkeysPressed()) {
            Instance.SaveSettings();
        }
    }
}










