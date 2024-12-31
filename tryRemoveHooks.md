you can find hooks on CelesteTAS by searching ```// todo: try remove hook on tas```

- [ConsoleEnhancement](Source/Gameplay/ConsoleEnhancement.cs): these codes can be merged into CelesteTAS

- [ActualPosition](Source/Gameplay/ActualPosition.cs): removable, needs CelesteTAS to provide a CenterCameraPosition

- [RestoreSettingsExt](Source/Utils/RestoreSettingsExt.cs): removable, needs CelesteTAS to provide a blacklist 

- [SimplifiedTrigger.ModGetCustomColor](Source/Gameplay/SimplifiedTrigger.cs): removable, needs either CelesteTAS to provide some API, or TAS Helper mod export related methods / fields.

- [SimplifiedTrigger.ModFindClickedEntities](Source/Gameplay/SimplifiedTrigger.cs): removable, TAS Helper mod exports.

- [PredictorCore.PreventSendStateToStudio](Source/Predictor/PredictorCore.cs): removable, needs CelesteTAS to provide a field to turn off SendStateToStudio

- [BetterInvincible](Source/Gameplay/BetterInvincible.cs): in some sense redirects SetCommand's "Set Invincible true" to "set TAS Helper's invincible on". seems hard to remove. But can do better with a mod export.... okay it's best to just merge into CelesteTAS.

- [AutoWatchEntity.CoreLogic](Source/Gameplay/AutoWatchEntity/CoreLogic.cs): doable, add some events.

# Other refactor

- InfoWatch if log to command.

- InfoWatch if different log level to in-game info panel / studio panel (a level should be none / name only?)