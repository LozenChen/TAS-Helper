

using Celeste.Mod.TASHelper.Utils;
using FMOD;
using FMOD.Studio;
using Force.DeepCloner;
using Force.DeepCloner.Helpers;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Celeste.Mod.SpeedrunTool.Extensions.CommonExtensions;
using TH = Celeste.Mod.TASHelper.TinySRT.TH_SaveLoadAction;

namespace Celeste.Mod.TASHelper.TinySRT;

// we want to use SRT as fewer as possible (to avoid wrong reference)
// so we copy almost everything to here

internal static class TH_MuteAudioUtils {
    public static readonly HashSet<string> RequireMuteAudioPaths = new HashSet<string> { "event:/game/general/strawberry_get", "event:/game/general/strawberry_laugh", "event:/game/general/strawberry_flyaway", "event:/game/general/seed_complete_main", "event:/game/general/key_get", "event:/game/general/cassette_get", "event:/game/05_mirror_temple/eyewall_destroy", "event:/char/badeline/boss_hug", "event:/char/badeline/boss_laser_fire" };

    public static readonly List<FMOD.Studio.EventInstance> RequireMuteAudios = new List<FMOD.Studio.EventInstance>();

    [Load]
    public static void Load() {
        On.FMOD.Studio.EventDescription.createInstance += EventDescriptionOnCreateInstance;
    }

    [Unload]
    public static void Unload() {
        On.FMOD.Studio.EventDescription.createInstance -= EventDescriptionOnCreateInstance;
    }

    public static RESULT EventDescriptionOnCreateInstance(On.FMOD.Studio.EventDescription.orig_createInstance orig, FMOD.Studio.EventDescription self, out FMOD.Studio.EventInstance instance) {
        RESULT result = orig(self, out instance);
        if (TH_StateManager.Instance.IsSaved && instance != null && self.getPath(out var path) == RESULT.OK && path != null && RequireMuteAudioPaths.Contains(path)) {
            RequireMuteAudios.Add(instance);
        }

        return result;
    }

    public static void AddAction() {
        TH.SafeAdd(null, delegate (Dictionary<Type, Dictionary<string, object>> _, Level level) {
            level.Entities.FindAll<SoundEmitter>().ForEach(delegate (SoundEmitter emitter) {
                emitter.Source.instance?.setVolume(0f);
            });
            foreach (FMOD.Studio.EventInstance requireMuteAudio in RequireMuteAudios) {
                requireMuteAudio.setVolume(0f);
            }

            RequireMuteAudios.Clear();
        }, delegate {
            RequireMuteAudios.Clear();
        });
    }
}

internal static class TH_StrawberryJamUtils {
    public static float currentOldFreezeTimer;

    public static float? savedOldFreezeTimer;

    public static float? loadOldFreezeTimer;

    public static readonly Lazy<MethodInfo> EngineUpdate = new Lazy<MethodInfo>(() => ModUtils.GetType("StrawberryJam2021", "Celeste.Mod.StrawberryJam2021.Entities.WonkyCassetteBlockController")?.GetMethodInfo("Engine_Update"));

    public static bool hooked;

    [Initialize]
    public static void Initialize() {
        EngineUpdate.Value?.IlHook((cursor, _) => {
            int localIndex = 0;
            if (cursor.TryGotoNext(MoveType.Before, (Instruction i) => i.MatchLdsfld<Engine>("FreezeTimer"), (Instruction i) => i.MatchStloc(out localIndex))) {
                cursor.Index++;
                cursor.Emit(OpCodes.Dup).Emit(OpCodes.Stsfld, typeof(TH_StrawberryJamUtils).GetFieldInfo(nameof(currentOldFreezeTimer)));
                if (cursor.TryGotoNext(MoveType.After, (Instruction i) => i.MatchLdloc(localIndex))) {
                    cursor.EmitDelegate<Func<float, float>>(RestoreOldFreezeTimer);
                    hooked = true;
                }
            }
        });
    }

    public static float RestoreOldFreezeTimer(float oldFreezeTimer) {
        float? num = loadOldFreezeTimer;
        if (num.HasValue) {
            float valueOrDefault = num.GetValueOrDefault();
            loadOldFreezeTimer = null;
            return valueOrDefault;
        }

        return oldFreezeTimer;
    }

    public static void AddSupport() {
        if (hooked) {
            TH.SafeAdd(delegate {
                savedOldFreezeTimer = currentOldFreezeTimer;
            }, delegate {
                loadOldFreezeTimer = savedOldFreezeTimer;
            }, delegate {
                savedOldFreezeTimer = null;
            });
        }
    }
}

internal static class TH_FrostHelperUtils {
    public static readonly Lazy<Type> AttachedDataHelperType = new Lazy<Type>(() => ModUtils.GetType("FrostHelper", "FrostHelper.Helpers.AttachedDataHelper"));

    public static readonly Lazy<Func<object, object[]>> GetAllData = new Lazy<Func<object, object[]>>(() => (Func<object, object[]>)(AttachedDataHelperType.Value?.GetMethodInfo("GetAllData")?.CreateDelegate(typeof(Func<object, object[]>))));

    public static readonly Lazy<Action<object, object[]>> SetAllData = new Lazy<Action<object, object[]>>(() => (Action<object, object[]>)(AttachedDataHelperType.Value?.GetMethodInfo("SetAllData")?.CreateDelegate(typeof(Action<object, object[]>))));

    public static void TH_CloneDataStore(object sourceObj, object clonedObj, DeepCloneState deepCloneState) {
        if (GetAllData.Value != null && SetAllData.Value != null) {
            object[] array = GetAllData.Value(sourceObj);
            if (array != null) {
                SetAllData.Value(clonedObj, array.DeepClone(deepCloneState));
            }
        }
    }

    public static void SupportFrostHelper() {
        if (!(AttachedDataHelperType.Value != null) || GetAllData.Value != null) {
            return;
        }

        MethodInfo setAttached = AttachedDataHelperType.Value.GetMethodInfo("SetAttached");
        if ((object)setAttached == null) {
            return;
        }

        Type genericCustomBoosterType = ModUtils.GetType("FrostHelper", "FrostHelper.Entities.Boosters.GenericCustomBooster");
        if ((object)genericCustomBoosterType == null) {
            return;
        }

        MethodInfo getBoosterThatIsBoostingPlayer = genericCustomBoosterType.GetMethodInfo("GetBoosterThatIsBoostingPlayer");
        if ((object)getBoosterThatIsBoostingPlayer == null) {
            return;
        }

        setAttached = setAttached.MakeGenericMethod(genericCustomBoosterType);
        TH.SafeAdd(delegate (Dictionary<Type, Dictionary<string, object>> values, Level level) {
            Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
            List<Entity> entities = level.Tracker.GetEntities<Player>();
            List<object> value3 = entities.Select((Entity player) => getBoosterThatIsBoostingPlayer.Invoke(null, new object[1] { player })).ToList();
            dictionary2["players"] = entities;
            dictionary2["boosters"] = value3;
            values[genericCustomBoosterType] = dictionary2.TH_DeepCloneShared();
        }, delegate (Dictionary<Type, Dictionary<string, object>> values, Level level) {
            Dictionary<string, object> dictionary = values[genericCustomBoosterType].TH_DeepCloneShared();
            if (dictionary.TryGetValue("players", out var value) && dictionary.TryGetValue("boosters", out var value2)) {
                List<Entity> list = value as List<Entity>;
                if (list != null) {
                    List<object> list2 = value2 as List<object>;
                    if (list2 != null) {
                        for (int i = 0; i < list.Count; i++) {
                            setAttached.Invoke(null, new object[2]
                            {
                                    list[i],
                                    list2[i]
                            });
                        }
                    }
                }
            }
        });
    }
}


internal static class TH_EventInstanceUtils {
    [Load]
    private static void Load() {
        On.FMOD.Studio.EventInstance.setParameterValue += EventInstanceOnsetParameterValue;
    }

    [Unload]
    private static void OnUnhook() {
        On.FMOD.Studio.EventInstance.setParameterValue -= EventInstanceOnsetParameterValue;
    }

    private static RESULT EventInstanceOnsetParameterValue(On.FMOD.Studio.EventInstance.orig_setParameterValue orig,
        EventInstance self, string name, float value) {
        RESULT result = orig(self, name, value);
        if (result == RESULT.OK) {
            self.SaveParameters(name, value);
        }

        return result;
    }
}


internal static class TH_EventInstanceExtensions {
    public static readonly ConditionalWeakTable<EventInstance, ConcurrentDictionary<string, float>> CachedParameters = new ConditionalWeakTable<EventInstance, ConcurrentDictionary<string, float>>();

    public static readonly ConditionalWeakTable<EventInstance, object> NeedManualClonedEventInstances = new ConditionalWeakTable<EventInstance, object>();

    public static readonly ConditionalWeakTable<EventInstance, object> CachedTimelinePositions = new ConditionalWeakTable<EventInstance, object>();

    public static EventInstance NeedManualClone(this EventInstance eventInstance) {
        NeedManualClonedEventInstances.Set(eventInstance, null);
        return eventInstance;
    }

    public static bool IsNeedManualClone(this EventInstance eventInstance) {
        return NeedManualClonedEventInstances.ContainsKey(eventInstance);
    }

    public static ConcurrentDictionary<string, float> GetSavedParameterValues(this EventInstance eventInstance) {
        if (!(eventInstance == null)) {
            return CachedParameters.GetOrCreateValue(eventInstance);
        }

        return null;
    }

    public static void SaveParameters(this EventInstance eventInstance, string param, float value) {
        if (param != null) {
            eventInstance.GetSavedParameterValues()[param] = value;
        }
    }

    public static int LoadTimelinePosition(this EventInstance eventInstance) {
        int num = 0;
        if (CachedTimelinePositions.TryGetValue(eventInstance, out var value)) {
            num = (int)value;
        }

        if (num > 0) {
            return num;
        }

        eventInstance.getTimelinePosition(out var position);
        return position;
    }

    public static void SaveTimelinePosition(this EventInstance eventInstance, int timelinePosition) {
        CachedTimelinePositions.Set(eventInstance, timelinePosition);
    }

    public static void CopyTimelinePosition(this EventInstance eventInstance, EventInstance otherEventInstance) {
        int num = otherEventInstance.LoadTimelinePosition();
        if (num > 0) {
            eventInstance.setTimelinePosition(num);
            eventInstance.SaveTimelinePosition(otherEventInstance.LoadTimelinePosition());
        }
    }

    public static EventInstance Clone(this EventInstance eventInstance) {
        string eventName = Audio.GetEventName(eventInstance);
        if (eventName.IsNullOrEmpty()) {
            return null;
        }

        EventInstance eventInstance2 = Audio.CreateInstance(eventName);
        if (eventInstance2 == null) {
            return null;
        }

        if (eventInstance.IsNeedManualClone()) {
            eventInstance2.NeedManualClone();
        }

        ConcurrentDictionary<string, float> savedParameterValues = eventInstance.GetSavedParameterValues();
        if (savedParameterValues != null) {
            foreach (KeyValuePair<string, float> item in savedParameterValues) {
                eventInstance2.setParameterValue(item.Key, item.Value);
            }
        }

        eventInstance2.CopyTimelinePosition(eventInstance);
        return eventInstance2;
    }

    public static void CopyParametersFrom(this EventInstance eventInstance, ConcurrentDictionary<string, float> parameters) {
        if (eventInstance == null || parameters == null) {
            return;
        }

        ConcurrentDictionary<string, float> concurrentDictionary = new ConcurrentDictionary<string, float>(eventInstance.GetSavedParameterValues());
        foreach (KeyValuePair<string, float> parameter2 in parameters) {
            eventInstance.setParameterValue(parameter2.Key, parameter2.Value);
        }

        foreach (KeyValuePair<string, float> item in concurrentDictionary) {
            if (!parameters.ContainsKey(item.Key) && eventInstance.getDescription(out var description) == RESULT.OK && description.getParameter(item.Key, out var parameter) == RESULT.OK) {
                eventInstance.setParameterValue(item.Key, parameter.defaultvalue);
            }
        }
    }
}