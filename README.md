# TAS-Helper

https://gamebanana.com/tools/12383

This mod is based on [CelesteTAS](https://github.com/psyGamer/CelesteTAS-EverestInterop) and aims to provide some extra convenience for making TASes.

Note that from TAS Helper v2.0.0, we target [psyGamer's branch](https://github.com/psyGamer/CelesteTAS-EverestInterop) of CelesteTAS (version >= 3.40.0) instead of [DemoJameson's branch](https://github.com/EverestAPI/CelesteTAS-EverestInterop). If you are still using DemoJameson's branch, then you should use [TAS Helper v1.9.15](https://github.com/LozenChen/TAS-Helper/releases/tag/v1.9.15).

# Features:

- Check the menu in game! The menu should contain enough descriptions about TAS Helper features.

- In the following, hazards mean vanilla's CrystalStaticSpinner, Lightning and DustStaticSpinner, FrostHelper's CustomSpinner/AttachedLightning, VivHelper's CustomSpinner, and ChronoHelper's ShatterSpinner/DarkLightning.

- Cycle Hitbox Colors -> basically same as that in CelesteTAS mod, plus a bit modification when hazards are not in view or when spinner freezes.

- Hazard Countdown -> show how many frames later, the Spinner/Lightning/DustBunny will be (un)collidable if condition is satisified. Being gray or not indicates the hazard's collidability.

- Load Range -> including InView Range and NearPlayer Range. Hazards are considered to satisfy some condition in their updates (turn on/off collision etc.) if they are inside/outside corresponding ranges. When using Load Range, will also draw a Load Range Collider of hazards. A hazard is considered to be inside a range, if its Load Range Collider collides with the range. For spinners/dust bunnies, the collider is their center point. For lighting, the Load Range Collider is a rectangle a bit larger than its hitbox. The purple box is Actual Near Player Range, which appears when player's position changed during different NearPlayer checks, just like actual collide hitboxes.

- Simplified Spinner -> redraw hitbox of Spinner and Dust, also allow you to remove their sprites.

- Predictor -> predict the future track of your tas file in real time, no longer need to run tas frequently!

- Pixel Grid -> a pixel grid around player to help you find out the distance easily. Usually to check if player can climbjump/wallbounce.

- Entity Activator Reminder -> remind you when a PandorasBox mod's Entity Activator is created.

- Camera Target -> show which direction the camera moves towards. Basically *CameraTarget = Player's position + CameraOffset*, plus CameraTarget should be bounded in room and some other modification, then *CameraPosition = (1-r)\*PreviousCameraPosition + r\*CameraTarget*, where *r = 0.074*. We visualize this by drawing the points Position, PreviousPosition and CameraTarget, and drawing a link from PreviousPosition to CameraTarget.

- CustomInfoHelper -> provide some fields / properties which are not easy to compute in CelesteTAS's CustomInfo. Check [CustomInfoHelper](https://github.com/LozenChen/TAS-Helper/blob/main/Source/Gameplay/CustomInfoHelper.cs)

- Order-of-Operation Stepping -> just like frame advance, but in a subframe scale, thus visualize order of operations in a frame. The bottom-left message indicates the next action (if there's no "begin/end" postfix) / current action (if there is) of the game engine.

- Allow opening console in TAS.

- Scrollable Console History Log -> Besides holding ctrl + up/down (provided by Everest), you can now use MouseWheel/PageUp/PageDown to scroll over the history logs. Press Ctrl+PageUp/Down to scroll to top/bottom.

- Hotkeys -> you can change some of the settings using hotkeys.

- Main Switch hotkey -> Settings are memorized in a way that, ActualSettings = MainSwitch state && MemorizedSettings (if both sides are boolean. Similar for other types). The Main Switch hotkey just modifies MainSwitch state, and will not modify MemorizedSettings. Editing settings in menu or using other hotkeys will modify MemorizedSettings.

- Add some commands -> Currently we have, spinner_freeze cmd, nearest_timeactive cmd, setting-related cmd, OoO config cmd. Check [Commands](https://github.com/LozenChen/TAS-Helper/blob/main/Docs/Commands.md)

- ...

# Feature Request:

  If you have feature requests related to TAS, you can ping/DM me @Lozen#0956 on Celeste discord server. Please describe your feature request as detailed as possible. However, there is no guarantee that the final result will be same as what you've demanded.

  When a feature is useful and standard enough to become a part of CelesteTAS, this feature will first be merged into TAS Helper (so you can get it at first time), and a pull request/an issue on this feature will be submitted to CelesteTAS simultaneously.

# Some details:

- FrostHelper's CustomSpinner may have "no cycle", which means they will turn on/off collidable every frame.

- BrokemiaHelper's CassetteSpinner, is considered as "no cycle", since its collidablity is completely determined by cassette music. However, its visibility do have a 15f cycle (useless, it can't interact with collidablity).

# Plans:

  Here lists some ideas, which I may not work on recently. Feel free if you like that idea and want to implement that in your mod (tell me when you've implemented it so i needn't work on them).

- Slowdown indicator (note there's 1 frame delay between DeltaTime and TimeRate)

- Key cycle indicator.

- Push on XMinty's AutoWatch PR on CelesteTAS, to support more entities (e.g. for an entity with a re-awake timer, watch the timer if it's not zero).

- SpeedrunTool multi-saveslots PR (Update: done, PR is created but never gets merged)

- Auto completion in Celeste Studio (when using something like "set invincible true"), and some other gadgets for Studio. (Update: see psyGamer's Studio v3)

# Known issues:

- There will be some offset between HiresRenderer and Gameplay contents when we use ExtendedVariant.ZoomLevel. This also applies to CelesteTAS.CenterCamera when we zoom out. -> maybe will fix this later.

- VivHelper spinner isn't fully supported if its hitbox is not prestored -> maybe will add support for them.

- Laggy when there are too many spinners (e.g. Strawberry Jam GrandMaster HeartSide) -> Partially solved in v1.4.7.

- Predictor can't handle commands like StunPause Simulate (StunPause Input is ok), SetCommands, InvokeCommands and so on. -> Currently don't plan to support them. Tell me if you need this feature.

- Celeste TAS hotkeys randomly work improperly -> Not sure if it's caused by TAS Helper.

- ~~Use SRT save, then reload asset, then SRT load. This causes crash -> I guess it's a general issue and only happens for mod developers, so just ignore it.~~