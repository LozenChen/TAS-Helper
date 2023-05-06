using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using System.Reflection;
using TAS.EverestInterop.Hitboxes;
using ChronoEntities = Celeste.Mod.ChronoHelper.Entities;
using VivEntities = VivHelper.Entities;

namespace Celeste.Mod.TASHelper.Entities;
internal static class SimplifiedSpinner {

    public static bool SpritesCleared => DebugRendered && TasHelperSettings.ClearSpinnerSprites;

    private static List<FieldInfo> CrysExtraComponentGetter;

    private static List<FieldInfo> VivSpinnerExtraComponentGetter;

    private static List<FieldInfo> ChronoSpinnerExtraComponentGetter;

    private static bool wasSpritesCleared = !SpritesCleared;

    private static bool AddingEntities = true;

    // sprites are created by e.g. AddSprites(), so they do not necessarily exist when load level

    private static bool Updated => !AddingEntities && wasSpritesCleared == SpritesCleared;
    public static void Load() {
        On.Monocle.Entity.DebugRender += PatchDebugRender;
        On.Monocle.EntityList.UpdateLists += OnLevelAddEntity;
        On.Celeste.Level.LoadLevel += OnLoadLevel;
    }

    public static void Unload() {
        On.Monocle.Entity.DebugRender -= PatchDebugRender;
        On.Monocle.EntityList.UpdateLists -= OnLevelAddEntity;
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
    }

    private static void CreateVivGetter() {
        VivSpinnerExtraComponentGetter = new() {
                typeof(VivEntities.CustomSpinner).GetField("border", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(VivEntities.CustomSpinner).GetField("filler", BindingFlags.NonPublic | BindingFlags.Instance)
            };
    }

    private static void CreateChronoGetter() {
        ChronoSpinnerExtraComponentGetter = new() {
                typeof(ChronoEntities.ShatterSpinner).GetField("border", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(ChronoEntities.ShatterSpinner).GetField("filler", BindingFlags.NonPublic | BindingFlags.Instance)
            };
    }
    public static void Initialize() {
        CrysExtraComponentGetter = new() {
            typeof(CrystalStaticSpinner).GetField("border", BindingFlags.NonPublic | BindingFlags.Instance),
            typeof(CrystalStaticSpinner).GetField("filler", BindingFlags.NonPublic | BindingFlags.Instance)
        };

        // this one must be at first
        typeof(Level).GetMethod("BeforeRender").IlHook((cursor, _) => {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Level>>(IlLevelBeforeRender);
        });

        if (ModUtils.FrostHelperInstalled) {
            typeof(Level).GetMethod("BeforeRender").IlHook((cursor, _) => {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Level>>(FrostBeforeRender);
            });
        }
        if (ModUtils.VivHelperInstalled) {
            CreateVivGetter();
            typeof(Level).GetMethod("BeforeRender").IlHook((cursor, _) => {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Level>>(VivBeforeRender);
            });
        }

        if (ModUtils.ChronoHelperInstalled) {
            CreateChronoGetter();
            typeof(Level).GetMethod("BeforeRender").IlHook((cursor, _) => {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Level>>(ChronoBeforeRender);
            });
        }

        if (ModUtils.BrokemiaHelperInstalled) {
            typeof(Level).GetMethod("LoadLevel").IlHook((cursor, _) => {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(TrackCassetteSpinner);
            });
            typeof(Level).GetMethod("BeforeRender").IlHook((cursor, _) => {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Level>>(BrokemiaBeforeRender);
            });
        }

        if (ModUtils.IsaGrabBagInstalled) {
            typeof(Level).GetMethod("LoadLevel").IlHook((cursor, _) => {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(TrackDreamSpinnerRenderer);
           });
            typeof(Level).GetMethod("BeforeRender").IlHook((cursor, _) => {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<Level>>(IsaGrabBagBeforeRender);
            });
        }

    }

    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        orig(self, playerIntro, isFromLoader);
        wasSpritesCleared = !SpritesCleared;
    }

    private static void OnLevelAddEntity(On.Monocle.EntityList.orig_UpdateLists orig, EntityList self) {
        if (self.Scene is Level) {
            AddingEntities |= self.ToAdd.Count > 0;
        }
        orig(self);
    }

    private static void IlLevelBeforeRender(Level self) {
        // here i assume all the components are always visible
        // if that's not the case, then this implementation has bug

        // all other hooks must be before this
        if (!Updated) {
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
            // we must set it here immediately, instead of setting this at e.g. Level.AfterRender
            // coz SpritesCleared may change during this period of time, in that case wasSpritesCleared will not detect this change
            wasSpritesCleared = SpritesCleared;
            AddingEntities = false;
        }
    }

    private static void FrostBeforeRender(Level self) {
        if (Updated) {
            return;
        }
        foreach (Entity customSpinner in self.Tracker.GetEntities<FrostHelper.CustomSpinner>()) {
            customSpinner.UpdateComponentVisiblity();
        }
        foreach (Entity renderer in self.Tracker.GetEntities<FrostHelper.SpinnerConnectorRenderer>()) {
            renderer.Visible = !SpritesCleared;
        }
        foreach (Entity renderer in self.Tracker.GetEntities<FrostHelper.SpinnerBorderRenderer>()) {
            renderer.Visible = !SpritesCleared;
        }
        foreach (Entity renderer in self.Tracker.GetEntities<FrostHelper.SpinnerDecoRenderer>()) {
            renderer.Visible = !SpritesCleared;
        }
    }

    private static void VivBeforeRender(Level self) {
        if (Updated) {
            return;
        }
        foreach (Entity customSpinner in self.Tracker.GetEntities<VivEntities.CustomSpinner>()) {
            customSpinner.UpdateComponentVisiblity();
            foreach (FieldInfo getter in VivSpinnerExtraComponentGetter) {
                object obj = getter.GetValue(customSpinner);
                if (obj != null) {
                    obj.SetFieldValue("Visible", !SpritesCleared);
                }
            }
        }
        // viv use Inherited(true) so all subclass of custom spinners are added to TrackedEntityTypes
        // so AnimatedSpinner is standalone, can't be fetched by track CustomSpinners
        foreach (Entity customSpinner in self.Tracker.GetEntities<VivEntities.AnimatedSpinner>()) {
            customSpinner.UpdateComponentVisiblity();
            foreach (FieldInfo getter in VivSpinnerExtraComponentGetter) {
                object obj = getter.GetValue(customSpinner);
                if (obj != null) {
                    obj.SetFieldValue("Visible", !SpritesCleared);
                }
            }
        }
        foreach (Entity customSpinner in self.Tracker.GetEntities<VivEntities.MovingSpinner>()) {
            customSpinner.UpdateComponentVisiblity();
            foreach (FieldInfo getter in VivSpinnerExtraComponentGetter) {
                object obj = getter.GetValue(customSpinner);
                if (obj != null) {
                    obj.SetFieldValue("Visible", !SpritesCleared);
                }
            }
        }
    }
    private static void ChronoBeforeRender(Level self) {
        if (Updated) {
            return;
        }
        foreach (Entity spinner in self.Tracker.GetEntities<ChronoEntities.ShatterSpinner>()) {
            spinner.UpdateComponentVisiblity();
            foreach (FieldInfo getter in ChronoSpinnerExtraComponentGetter) {
                object obj = getter.GetValue(spinner);
                if (obj != null) {
                    obj.SetFieldValue("Visible", !SpritesCleared);
                }
            }
        }
    }

    private static void TrackCassetteSpinner(Level self) {
        Type t = typeof(BrokemiaHelper.CassetteSpinner);
        if (!Tracker.TrackedEntityTypes.ContainsKey(t)) {
            Tracker.TrackedEntityTypes.Add(t, new List<Type>());
            Tracker.TrackedEntityTypes[t].Add(t);
        }
        if (!self.Tracker.Entities.ContainsKey(t)) {
            self.Tracker.Entities.Add(t, new List<Entity>());
        }
    }
    private static void BrokemiaBeforeRender(Level self) {
        if (Updated) {
            return;
        }
        foreach (Entity spinner in self.Tracker.GetEntities<BrokemiaHelper.CassetteSpinner>()) {
            spinner.UpdateComponentVisiblity();
            foreach (FieldInfo getter in CrysExtraComponentGetter) {
                object obj = getter.GetValue(spinner);
                if (obj != null) {
                    obj.SetFieldValue("Visible", !SpritesCleared);
                }
            }
        }
    }

    private static void TrackDreamSpinnerRenderer(Level self) {
        Type t = typeof(IsaGrabBag.DreamSpinnerRenderer);
        if (!Tracker.TrackedEntityTypes.ContainsKey(t)) {
            Tracker.TrackedEntityTypes.Add(t, new List<Type>());
            Tracker.TrackedEntityTypes[t].Add(t);
        }
        if (!self.Tracker.Entities.ContainsKey(t)) {
            self.Tracker.Entities.Add(t, new List<Entity>());
        }
    }

    private static void IsaGrabBagBeforeRender(Level self) {
        if (Updated) {
            return;
        }
        foreach (Entity renderer in self.Tracker.GetEntities<IsaGrabBag.DreamSpinnerRenderer>()) {
            renderer.Visible = !SpritesCleared;
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
     * We should not make Dust itself invivible, otherwise Dust.Render and thus our compensation will not be called 
     */

    private static void PatchDebugRender(On.Monocle.Entity.orig_DebugRender orig, Entity self, Camera camera) {
        if (!TasHelperSettings.SpinnerEnabled || SpinnerHelper.HazardType(self) == null) {
            orig(self, camera);
            return;
        }

#pragma warning disable CS8629
        RenderHelper.SpinnerColorIndex index = RenderHelper.CycleHitboxColorIndex(self, SpinnerHelper.GetOffset(self).Value, PlayerHelper.CameraPosition);
#pragma warning restore CS8629
        Color color = RenderHelper.GetSpinnerColor(index);
        // camera.Position is a bit different from CameraPosition, if you use CelesteTAS's center camera
        if (!SpinnerHelper.isLightning(self) && TasHelperSettings.EnableSimplifiedSpinner) {
            RenderHelper.DrawSpinnerCollider(self, color);
        }
        else {
            self.Collider.Render(camera, color * (self.Collidable ? 1f : HitboxColor.UnCollidableAlpha));
        }

        LoadRangeCountDownCameraTarget.DrawLoadRangeColliderCountdown(self, index);
    }

}