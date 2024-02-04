using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.RuntimeDetour;
using System.Reflection;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;
internal static class SimplifiedSpinner {

    // Tas mod's UnloadedRoomHitbox also draws spinner textures, but we do not plan to clear them
    // some room like Farewell [c-alt-01], adjacent rooms can have spinner in same position! in that case, it may seem that sprite is not cleared
    public static bool SpritesCleared => DebugRendered && TasHelperSettings.ClearSpinnerSprites;

    private static List<FieldInfo> CrysExtraComponentGetter;

    private static List<FieldInfo> VivSpinnerExtraComponentGetter;

    private static List<FieldInfo> ChronoSpinnerExtraComponentGetter;

    private static bool wasSpritesCleared = !SpritesCleared;

    private static bool NeedClearSprites = true;

    // sprites are created by e.g. AddSprites(), so they do not necessarily exist when load level

    private static bool NotUpdated => NeedClearSprites || wasSpritesCleared != SpritesCleared;

    private static readonly List<Action<Level>> ClearSpritesAction = new();

    [Load]
    public static void Load() {
        // hook after CelesteTAS.CycleHitboxColor's hook
        using (new DetourContext { After = new List<string> { "*", "CelesteTAS-EverestInterop" }, ID = "TAS Helper SimplifiedSpinner" }) {
            // CelesteTAS.HitboxOptimized also hooks this, and it'll early return if entity is not in the camera
            // so we need to be after HitboxOptimized hook, which already uses After = {"*"}, so we need even more configs
            On.Monocle.Entity.DebugRender += PatchDebugRender;
        }
        On.Celeste.Level.LoadLevel += OnLoadLevel;
    }

    [Unload]
    public static void Unload() {
        On.Monocle.Entity.DebugRender -= PatchDebugRender;
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
    }

    [Initialize]
    public static void Initialize() {
        typeof(Level).GetMethod("BeforeRender").IlHook((cursor, _) => {
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

        if (ModUtils.GetType("ChronoHelper", "Celeste.Mod.ChronoHelper.Entities.ShatterSpinner") is { } chronoSpinnerType) {
            ChronoSpinnerExtraComponentGetter = new() {
                chronoSpinnerType.GetField("border", BindingFlags.NonPublic | BindingFlags.Instance),
                chronoSpinnerType.GetField("filler", BindingFlags.NonPublic | BindingFlags.Instance)
            };
            ClearSpritesAction.Add(self => {
                foreach (Entity spinner in self.Tracker.Entities[chronoSpinnerType]) {
                    spinner.UpdateComponentVisiblity();
                    foreach (FieldInfo getter in ChronoSpinnerExtraComponentGetter) {
                        object obj = getter.GetValue(spinner);
                        if (obj != null) {
                            obj.SetFieldValue("Visible", !SpritesCleared);
                        }
                    }
                }
            });
            OnCreateSprites(chronoSpinnerType);
        }

        if (ModUtils.GetType("BrokemiaHelper", "BrokemiaHelper.CassetteSpinner") is { } cassetteSpinnerType) { // we use this as a mod version check
            LevelExtensions.AddToTracker(cassetteSpinnerType);
            ClearSpritesAction.Add(self => {
                if (!self.Tracker.Entities.ContainsKey(cassetteSpinnerType)) {
                    // there's report that here's a KeyNotFoundException
                    // https://discord.com/channels/403698615446536203/754495709872521287/1180852881369350295
                    // though it never happens for me
                    // don't know why this would happen
                    // anyway, we add a redundant check
                    self.Tracker.Entities.Add(cassetteSpinnerType, new List<Entity>());
                }
                foreach (Entity spinner in self.Tracker.Entities[cassetteSpinnerType]) {
                    spinner.UpdateComponentVisiblity();
                    foreach (FieldInfo getter in CrysExtraComponentGetter) {
                        object obj = getter.GetValue(spinner);
                        if (obj != null) {
                            obj.SetFieldValue("Visible", !SpritesCleared);
                        }
                    }
                }
            });
            // CreateSprites inherited from Crys spinner, so no need to hook
        }

        if (ModUtils.GetType("IsaGrabBag", "Celeste.Mod.IsaGrabBag.DreamSpinnerRenderer") is { } dreamSpinnerRendererType) {
            LevelExtensions.AddToTracker(dreamSpinnerRendererType);
            ClearSpritesAction.Add(self => {
                if (!self.Tracker.Entities.ContainsKey(dreamSpinnerRendererType)) {
                    self.Tracker.Entities.Add(dreamSpinnerRendererType, new List<Entity>());
                }
                foreach (Entity renderer in self.Tracker.Entities[dreamSpinnerRendererType]) {
                    renderer.Visible = !SpritesCleared;
                }
            });
            EOF(dreamSpinnerRendererType.GetConstructor(Type.EmptyTypes));
        }

        void EOF(MethodBase method) {
            method.IlHook((cursor, _) => {
                cursor.Goto(cursor.Instrs.Count - 1);
                cursor.EmitDelegate(CallNeedClearSprites);
            });
        }

        void OnCreateSprites(Type type) {
            EOF(type.GetMethod("CreateSprites", BindingFlags.NonPublic | BindingFlags.Instance));
        }
    }

    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        orig(self, playerIntro, isFromLoader);
        wasSpritesCleared = !SpritesCleared;
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
                if (obj != null) {
                    obj.SetFieldValue("Visible", !SpritesCleared);
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
                    if (obj != null) {
                        obj.SetFieldValue("Visible", !SpritesCleared);
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

    private static void PatchDebugRender(On.Monocle.Entity.orig_DebugRender orig, Entity self, Camera camera) {
        if (!TasHelperSettings.Enabled || !self.isHazard()) {
            orig(self, camera);
            return;
        }

        SpinnerRenderHelper.SpinnerColorIndex index = SpinnerRenderHelper.GetSpinnerColorIndex(self, true);
        Color color = SpinnerRenderHelper.GetSpinnerColor(index);
        bool collidable = SpinnerCalculateHelper.GetCollidable(self);

        int width = camera.Viewport.Width;
        int height = camera.Viewport.Height;
        Rectangle bounds = new((int)camera.Left - width / 2, (int)camera.Top - height / 2, width * 2, height * 2);
        if (self.Right < bounds.Left || self.Left > bounds.Right || self.Top > bounds.Bottom ||
            self.Bottom < bounds.Top) {
            // skip part of render
        }
        else {
            if (TasHelperSettings.EnableSimplifiedSpinner && !self.isLightning()) {
                ActualCollideHitboxDelegatee.DrawLastFrameHitbox(!TasHelperSettings.ApplyActualCollideHitboxForSpinner, self, camera, color, collidable, SpinnerRenderHelper.DrawSpinnerCollider);
            }
            else if (TasHelperSettings.EnableSimplifiedLightning && self.isLightning()) {
                ActualCollideHitboxDelegatee.DrawLastFrameHitbox(!TasHelperSettings.ApplyActualCollideHitboxForLightning, self, camera, color, collidable, SimplifiedLightning.DrawOutline);
            }
            else {
                self.Collider.Render(camera, color * (collidable ? 1f : HitboxColor.UnCollidableAlpha));
            }
        }
        LoadRangeCollider.Draw(self);
        Countdown.Draw(self, index, collidable);
    }
}