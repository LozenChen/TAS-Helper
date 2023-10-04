# TAS-Helper

https://gamebanana.com/tools/12383

A Celeste Mod designed to be a tool in TAS making.

# Features:

- In the following, hazards mean vanilla's CrystalStaticSpinner, Lightning and DustStaticSpinner, FrostHelper's CustomSpinner/AttachedLightning, VivHelper's CustomSpinner, and ChronoHelper's ShatterSpinner/DarkLightning.

- Cycle Hitbox Colors -> basically same as that in CelesteTAS mod, plus a bit modification when hazards are not in view or when spinner freezes.

- Hazard Countdown -> show how many frames later, the Spinner/Lightning/DustBunny will be (un)collidable if condition is satisified.

- Load Range -> including InView Range and NearPlayer Range. Hazards are considered to satisfy some condition in their updates (turn on/off collision etc.) if they are inside/outside corresponding ranges. When using Load Range, will also draw a Load Range Collider of hazards. A hazard is considered to be inside a range, if its Load Range Collider collides with the range. For spinners/dust bunnies, the collider is their center point. For lighting, the Load Range Collider is a rectangle a bit larger than its hitbox. The purple box is Actual Near Player Range, which appears when player's position changed during different NearPlayer checks, just like actual collide hitboxes.

- Simplified Spinner -> redraw hitbox of Spinner and Dust, also allow you to remove their sprites.

- Predictor -> predict the future track of your tas file in real time, no longer need to run tas frequently!

- Pixel Grid -> a pixel grid around player to help you find out the distance easily. Usually to check if player can climbjump/wallbounce.

- Entity Activator Reminder -> remind you when a PandorasBox mod's Entity Activator is created.

- Camera Target -> show which direction the camera moves towards. Basically *CameraTarget = Player's position + CameraOffset*, plus CameraTarget should be bounded in room and some other modification, then *CameraPosition = (1-r)\*PreviousCameraPosition + r\*CameraTarget*, where *r = 0.074*. We visualize this by drawing the points Position, PreviousPosition and CameraTarget, and drawing a link from PreviousPosition to CameraTarget.

- Hotkeys -> you can change some of the settings using hotkeys.

- Main Switch hotkey -> Settings are memorized in a way that, ActualSettings = MainSwitch state && MemorizedSettings (if both sides are boolean. Similar for other types). The Main Switch hotkey just modifies MainSwitch state, and will not modify MemorizedSettings. Editing settings in menu or using other hotkeys will modify MemorizedSettings.

- Add some commands -> Currently only spinner_freeze cmd, nearest_timeactive cmd + some setting-related cmd.

- ... Check the menu in game!

# Some details:

- FrostHelper's CustomSpinner may have "no cycle", which means they will turn on/off collidable every frame.

- BrokemiaHelper's CassetteSpinner, is considered as "no cycle", since its collidablity is completely determined by cassette music. However, its visibility do have a 15f cycle (useless, it can't interact with collidablity).

# Plans:

  Here lists some ideas, which I may not work on recently. Feel free if you like that idea and want to implement that in your mod (tell me when you've implemented it so i needn't work on them').

- Slowdown indicator (note there's 1 frame delay between DeltaTime and TimeRate)

- Key cycle indicator.

- FlingBird indicator.

- Scrollable console.

- Better custom info. (not necessary due to the latest EvalLua command)

- Push on XMinty's AutoWatch PR on CelesteTAS, to support more entities (e.g. for an entity with a re-awake timer, watch the timer if it's not zero).

- Auto completion in Celeste Studio (when using something like "set invincible true"), and some other gadgets for Studio. (Update: relating codes already exist in Studio, but it seems they have not been used)

- Order of operations visualizer (probably as an individual mod), allows you to insert breakpoints in gameloops, so to observe sub-frame phenomenon.

# Known issues:

- Actual Collide Hitboxes are overridden -> it's actually bad to use actual collide hitboxes when doing a spinner stun, you really need the exact frame the hazard becomes collidable (opaque). So personnally i do not suggest using actual collide hitboxes in this case. Appended hitbox sounds good but current implement relies on opacity to show information. I have no good idea about it so it's set aside.

- VivHelper spinner isn't fully supported if its hitbox is not prestored -> maybe will add support for them.

- Laggy when there are too many spinners (e.g. Strawberry Jam GrandMaster HeartSide) -> Partially solved in v1.4.7

- Hotkeys can't work after several savestates -> Should be totally fixed in v1.6.5.

- TAS Helper does not save settings (change settings in the menu) when closing game with the X instead of the exit button in game -> Can't reproduce. It's said that turning off and on tashelper after changing settings will work. After several changes on settings system, i guess this bug should be addressed after v1.6.5.

- YamlException in Log.txt. -> fixed in v1.6.5.

- Predictor can't handle commands like StunPause Simulate (StunPause Input is ok), SetCommands, InvokeCommands and so on. -> Currently don't plan to support them. Tell me if you need this feature.

- Celeste TAS hotkeys randomly work improperly -> Not sure if it's caused by TAS Helper.