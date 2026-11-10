using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner.Render;

// clear the sprite so SimplifiedSpinner renders correctly
internal static class SpriteCleaner {
    // Tas mod's UnloadedRoomHitbox also draws spinner textures, but we do not plan to clear them
    // some room like Farewell [c-alt-01], adjacent rooms can have spinner in same position! in that case, it may seem that sprite is not cleared
    public static bool SpritesCleared => DebugRendered && TasHelperSettings.ClearSpinnerSprites;

    private static List<FieldInfo> CrysExtraComponentGetter;

    private static List<FieldInfo> VivSpinnerExtraComponentGetter;

    private static FieldInfo XaphanSpinnerBorderGetter;

    private static FieldInfo XaphanSpinnerFillerGetter;

    private static List<FieldInfo> ChroniaSpinnerExtraComponentGetter;

    private static List<FieldInfo> ChronoSpinnerExtraComponentGetter;

    private static bool wasSpritesCleared = !SpritesCleared;

    private static bool NeedClearSprites = true;

    // sprites are created by e.g. AddSprites(), so they do not necessarily exist when load level

    private static bool NotUpdated => NeedClearSprites || wasSpritesCleared != SpritesCleared; // the latter one is used for hotkey changing that state

    private static readonly List<Action<Level>> ClearSpritesAction = new();

    [LoadLevel]

    private static void LoadLevel() {
        NeedClearSprites = true;
    }

    [Initialize]
    public static void Initialize() {
        typeof(Level).GetMethod("BeforeRender").ILHook((cursor, _) => {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(LevelBeforeRender);
        });

        CrysExtraComponentGetter = new() {
            typeof(CrystalStaticSpinner).GetField("border", BindingFlags.NonPublic | BindingFlags.Instance),
            typeof(CrystalStaticSpinner).GetField("filler", BindingFlags.NonPublic | BindingFlags.Instance)
        };
        ClearSpritesAction.Add(VanillaBeforeRender);
        OnCreateSprites(typeof(CrystalStaticSpinner));
        EOF(typeof(DustGraphic).GetConstructor(new Type[] { typeof(bool), typeof(bool), typeof(bool) }));


        if (ModUtils.GetType("FrostHelper", "FrostHelper.CustomSpinner") is { } frostSpinnerType && ModUtils.GetType("FrostHelper", "FrostHelper.SpinnerConnectorRenderer") is { } rendererType1 && ModUtils.GetType("FrostHelper", "FrostHelper.SpinnerBorderRenderer") is { } rendererType2 && ModUtils.GetType("FrostHelper", "FrostHelper.SpinnerDecoRenderer") is { } rendererType3) {
            ClearSpritesAction.Add(self => FrostBeforeRender(self, frostSpinnerType, new Type[] { rendererType1, rendererType2, rendererType3 }));
            OnCreateSprites(frostSpinnerType);
        }

        if (ModUtils.GetType("VivHelper", "VivHelper.Entities.CustomSpinner") is { } vivSpinnerType && ModUtils.GetType("VivHelper", "VivHelper.Entities.AnimatedSpinner") is { } vivAnimSpinnerType && ModUtils.GetType("VivHelper", "VivHelper.Entities.MovingSpinner") is { } vivMoveSpinnerType) {
            VivSpinnerExtraComponentGetter = new() {
                vivSpinnerType.GetField("border", BindingFlags.NonPublic | BindingFlags.Instance),
                vivSpinnerType.GetField("filler", BindingFlags.NonPublic | BindingFlags.Instance)
            };
            ClearSpritesAction.Add(self => VivBeforeRender(self, new Type[] { vivSpinnerType, vivAnimSpinnerType, vivMoveSpinnerType }));
            OnCreateSprites(vivSpinnerType);
            OnCreateSprites(vivAnimSpinnerType);
        }

        if (ModUtils.GetType("XaphanHelper", "Celeste.Mod.XaphanHelper.Entities.CustomSpinner") is { } xaphanSpinnerType) {
            XaphanSpinnerBorderGetter = xaphanSpinnerType.GetField("border", BindingFlags.NonPublic | BindingFlags.Instance);
            ClearSpritesAction.Add(self => {
                foreach (Entity spinner in self.Tracker.SafeGetEntities(xaphanSpinnerType)) {
                    spinner.UpdateComponentVisiblity();
                    object obj = XaphanSpinnerBorderGetter.GetValue(spinner);
                    if (obj is Entity e) {
                        e.Visible = SpritesCleared;
                    }
                }
            });
            OnCreateSprites(xaphanSpinnerType);

            XaphanSpinnerFillerGetter = xaphanSpinnerType.GetField("fillers", BindingFlags.Public | BindingFlags.Instance) ?? xaphanSpinnerType.GetField("filler", BindingFlags.NonPublic | BindingFlags.Instance);
            // after v1.0.64 (i.e. after the Secret of Celeste Mountain chap 3), "fillers" instead of "filler"
            xaphanSpinnerType.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance).HookAfter<Entity>((spinner) => {
                if (!spinner.GetFieldValue<bool>("Hidden")) {
                    object obj = XaphanSpinnerFillerGetter.GetValue(spinner);
                    if (obj is IEnumerable list) {
                        foreach (object o in list) {
                            ((Entity)o).Visible = !SpritesCleared;
                        }
                    }
                    else if (obj is Entity e) {
                        e.Visible = !SpritesCleared;
                    }
                }
            });
            // coz xaphanSpinner sets its Visible every frame
        }

        if (ModUtils.GetType("ChroniaHelper", "ChroniaHelper.Entities.SeamlessSpinner") is { } chroniaSpinnerType) {
            FieldInfo f1 = chroniaSpinnerType.GetFieldInfo("border");
            FieldInfo f2 = chroniaSpinnerType.GetFieldInfo("filler");
            if (f1 is not null && f2 is not null) {
                ChroniaSpinnerExtraComponentGetter = [f1, f2];
                ClearSpritesAction.Add(self => {
                    foreach (Entity spinner in self.Tracker.Entities[chroniaSpinnerType]) {
                        spinner.UpdateComponentVisiblity();
                        foreach (FieldInfo getter in ChroniaSpinnerExtraComponentGetter) {
                            object obj = getter.GetValue(spinner);
                            if (obj is Entity e) {
                                e.Visible = !SpritesCleared;
                            }
                        }
                    }
                });
                OnCreateSprites(chroniaSpinnerType);
            }
            else if (chroniaSpinnerType.GetMethod("Render") is { } method && method.DeclaringType == chroniaSpinnerType) {
                // check declaring type so we won't accidentally hook Entity.Render
                method.ILHook(il => {
                    ILCursor cursor = new ILCursor(il);
                    Instruction target = cursor.Next;
                    cursor.EmitDelegate(GetSpritesCleared);
                    cursor.Emit(OpCodes.Brfalse, target);
                    cursor.Emit(OpCodes.Ret);
                });

                static bool GetSpritesCleared() => SpritesCleared;
            }
            else {
                throw new Exception($"TASHelper/{nameof(SimplifiedSpinner)} can't support ChroniaHelper/SeamlessSpinner.");
            }
        }

        if (ModUtils.GetType("ChronoHelper", "Celeste.Mod.ChronoHelper.Entities.ShatterSpinner") is { } chronoSpinnerType) {
            ChronoSpinnerExtraComponentGetter = [
                chronoSpinnerType.GetField("border", BindingFlags.NonPublic | BindingFlags.Instance),
                chronoSpinnerType.GetField("filler", BindingFlags.NonPublic | BindingFlags.Instance)
            ];
            ClearSpritesAction.Add(self => {
                foreach (Entity spinner in self.Tracker.Entities[chronoSpinnerType]) {
                    spinner.UpdateComponentVisiblity();
                    foreach (FieldInfo getter in ChronoSpinnerExtraComponentGetter) {
                        object obj = getter.GetValue(spinner);
                        if (obj is Entity e) {
                            e.Visible = !SpritesCleared;
                        }
                    }
                }
            });
            OnCreateSprites(chronoSpinnerType);
        }

        if (ModUtils.GetType("BrokemiaHelper", "BrokemiaHelper.CassetteSpinner") is { } cassetteSpinnerType) { // we use this as a mod version check
            ClearSpritesAction.Add(self => {
                /*
                if (!self.Tracker.Entities.ContainsKey(cassetteSpinnerType)) {
                    // there's report that here's a KeyNotFoundException
                    // https://discord.com/channels/403698615446536203/754495709872521287/1180852881369350295
                    // though it never happens for me
                    // don't know why this would happen
                    // anyway, we add a redundant check
                    self.Tracker.Entities.Add(cassetteSpinnerType, new List<Entity>());
                }
                */
                foreach (Entity spinner in self.Tracker.SafeGetEntities(cassetteSpinnerType)) {
                    spinner.UpdateComponentVisiblity();
                    foreach (FieldInfo getter in CrysExtraComponentGetter) {
                        object obj = getter.GetValue(spinner);
                        if (obj is Entity e) {
                            e.Visible = !SpritesCleared;
                        }
                    }
                }
            });
            // CreateSprites inherited from Crys spinner, so no need to hook
        }

        if (ModUtils.GetType("IsaGrabBag", "Celeste.Mod.IsaGrabBag.DreamSpinnerRenderer") is { } dreamSpinnerRendererType) {
            ClearSpritesAction.Add(self => {
                foreach (Entity renderer in self.Tracker.SafeGetEntities(dreamSpinnerRendererType)) {
                    renderer.Visible = !SpritesCleared;
                }
            });
            EOF(dreamSpinnerRendererType.GetConstructor(Type.EmptyTypes));
        }

        // Additional catalog spinner variants: clear their vanilla-style border/filler and any custom
        // sprite collections. This is best-effort until each type is exercised in a test map.
        AddGenericSpinnerCleaner(ModUtils.GetType("FrostHelper", "FrostHelper.Entities.CustomDustBunny"));
        AddGenericSpinnerCleaner(ModUtils.GetType("DashToggleHelper", "Celeste.Mod.DashToggleHelper.DashToggleStaticSpinner"));
        AddGenericSpinnerCleaner(ModUtils.GetType("ScugHelper", "Celeste.Mod.ScugHelper.Entities.GPUSpinner"));
        AddGenericSpinnerCleaner(ModUtils.GetType("auspicioushelper", "Celeste.Mod.auspicioushelper.Wrappers.Spinner"));
        AddGenericSpinnerCleaner(ModUtils.GetType("NeihraHelper", "Celeste.Mod.NeihraHelper.Entities.CustomSpinner"));
        AddGenericSpinnerCleaner(ModUtils.GetType("ReverseHelper", "Celeste.Mod.ReverseHelper.Entities.SquareSpinner"));
        AddGenericSpinnerCleaner(ModUtils.GetType("TeamGames", "Celeste.Mod.TeamGames.Entities.TeamSpinner"));
        AddGenericSpinnerCleaner(ModUtils.GetType("WindHelper", "Celeste.Mod.WindHelper.Entities.FloatingSpinner"));
        AddGenericSpinnerCleaner(ModUtils.GetType("BreakableSpinners", "Celeste.Mod.BreakableSpinners.BreakableCrystalSpinner"));
        AddGenericSpinnerCleaner(ModUtils.GetType("ScugHelper", "Celeste.Mod.ScugHelper.Entities.StarjumpSpinner"));

        static void EOF(MethodBase method) {
            method.ILHook((cursor, _) => {
                cursor.Goto(cursor.Instrs.Count - 1);
                cursor.EmitDelegate(CallNeedClearSprites);
            });
        }

        static void OnCreateSprites(Type type) {
            if (type.GetMethod("CreateSprites", BindingFlags.NonPublic | BindingFlags.Instance) is { } method) {
                EOF(method);
            }
            else {
                throw new Exception($"TASHelper/{nameof(SimplifiedSpinner)} can't handle since {type} seems to have been updated.");
            }
        }

        void AddGenericSpinnerCleaner(Type type) {
            if (type is null) {
                return;
            }
            string[] fields = { "filler", "border", "fillers", "offImages", "onImages", "crystal", "bgTex", "fgTex", "image" };
            ClearSpritesAction.Add(self => {
                foreach (Entity spinner in self.Tracker.SafeGetEntities(type)) {
                    spinner.UpdateComponentVisiblity();
                    foreach (string fieldName in fields) {
                        FieldInfo field = type.GetFieldInfo(fieldName);
                        if (field is not null) {
                            SetVisible(field.GetValue(spinner));
                        }
                    }
                }
            });
            foreach (string methodName in new[] { "CreateSprites", "CreateSpinnerSprites", "CreateOffSprites" }) {
                if (type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance) is { } method) {
                    EOF(method);
                }
            }

            void SetVisible(object obj) {
                switch (obj) {
                    case Entity entity:
                        entity.Visible = !SpritesCleared;
                        break;
                    case Component component:
                        component.Visible = !SpritesCleared;
                        break;
                    case IEnumerable list:
                        foreach (object item in list) {
                            SetVisible(item);
                        }
                        break;
                }
            }
        }
    }

    private static void LevelBeforeRender(Level self) {
        /* the following comments are based on the implementation that: detect adding entities in Scene.Entities.UpdateLists, which we abandoned
         the most common Scene.Entities.UpdateLists call happens in Scene.BeforeUpdate
         CrystalStaticSpinner.CreateSprites happen in Scene.Update, which add the entity border and filler to Scene, so border and filler will render next frame
         however, it also add some image as components of CrystalStaticSpinner, they will render this frame
         so we should clear it right now
        */

        // we manually track in which cases, we need to clear sprites, so we do not need to update every frame

        // here i assume all the components are always visible
        // if that's not the case, then this implementation has bug

        if (NotUpdated) {
            foreach (Action<Level> action in ClearSpritesAction) {
                action(self);
            }

            // we must set it here immediately, instead of setting this at e.g. Level.AfterRender
            // coz SpritesCleared may change during this period of time, in that case wasSpritesCleared will not detect this change

            wasSpritesCleared = SpritesCleared;
            NeedClearSprites = false;
        }
    }

    private static void CallNeedClearSprites() {
        NeedClearSprites = true;
    }
    private static void VanillaBeforeRender(Level self) {
        foreach (Entity dust in self.Tracker.GetEntities<DustStaticSpinner>()) {
            dust.UpdateComponentVisiblity();
        }
        foreach (Entity spinner in self.Tracker.GetEntities<CrystalStaticSpinner>()) {
            spinner.UpdateComponentVisiblity();
            foreach (FieldInfo getter in CrysExtraComponentGetter) {
                object obj = getter.GetValue(spinner);
                if (obj is Entity e) {
                    e.Visible = !SpritesCleared;
                }
            }
        }
    }

    private static void FrostBeforeRender(Level self, Type customSpinnerType, Type[] renderers) {
        foreach (Entity customSpinner in self.Tracker.Entities[customSpinnerType]) {
            customSpinner.UpdateComponentVisiblity();
        }
        foreach (Type type in renderers) {
            foreach (Entity renderer in self.Tracker.Entities[type]) {
                renderer.Visible = !SpritesCleared;
            }
        }
    }

    private static void VivBeforeRender(Level self, Type[] types) {
        foreach (Type type in types) {
            foreach (Entity customSpinner in self.Tracker.Entities[type]) {
                customSpinner.UpdateComponentVisiblity();
                foreach (FieldInfo getter in VivSpinnerExtraComponentGetter) {
                    object obj = getter.GetValue(customSpinner);
                    if (obj is Entity e) {
                        e.Visible = !SpritesCleared;
                    }
                }
            }
        }
    }


    private static void UpdateComponentVisiblity(this Entity self) {
        foreach (Component component in self.Components) {
            component.Visible = !SpritesCleared;
        }
    }


    /* How DustStaticSpinner (in the following, call Dust) works:
     * DustStaticSpinner has a component DustGraphic called Sprite
     * DustGraphic has 2 parts to render, DustGraphic.Eyeballs and DustGraphic itself
     * Eyeballs will be added to scene
     * Eyeballs will render if Dust.Visible and DustGraphic.Visible (and itself is Visible)
     * DustGraphic itself is a component
     * If called by GameplayRenderer, it will render if Dust and DustGraphic are Visible
     * However, DustGraphic.Added also adds a component DustEdge to Dust, which has a field Action RenderDust = DustGraphic.Render
     * Entity DustEdges.BeforeUpdate will call every DustEdge's RenderDust when Dust and DustEdge are visible
     * So DustGraphic.Render will be called even if DustGraphic itself is invisible
     * 
     * So we need to make DustGraphic and DustEdge invisible, instead of just DustGraphic
     * We should not make Dust itself invivible
     */
}
