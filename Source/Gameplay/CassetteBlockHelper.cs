using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.TASHelper.Gameplay;
internal static class CassetteBlockHelper {

    public static bool Enabled => TasHelperSettings.EnableCassetteBlockHelper;

    [Load]
    public static void Load() {
        On.Celeste.Level.LoadLevel += OnLoadLevel;
        IL.Monocle.Engine.Update += ILEngineUpdate;
        IL.Celeste.Celeste.Freeze += ILCelesteFreeze;
    }


    [Unload]
    public static void Unload() {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
        IL.Monocle.Engine.Update -= ILEngineUpdate;
        IL.Celeste.Celeste.Freeze -= ILCelesteFreeze;
    }


    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader = false) {
        orig(level, playerIntro, isFromLoader);
        if (Enabled) {
            CasstteBlockVisualizer.AddToScene(level);
            CasstteBlockVisualizer.BuildBeatColors(level);
        }
    }

    private static void ILEngineUpdate(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        if (cursor.TryGotoNext(ins => ins.MatchLdsfld<Engine>("FreezeTimer"), ins => ins.MatchCall<Engine>("get_RawDeltaTime"))) {
            cursor.EmitDelegate(SJ_AdvanceCasstteBlockVisualizer);
        }
    }

    private static void ILCelesteFreeze(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        if (cursor.TryGotoNext(ins => ins.MatchCall<Engine>("get_Scene"), ins => ins.OpCode == OpCodes.Brfalse_S)) {
            cursor.EmitDelegate(Vanilla_AdvanceCasstteBlockVisualizer);
        }
    }

    private static void SJ_AdvanceCasstteBlockVisualizer() {
        if (Engine.Scene.Tracker.GetEntity<CasstteBlockVisualizer>() is { } visualizer && visualizer.cbmType == CasstteBlockVisualizer.CBMType.SJ) {
            visualizer.Update();
        }
    }

    private static void Vanilla_AdvanceCasstteBlockVisualizer() {
        if (Engine.Scene.Tracker.GetEntity<CasstteBlockVisualizer>() is { } visualizer && visualizer.cbmType == CasstteBlockVisualizer.CBMType.Vanilla) {
            CasstteBlockVisualizer.FreezeAdvanceTime();
        }
    }

    [Tracked(false)]
    public class CasstteBlockVisualizer : Entity {

        public static CasstteBlockVisualizer Instance;

        public int currColorIndex;

        public Entity cbm;

        public bool findCBM;

        public int maxBeat;

        public enum CBMType { Vanilla, SJ };

        public CBMType cbmType;
        public CasstteBlockVisualizer() {
            base.Tag = Tags.HUD;
            Depth = -10000; // update after CasstteBlockManager
            Position = new Vector2(1780f, 20f);
            Collidable = false;
            Visible = false;
            findCBM = false;
            currColorIndex = -1;
            TimeElapse = NearestTime = 0;
            maxBeat = 4;
            // some order of operation issues suck so the numbers we show are "not" correct. though you can have other understandings to make that correct
        }

        public static bool AddToScene(Level level) {
            Instance = new CasstteBlockVisualizer();
            level.Add(Instance);
            return true;
        }

        [Initialize]
        private static void Initialize() {
            // Test maps: vanilla 9A, SJ ShatterSong and GMHS, Spring Collab 2020 Expert CANADIAN, Into-the-Jungle maps
            SJ_CBMType = ModUtils.GetType("StrawberryJam2021", "Celeste.Mod.StrawberryJam2021.Entities.WonkyCassetteBlockController");
            SJ_CassetteBlockType = ModUtils.GetType("StrawberryJam2021", "Celeste.Mod.StrawberryJam2021.Entities.WonkyCassetteBlock");
            if (ModUtils.GetType("FrostHelper", "FrostHelper.CassetteTempoTrigger")?.GetMethodInfo("SetManagerTempo") is { } method) {
                method.HookAfter(SetStateChanged);
            }
            // todo: support JungleHelper cassette
        }

        internal static void BuildBeatColors(Level level) {
            // we assume cassetteblocks with same color always have same beats
            if (SJ_CassetteBlockType is not null) {
                beatColors = new();
                foreach (Entity cassetteBlock in level.Tracker.Entities[SJ_CassetteBlockType]) {
                    int[] OnAtBeats = cassetteBlock.GetFieldValue<int[]>("OnAtBeats");
                    int controllerIndex = cassetteBlock.GetFieldValue<int>("ControllerIndex");
                    Color color = (cassetteBlock as CassetteBlock).color;
                    foreach (int n in OnAtBeats) {
                        beatColors[n + controllerIndex * SJWonkyCassetteBlockControllerSimulator.minorOffset] = color;
                    }
                }
            }
        }

        internal static Type SJ_CBMType;

        internal static Type SJ_CassetteBlockType;

        public static Dictionary<int, Color> beatColors = new();

        private const int loop = 330; // contains two full cycles
        public override void Update() {
            if (Predictor.Core.InPredict) {
                return;
            }
            if (!findCBM) {
                if (Engine.Scene.Tracker.GetEntity<CassetteBlockManager>() is { } cbm) {
                    Visible = true;
                    this.cbm = cbm;
                    findCBM = true;
                    cbmType = CBMType.Vanilla;
                }
                else if (SJ_CBMType != null && Engine.Scene.Tracker.Entities.TryGetValue(SJ_CBMType, out List<Entity> list) && list.Count > 0) {
                    Visible = true;
                    this.cbm = list[0];
                    findCBM = true;
                    cbmType = CBMType.SJ;
                    this.Tag |= Tags.TransitionUpdate;
                }
                else {
                    Visible = false;
                    RemoveSelf();
                    return;
                }
            }
            if (cbm is null || cbm.Scene != Engine.Scene) { // always check this, so in case there's a teleport or something... so the cbm is actually removed from scene (but we still have a reference to it so it's not cleared by GC)
                RemoveSelf();
                Visible = false;
                return;
            }
            Instance = this;
            base.Update();
            TimeElapse++;
            bool timerateGotoMonitor = Math.Abs(Engine.DeltaTime - LastDeltaTime) > 0.0001f;
            bool normalGotoMonitor = false;
            if (!timerateGotoMonitor && TimeElapse < NearestTime) {
                // nothing happens
            }
            else if (!timerateGotoMonitor && TimeElapse >= NearestTime) {
                bool found = false;
                foreach (KeyValuePair<int, List<int>> pair in ColorSwapTime) {
                    List<int> list = pair.Value;
                    if (list.IsNotNullOrEmpty() && list[0] == TimeElapse) {
                        found = true;
                        list.RemoveAt(0);
                        currColorIndex = pair.Key; // only makes sense to vanilla
                        if (list.IsNullOrEmpty()) {
                            normalGotoMonitor = true;
                        }
                    }
                }
                if (!found) {
                    normalGotoMonitor = true;
                }
                else if (!normalGotoMonitor) {
                    NearestTime = loop;
                    foreach (List<int> list2 in ColorSwapTime.Values) {
                        if (list2.IsNotNullOrEmpty()) {
                            NearestTime = Math.Min(NearestTime, list2[0]);
                        }
                    }
                }
            }

            if (timerateGotoMonitor || normalGotoMonitor || stateChanged) {
                stateChanged = false;
                LastDeltaTime = Engine.DeltaTime;
                bool hasData;
                if (cbmType == CBMType.Vanilla) {
                    VanillaCasstteBlockManagerSimulator.Initialize(cbm, out currColorIndex, out maxBeat);
                    hasData = VanillaCasstteBlockManagerSimulator.UpdateLoop(2 * loop, out ColorSwapTime);
                }
                else {
                    SJWonkyCassetteBlockControllerSimulator.Initialize(cbm, out currColorIndex, out maxBeat);
                    hasData = SJWonkyCassetteBlockControllerSimulator.UpdateLoop(2 * loop, out ColorSwapTime);
                }
                if (hasData) {
                    TimeElapse = 0;
                    NearestTime = loop;
                    foreach (List<int> list in ColorSwapTime.Values) {
                        if (list.IsNotNullOrEmpty()) {
                            NearestTime = Math.Min(NearestTime, list[0]);
                        }
                    }
                }
                else {
                    TimeElapse = 0;
                    NearestTime = 0;
                    ColorSwapTime.Clear();
                }
            }
        }

        public int TimeElapse = 0;

        public int NearestTime = 0;

        public float LastDeltaTime = 0f;

        public static Dictionary<int, List<int>> ColorSwapTime = new();

        private static Vector2 textOffset = new Vector2(40f, -10f);

        public static bool stateChanged = false;

        public static void SetStateChanged() {
            stateChanged = true;
        }

        public static void FreezeAdvanceTime() {
            stateChanged = true;
            // the time argument is unnecessary
            // as we will goto sync with cbm (on which the freeze advance time has been applied)
            // yeah maybe we can have better ways, but this is easy and safe to implement, also actually not very expansive
        }
        public override void Render() {
            if (!DebugRendered) {
                return;
            }
            Vector2 pos = Position;
            if (cbmType == CBMType.Vanilla) {
                for (int i = 0; i < maxBeat; i++) {
                    Monocle.Draw.Rect(pos, 20f, 20f, VanillaColorChooser(i, i == currColorIndex));
                    Monocle.Draw.HollowRect(pos - Vector2.One, 22f, 22f, Color.Black);
                    if (ColorSwapTime[i] is { } list && list.Count > 0) {
                        Message.RenderMessage((list[0] - TimeElapse).ToString(), pos + textOffset, Vector2.Zero, Vector2.One * 0.7f);
                    }

                    pos += Vector2.UnitY * 40f;
                }
            }
            else {
                pos += textOffset;
                int highestIndex = 0;
                HashSet<int> minorControllers = new();
                foreach (int index in ColorSwapTime.Keys) {
                    if (Math.Abs(index) > SJWonkyCassetteBlockControllerSimulator.minorOffset / 2) {
                        int controllerIndex = (int)Math.Round((float)index / (float)SJWonkyCassetteBlockControllerSimulator.minorOffset);
                        // we know these are added later, so highestIndex is already calculated
                        minorControllers.Add(controllerIndex);
                        if (ColorSwapTime[index] is { } list && list.Count > 0) {
                            int y = index - controllerIndex * SJWonkyCassetteBlockControllerSimulator.minorOffset;
                            Message.RenderMessage(y.ToString(), pos + new Vector2(-27f - 160f * (controllerIndex - 1), 40f * (highestIndex + y + 3)), Vector2.UnitX * 0.5f, Vector2.One * 0.7f, 2f, Color.OrangeRed, Color.Black);
                            Message.RenderMessage((list[0] - TimeElapse).ToString(), pos + new Vector2(-160f * (controllerIndex - 1), 40f * (highestIndex + y + 3)), Vector2.Zero, Vector2.One * 0.7f);
                            if (beatColors.TryGetValue(index, out Color color)) {
                                Vector2 target = pos + new Vector2(-160f * controllerIndex + 80f, 12f + 40f * (highestIndex + y + 3));
                                Monocle.Draw.Rect(target, 20f, 20f, color);
                                Monocle.Draw.HollowRect(target - Vector2.One, 22f, 22f, Color.Black);
                            }
                        }
                    }
                    else if (ColorSwapTime[index] is { } list && list.Count > 0) {
                        Message.RenderMessage(index.ToString(), pos + new Vector2(-27f, 40f * index), Vector2.UnitX * 0.5f, Vector2.One * 0.7f, 2f, Color.Orange, Color.Black);
                        Message.RenderMessage((list[0] - TimeElapse).ToString(), pos + new Vector2(0, 40f * index), Vector2.Zero, Vector2.One * 0.7f);
                        highestIndex = Math.Max(highestIndex, index);
                        if (beatColors.TryGetValue(index, out Color color)) {
                            Vector2 target = pos + new Vector2(-80f, 12f + 40f * index);
                            Monocle.Draw.Rect(target, 20f, 20f, color);
                            Monocle.Draw.HollowRect(target - Vector2.One, 22f, 22f, Color.Black);
                        }
                    }
                }
                if (minorControllers.IsNotEmpty()) {
                    Message.RenderMessage("main", pos + new Vector2(-20f, 40f * (highestIndex + 1)), Vector2.UnitX * 0.5f, Vector2.One * 0.7f, 2f, Color.Orange, Color.Black);
                }
                foreach (int controllerIndex in minorControllers) {
                    Message.RenderMessage(minorControllers.Count > 1 ? $"minor {controllerIndex}" : "minor", pos + new Vector2(140f - 160f * controllerIndex, 40f * (highestIndex + 2)), Vector2.UnitX * 0.5f, Vector2.One * 0.7f, 2f, Color.OrangeRed, Color.Black);
                }
            }
        }

        public static Color VanillaColorChooser(int index, bool notDark = true) {
            return notDark ? index switch {
                0 => Calc.HexToColor("49aaf0"),
                1 => Calc.HexToColor("f049be"),
                2 => Calc.HexToColor("fcdc3a"),
                3 => Calc.HexToColor("38e04e"),
                _ => Color.Red,
            } : index switch {
                0 => Calc.HexToColor("1d539b"),
                1 => Calc.HexToColor("60237a"),
                2 => Calc.HexToColor("646b25"),
                3 => Calc.HexToColor("166d32"),
                _ => Color.Red,
            };
        }
    }


    public static class VanillaCasstteBlockManagerSimulator {

        private static int currentIndex;

        private static float beatTimer;

        private static int beatIndex;

        private static float tempoMult;

        private static int leadBeats;

        private static int maxBeat;

        private static int beatsPerTick;

        private static int ticksPerSwap;

        private static int beatIndexMax;

        public static void Initialize(Entity entity, out int currColorIndex, out int maxBeats) {
            currColorIndex = -1;
            maxBeats = 1;
            if (entity is not CassetteBlockManager cbm) {
                return;
            }
            currentIndex = cbm.currentIndex;
            beatTimer = cbm.beatTimer;
            beatIndex = cbm.beatIndex;
            tempoMult = cbm.tempoMult;
            leadBeats = cbm.leadBeats;
            maxBeat = maxBeats = cbm.maxBeat;
            beatsPerTick = cbm.beatsPerTick;
            ticksPerSwap = cbm.ticksPerSwap;
            beatIndexMax = cbm.beatIndexMax;
            foreach (CassetteBlock block in cbm.Scene.Tracker.GetEntities<CassetteBlock>()) {
                if (block.Activated) {
                    currColorIndex = block.Index;
                    break;
                }
            }
        }

        private static Type jungle_SwingBlockType;

        [Initialize]
        private static void SupportJungleHelper() {
            jungle_SwingBlockType = ModUtils.GetType("JungleHelper", "Celeste.Mod.JungleHelper.Entities.SwingCassetteBlock");
        }

        public static bool UpdateLoop(int loop, out Dictionary<int, List<int>> swapTime) {
            swapTime = new();
            for (int j = 0; j < maxBeat; j++) {
                swapTime[j] = new List<int>();
            }
            float time = Engine.DeltaTime * tempoMult;
            bool jungleFlag = jungle_SwingBlockType is not null && Engine.Scene.Tracker.Entities[jungle_SwingBlockType].Count > 0;

            for (int timeElapsed = 1; timeElapsed <= loop; timeElapsed++) {
                beatTimer += time;
                if (jungleFlag) {
                    float gate = 1f / 6f * ((beatIndex % 2 == 0) ? 1.32f : 0.68f);
                    if (beatTimer < gate) {
                        continue;
                    }
                    else {
                        beatTimer -= gate;
                    }
                }
                else {
                    if (beatTimer < 1f / 6f) {
                        continue;
                    }
                    else {
                        beatTimer -= 1f / 6f;
                    }
                }
                beatIndex++;
                beatIndex %= beatIndexMax;
                if (beatIndex % (beatsPerTick * ticksPerSwap) == 0) {
                    currentIndex++;
                    currentIndex %= maxBeat;
                    // SetActiveIndex(currentIndex);
                    swapTime[currentIndex].Add(timeElapsed);
                }
                /*
                else {
                    // before swap (i.e. collidable change), there's a short time where cassette blocks shift up or down
                    if ((beatIndex + 1) % (beatsPerTick * ticksPerSwap) == 0) {
                        SetWillActivate((currentIndex + 1) % maxBeat);
                    }
                }
                */
                if (leadBeats > 0) {
                    leadBeats--;
                    if (leadBeats == 0) {
                        beatIndex = 0;
                    }
                }
            }
            return true;
        }
    }


    public static class SJWonkyCassetteBlockControllerSimulator {

        public static bool disabled = false;

        public static float CassetteBeatTimer;

        public static int CassetteWonkyBeatIndex;

        public static float MusicBeatTimer;

        public static int MusicWonkyBeatIndex;

        public static int maxBeats;

        public static float beatIncrement;

        public static int barLength;

        public static int beatLength;

        public static List<MinorSimulator> minorSimulators;

        public const int minorOffset = 64;
        public static void Initialize(Entity entity, out int currColorIndex, out int maxBeat) {
            currColorIndex = -1;
            maxBeat = maxBeats = 1;
            if (ModUtils.GetType("StrawberryJam2021", "Celeste.Mod.StrawberryJam2021.StrawberryJam2021Module")?.GetPropertyValue<EverestModuleSession>("Session") is not { } session || session.GetFieldValue<bool>("CassetteBlocksDisabled") || entity.GetType() != CasstteBlockVisualizer.SJ_CBMType || MinorSimulator.minor_type is null) {
                disabled = true;
                return;
            }
            else {
                disabled = false; // this line is necessary, as we may intialize it more than once, and the value of "CassetteBlocksDisabled" may change during this
            }

            CassetteBeatTimer = session.GetFieldValue<float>("CassetteBeatTimer");
            CassetteWonkyBeatIndex = session.GetFieldValue<int>("CassetteWonkyBeatIndex");
            MusicBeatTimer = session.GetFieldValue<float>("MusicBeatTimer");
            MusicWonkyBeatIndex = session.GetFieldValue<int>("MusicWonkyBeatIndex");
            maxBeats = entity.GetFieldValue<int>("maxBeats");
            beatIncrement = entity.GetFieldValue<float>("beatIncrement");
            maxBeat = barLength = entity.GetFieldValue<int>("barLength");
            beatLength = entity.GetFieldValue<int>("beatLength");
            minorSimulators = new();
            foreach (Entity minor_Entity in Engine.Scene.Tracker.Entities[MinorSimulator.minor_type]) {
                minorSimulators.Add(new MinorSimulator(minor_Entity));
            }
        }

        public static bool UpdateLoop(int loop, out Dictionary<int, List<int>> swapTime) {
            swapTime = new();
            swapTimes = new();
            if (disabled) {
                return false;
            }
            for (int j = 0; j < barLength; j++) {
                swapTimes[j] = new List<int>();
            }
            foreach (MinorSimulator minor in minorSimulators) {
                for (int j = minor.ControllerIndex * minorOffset; j < minor.ControllerIndex * minorOffset + minor.barLength; j++) {
                    swapTimes[j] = new List<int>();
                }
            }

            float time = Engine.DeltaTime;
            for (int timeElapsed = 1; timeElapsed <= loop; timeElapsed++) {
                AdvanceMusic(time, timeElapsed);
            }
            swapTime = TinySRT.TH_DeepClonerUtils.TH_DeepCloneShared<Dictionary<int, List<int>>>(swapTimes);
            return true;
        }

        private static Dictionary<int, List<int>> swapTimes = new();
        public static void AdvanceMusic(float time, int index) {
            // SJ casstteblocks are a bit different, controllers give different beats, and cassetteblocks determine if they should activate depending on their OnAtBeats data (instead of just color/index)
            // moreover, cassetteblocks have controller index to determine which controller they should follow, controller index = 0 is the main one i guess
            // different controller can have different parameters, but minor is dominated by main anyway
            if (disabled) {
                return;
            }

            CassetteBeatTimer += time;
            bool synchronizeMinorControllers = false;
            if (CassetteBeatTimer >= beatIncrement) {
                CassetteBeatTimer -= beatIncrement;

                int num = (CassetteWonkyBeatIndex + 1) % maxBeats;
                int beatInBar = CassetteWonkyBeatIndex / (16 / beatLength) % barLength;
                int nextBeatInBar = num / (16 / beatLength) % barLength;

                swapTimes[beatInBar].Add(index);
                /*
                foreach (WonkyCassetteBlock wonkyBlock in enumerable) {
                    if (wonkyBlock.ControllerIndex == 0) {
                        wonkyBlock.Activated = wonkyBlock.OnAtBeats.Contains(beatInBar);
                        if (wonkyBlock.OnAtBeats.Contains(nextBeatInBar) != wonkyBlock.Activated && beatIncrementsNext) {
                            wonkyBlock.WillToggle();
                        }
                    }
                }
                */
                CassetteWonkyBeatIndex = (CassetteWonkyBeatIndex + 1) % maxBeats;
                if (nextBeatInBar == 0 && beatInBar != 0) {
                    synchronizeMinorControllers = true;
                }
            }

            MusicBeatTimer += time;
            if (MusicBeatTimer >= beatIncrement) {
                MusicBeatTimer -= beatIncrement;
                MusicWonkyBeatIndex = (MusicWonkyBeatIndex + 1) % maxBeats;
            }

            foreach (MinorSimulator minor in minorSimulators) {
                if (synchronizeMinorControllers) {
                    minor.Synchronize(time, CassetteBeatTimer);
                }
                minor.AdvanceMusic(time, index);
            }
        }

        public class MinorSimulator {
            public int barLength;

            public int beatLength;

            public int ControllerIndex;

            public int CassetteWonkyBeatIndex;

            public float CassetteBeatTimer;

            private float beatIncrement;

            private float beatDelta;

            private int maxBeats;

            [Initialize]
            public static void Initialize() {
                minor_type = ModUtils.GetType("StrawberryJam2021", "Celeste.Mod.StrawberryJam2021.Entities.WonkyMinorCassetteBlockController");
            }

            internal static Type minor_type;
            public MinorSimulator(Entity entity) {
                if (minor_type is null || entity.GetType() != minor_type) {
                    throw new Exception("Bad Argument");
                }
                barLength = entity.GetFieldValue<int>("barLength");
                beatLength = entity.GetFieldValue<int>("beatLength");
                ControllerIndex = entity.GetFieldValue<int>("ControllerIndex");
                CassetteWonkyBeatIndex = entity.GetFieldValue<int>("CassetteWonkyBeatIndex");
                CassetteBeatTimer = entity.GetFieldValue<float>("CassetteBeatTimer");
                beatIncrement = entity.GetFieldValue<float>("beatIncrement");
                beatDelta = entity.GetFieldValue<float>("beatDelta");
                maxBeats = entity.GetFieldValue<int>("maxBeats");
            }

            public void Synchronize(float time, float parentCassetteBeatTimer) {
                CassetteWonkyBeatIndex = 0;
                CassetteBeatTimer = beatDelta + (parentCassetteBeatTimer - time);
            }

            public void AdvanceMusic(float time, int index) {
                CassetteBeatTimer += time;
                if (!(CassetteBeatTimer >= beatIncrement)) {
                    return;
                }
                CassetteBeatTimer -= beatIncrement;
                int beatInBar = CassetteWonkyBeatIndex / (16 / beatLength) % barLength;
                swapTimes[beatInBar + ControllerIndex * minorOffset].Add(index);
                CassetteWonkyBeatIndex = (CassetteWonkyBeatIndex + 1) % maxBeats;
            }
        }
    }
}