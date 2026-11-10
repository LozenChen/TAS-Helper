using Celeste.Mod.TASHelper.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.TASHelper.Gameplay;

public static partial class CassetteBlockHelper {

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

        public static void Initialize(Entity entity, out int currColorIndex, out int maxBeats, out int outBeatIndexMax, out float outTempoMult, out int vanillaIndexPeriod) {
            currColorIndex = -1;
            if (entity is not CassetteBlockManager cbm) {
                maxBeats = 1;
                outBeatIndexMax = 256;
                outTempoMult = 1f;
                vanillaIndexPeriod = 8;
                return;
            }
            currentIndex = cbm.currentIndex;
            beatTimer = cbm.beatTimer;
            beatIndex = cbm.beatIndex;
            outTempoMult = tempoMult = cbm.tempoMult;
            leadBeats = cbm.leadBeats;
            maxBeat = maxBeats = cbm.maxBeat;
            beatsPerTick = cbm.beatsPerTick;
            ticksPerSwap = cbm.ticksPerSwap;
            outBeatIndexMax = beatIndexMax = cbm.beatIndexMax;
            vanillaIndexPeriod = beatsPerTick * ticksPerSwap;
            foreach (CassetteBlock block in cbm.Scene.Tracker.GetEntities<CassetteBlock>()) {
                if (block.Activated) {
                    currColorIndex = block.Index;
                    break;
                }
            }
        }

        internal static Type jungle_SwingBlockType;

        [Initialize]
        private static void SupportJungleHelper() {
            jungle_SwingBlockType = ModUtils.GetType("JungleHelper", "Celeste.Mod.JungleHelper.Entities.SwingCassetteBlock");
        }

        private const int infiniteLoopPreventer = 2048;
        public static bool GetLoopCount(float advanceTime, out int count) {
            count = 0;
            float time = Engine.DeltaTime * tempoMult;
            // if tempoMult = 0, then infinite loop
            while (advanceTime > 0 && count < infiniteLoopPreventer) {
                advanceTime -= time;
                count++;
            }
            return advanceTime > -0.0001f;
        }

        public static bool UpdateLoop(int loop, ref Dictionary<int, List<int>> swapTime) {
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
        public static void Initialize(Entity entity, out int currColorIndex, out int maxBeat, out int beatIndexMax, out float maxBeatTimer) {
            currColorIndex = -1;
            if (ModUtils.GetType("StrawberryJam2021", "Celeste.Mod.StrawberryJam2021.StrawberryJam2021Module")?.GetPropertyValue<EverestModuleSession>("Session") is not { } session || session.GetFieldValue<bool>("CassetteBlocksDisabled") || entity.GetType() != CassetteBlockVisualizer.SJ_CBMType || MinorSimulator.minor_type is null) {
                disabled = true;
                maxBeat = maxBeats = 1;
                beatIndexMax = 500;
                maxBeatTimer = 10f;
                return;
            }
            else {
                disabled = false; // this line is necessary, as we may intialize it more than once, and the value of "CassetteBlocksDisabled" may change during this
            }

            CassetteBeatTimer = session.GetFieldValue<float>("CassetteBeatTimer");
            CassetteWonkyBeatIndex = session.GetFieldValue<int>("CassetteWonkyBeatIndex");
            MusicBeatTimer = session.GetFieldValue<float>("MusicBeatTimer");
            MusicWonkyBeatIndex = session.GetFieldValue<int>("MusicWonkyBeatIndex");
            beatIndexMax = maxBeats = entity.GetFieldValue<int>("maxBeats");
            beatIncrement = entity.GetFieldValue<float>("beatIncrement");
            maxBeatTimer = 60f * beatIncrement;
            maxBeat = barLength = entity.GetFieldValue<int>("barLength");
            beatLength = entity.GetFieldValue<int>("beatLength");
            minorSimulators = new();
            foreach (Entity minor_Entity in Engine.Scene.Tracker.Entities[MinorSimulator.minor_type]) {
                minorSimulators.Add(new MinorSimulator(minor_Entity));
            }
        }

        public static bool UpdateLoop(int loop, ref Dictionary<int, List<int>> swapTime) {
            swapTime = new();
            if (disabled) {
                return false;
            }
            for (int j = 0; j < barLength; j++) {
                swapTime[j] = new List<int>();
            }
            foreach (MinorSimulator minor in minorSimulators) {
                for (int j = minor.ControllerIndex * minorOffset; j < minor.ControllerIndex * minorOffset + minor.barLength; j++) {
                    swapTime[j] = new List<int>();
                }
            }

            float time = Engine.DeltaTime;
            for (int timeElapsed = 1; timeElapsed <= loop; timeElapsed++) {
                AdvanceMusic(ref swapTime, time, timeElapsed);
            }
            return true;
        }
        public static void AdvanceMusic(ref Dictionary<int, List<int>> swapTime, float time, int index) {
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

                swapTime[beatInBar].Add(index);
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
                minor.AdvanceMusic(ref swapTime, time, index);
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

            public void AdvanceMusic(ref Dictionary<int, List<int>> swapTime, float time, int index) {
                CassetteBeatTimer += time;
                if (!(CassetteBeatTimer >= beatIncrement)) {
                    return;
                }
                CassetteBeatTimer -= beatIncrement;
                int beatInBar = CassetteWonkyBeatIndex / (16 / beatLength) % barLength;
                swapTime[beatInBar + ControllerIndex * minorOffset].Add(index);
                CassetteWonkyBeatIndex = (CassetteWonkyBeatIndex + 1) % maxBeats;
            }
        }
    }

    public static class QMWonkyCassetteBlockControllerSimulator {

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

        public const int minorOffset = SJWonkyCassetteBlockControllerSimulator.minorOffset;
        public static void Initialize(Entity entity, out int currColorIndex, out int maxBeat, out int beatIndexMax, out float maxBeatTimer) {
            currColorIndex = -1;
            if (ModUtils.GetType("QuantumMechanics", "Celeste.Mod.QuantumMechanics.QuantumMechanicsModule")?.GetPropertyValue<EverestModuleSession>("Session") is not { } session || session.GetFieldValue<bool>("CassetteBlocksDisabled") || entity.GetType() != CassetteBlockVisualizer.QM_CBMType || MinorSimulator.minor_type is null) {
                disabled = true;
                maxBeat = maxBeats = 1;
                beatIndexMax = 500;
                maxBeatTimer = 10f;
                return;
            }
            else {
                disabled = false; // this line is necessary, as we may intialize it more than once, and the value of "CassetteBlocksDisabled" may change during this
            }

            CassetteBeatTimer = session.GetFieldValue<float>("CassetteBeatTimer");
            CassetteWonkyBeatIndex = session.GetFieldValue<int>("CassetteWonkyBeatIndex");
            MusicBeatTimer = session.GetFieldValue<float>("MusicBeatTimer");
            MusicWonkyBeatIndex = session.GetFieldValue<int>("MusicWonkyBeatIndex");
            beatIndexMax = maxBeats = entity.GetFieldValue<int>("maxBeats");
            beatIncrement = entity.GetFieldValue<float>("beatIncrement");
            maxBeatTimer = 60f * beatIncrement;
            maxBeat = barLength = entity.GetFieldValue<int>("barLength");
            beatLength = entity.GetFieldValue<int>("beatLength");
            minorSimulators = new();
            foreach (Entity minor_Entity in Engine.Scene.Tracker.Entities[MinorSimulator.minor_type]) {
                minorSimulators.Add(new MinorSimulator(minor_Entity));
            }
        }

        public static bool UpdateLoop(int loop, ref Dictionary<int, List<int>> swapTime) {
            swapTime = new();
            if (disabled) {
                return false;
            }
            for (int j = 0; j < barLength; j++) {
                swapTime[j] = new List<int>();
            }
            foreach (MinorSimulator minor in minorSimulators) {
                for (int j = minor.ControllerIndex * minorOffset; j < minor.ControllerIndex * minorOffset + minor.barLength; j++) {
                    swapTime[j] = new List<int>();
                }
            }

            float time = Engine.DeltaTime;
            for (int timeElapsed = 1; timeElapsed <= loop; timeElapsed++) {
                AdvanceMusic(ref swapTime, time, timeElapsed);
            }
            return true;
        }

        public static void AdvanceMusic(ref Dictionary<int, List<int>> swapTime, float time, int index) {
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

                swapTime[beatInBar].Add(index);
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
                minor.AdvanceMusic(ref swapTime, time, index);
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
                minor_type = ModUtils.GetType("QuantumMechanics", "Celeste.Mod.QuantumMechanics.Entities.WonkyMinorCassetteBlockController");
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

            public void AdvanceMusic(ref Dictionary<int, List<int>> swapTime, float time, int index) {
                CassetteBeatTimer += time;
                if (!(CassetteBeatTimer >= beatIncrement)) {
                    return;
                }
                CassetteBeatTimer -= beatIncrement;
                int beatInBar = CassetteWonkyBeatIndex / (16 / beatLength) % barLength;
                swapTime[beatInBar + ControllerIndex * minorOffset].Add(index);
                CassetteWonkyBeatIndex = (CassetteWonkyBeatIndex + 1) % maxBeats;
            }
        }
    }
}
