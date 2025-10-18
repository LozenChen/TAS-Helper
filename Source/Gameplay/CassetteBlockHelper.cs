using Celeste.Mod.TASHelper.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.TASHelper.Gameplay;



public static class CassetteBlockHelper {

    public static bool Enabled => TasHelperSettings.EnableCassetteBlockHelper;

    public static bool ShowExtraInfo => TasHelperSettings.CassetteBlockHelperShowExtraInfo;

    public static void OnEnabledChanged() {
        if (Engine.Scene is Level level) {
            if (Enabled) {
                if (level.Tracker.GetEntity<CassetteBlockVisualizer>() is null) {
                    CassetteBlockVisualizer.AddToScene(level);
                    CassetteBlockVisualizer.BuildBeatColors(level);
                }
            }
        }
    }

    public enum Alignments { TopRight, BottomRight, TopLeft, BottomLeft, None };
    // the difference between TopRight and None :
    // if None, then we just use DefaultPosition (which can be modified by user)
    // if TopRight, we will ignore DefaultPosition but use TopRightInScreen instead

    public static Alignments Alignment => TasHelperSettings.CassetteBlockInfoAlignment;

    [Load]
    private static void Load() {
        IL.Monocle.Engine.Update += ILEngineUpdate;
        IL.Celeste.Celeste.Freeze += ILCelesteFreeze;
    }


    [Unload]
    private static void Unload() {
        IL.Monocle.Engine.Update -= ILEngineUpdate;
        IL.Celeste.Celeste.Freeze -= ILCelesteFreeze;
    }

    [LoadLevel]
    private static void OnLoadLevel(Level level) {
        if (Enabled) {
            CassetteBlockVisualizer.AddToScene(level);
            CassetteBlockVisualizer.BuildBeatColors(level);
        }
    }

    private static void ILEngineUpdate(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        if (cursor.TryGotoNext(ins => ins.MatchLdsfld<Engine>("FreezeTimer"), ins => ins.MatchCall<Engine>("get_RawDeltaTime"))) {
            cursor.EmitDelegate(SJ_AdvanceCasstteBlockVisualizer);
        }
    }

    private static void ILCelesteFreeze(ILContext il) {
        // XaphanHelper.Managers.TimeManager has a hook on Celeste.Freeze, which completely skips the orig method
        // so our hook fails
        ILCursor cursor = new ILCursor(il);
        if (cursor.TryGotoNext(ins => ins.MatchCall<Engine>("get_Scene"), ins => ins.OpCode == OpCodes.Brfalse_S)) {
            cursor.Index += 2;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(Vanilla_AdvanceCasstteBlockVisualizer);
        }
    }

    private static void SJ_AdvanceCasstteBlockVisualizer() {
        // we handle freeze frames using the same way as the corresponding cassette block manager
        if (Engine.Scene.Tracker.GetEntity<CassetteBlockVisualizer>() is { } visualizer && (visualizer.cbmType == CassetteBlockVisualizer.CBMType.SJ || visualizer.cbmType == CassetteBlockVisualizer.CBMType.QM)) {
            visualizer.Update();
        }
    }

    private static void Vanilla_AdvanceCasstteBlockVisualizer(float time) {
        if (Engine.Scene.Tracker.GetEntity<CassetteBlockVisualizer>() is { } visualizer && visualizer.cbmType == CassetteBlockVisualizer.CBMType.Vanilla) {
            CassetteBlockVisualizer.FreezeAdvanceTime(visualizer, time);
        }
    }

    [Tracked(false)]
    public class CassetteBlockVisualizer : Entity {

        public static CassetteBlockVisualizer Instance;

        public int currColorIndex;

        public Entity cbm;

        public bool findCBM;

        public int maxBeat;

        public int beatIndexMax;

        public int beatIndex;

        public float beatTimer;

        public float tempoMult;

        public float maxBeatTimer;

        public List<Tuple<int, int, int, float, float>> minorControllerData = new();

        public enum CBMType { Vanilla, SJ, QM, None };

        public CBMType cbmType;

        public static Vector2 PositionWithoutAlignment = new Vector2(1780f, 20f);

        private static readonly Vector2 TopRightInScreen = new Vector2(1780f, 20f);

        public static bool needReAlignment = true;

        public CassetteBlockVisualizer() {
            base.Tag = Tags.HUD;
            Depth = -10000; // update after CasstteBlockManager
            Position = PositionWithoutAlignment;
            Collidable = false;
            Visible = false;
            findCBM = false;
            currColorIndex = -1;
            TimeElapse = NearestTime = 0;
            maxBeat = 4;
            beatIndexMax = 256;
            beatIndex = 0;
            beatTimer = 0f;
            tempoMult = 1f;
            maxBeatTimer = 1f / 6f;
            cbmType = CBMType.None;
            // some order of operation issues suck so the numbers we show are "not" correct. though you can have other understandings to make that correct
        }

        public static bool AddToScene(Level level) {
            if (Instance is not null) {
                Instance.RemoveSelf();
                Instance.Visible = Instance.Active = false;
            }
            Instance = new CassetteBlockVisualizer();
            level.Add(Instance);
            needReAlignment = true;
            return true;
        }

        [Initialize]
        private static void Initialize() {
            // Test maps: vanilla 9A, SJ ShatterSong and GMHS, Spring Collab 2020 Expert CANADIAN, Into-the-Jungle maps
            // CommunalHelper/CustomCassetteBlock is a subclass of CassetteBlock, and it seems that it's nothing different from the original ones, except colors
            SJ_CBMType = ModUtils.GetType("StrawberryJam2021", "Celeste.Mod.StrawberryJam2021.Entities.WonkyCassetteBlockController");
            SJ_CassetteBlockType = ModUtils.GetType("StrawberryJam2021", "Celeste.Mod.StrawberryJam2021.Entities.WonkyCassetteBlock");
            QM_CBMType = ModUtils.GetType("QuantumMechanics", "Celeste.Mod.QuantumMechanics.Entities.WonkyCassetteBlockController");
            QM_CassetteBlockType = ModUtils.GetType("QuantumMechanics", "Celeste.Mod.QuantumMechanics.Entities.WonkyCassetteBlock");
            if (ModUtils.GetType("FrostHelper", "FrostHelper.CassetteTempoTrigger")?.GetMethodInfo("SetManagerTempo") is { } method) {
                method.HookAfter(SetStateChanged);
            }
        }

        internal static void BuildBeatColors(Level level) {
            // we assume cassetteblocks with same color always have same beats
            if (SJ_CassetteBlockType is not null) {
                SJbeatColors = new();
                foreach (Entity cassetteBlock in level.Tracker.Entities[SJ_CassetteBlockType]) {
                    int[] OnAtBeats = cassetteBlock.GetFieldValue<int[]>("OnAtBeats");
                    int controllerIndex = cassetteBlock.GetFieldValue<int>("ControllerIndex");
                    Color color = (cassetteBlock as CassetteBlock)!.color;
                    foreach (int n in OnAtBeats) {
                        SJbeatColors[n + controllerIndex * SJWonkyCassetteBlockControllerSimulator.minorOffset] = color;
                    }
                }
            }
            if (QM_CassetteBlockType is not null) {
                QMbeatColors = new();
                foreach (Entity cassetteBlock in level.Tracker.Entities[QM_CassetteBlockType]) {
                    int[] OnAtBeats = cassetteBlock.GetFieldValue<int[]>("OnAtBeats");
                    int controllerIndex = cassetteBlock.GetFieldValue<int>("ControllerIndex");
                    Color color = (cassetteBlock as CassetteBlock)!.color;
                    foreach (int n in OnAtBeats) {
                        QMbeatColors[n + controllerIndex * QMWonkyCassetteBlockControllerSimulator.minorOffset] = color;
                    }
                }
            }
        }

        internal static Type SJ_CBMType;

        internal static Type SJ_CassetteBlockType;

        internal static Type QM_CBMType;

        internal static Type QM_CassetteBlockType;

        public static Dictionary<int, Color> SJbeatColors = new();

        public static Dictionary<int, Color> QMbeatColors = new();

        private const int loop = 512; // contains two full cycles
        public override void Update() {
            if (Predictor.PredictorCore.InPredict) {
                return;
            }
            if (!Enabled) {
                RemoveSelf();
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
                else if (QM_CBMType != null && Engine.Scene.Tracker.Entities.TryGetValue(QM_CBMType, out List<Entity> list2) && list2.Count > 0) {
                    Visible = true;
                    this.cbm = list2[0];
                    findCBM = true;
                    cbmType = CBMType.QM;
                    this.Tag |= Tags.TransitionUpdate;
                }
                else {
                    Visible = false;
                    RemoveSelf();
                    return;
                }
            }
            if (cbm is null || cbm.Scene != Engine.Scene || cbmType == CBMType.None) { // always check this, so in case there's a teleport or something... so the cbm is actually removed from scene (but we still have a reference to it so it's not cleared by GC)
                RemoveSelf();
                Visible = false;
                return;
            }
            Instance = this;
            base.Update();
            TimeElapse++;
            bool timerateGotoMonitor = Math.Abs(Engine.DeltaTime - LastDeltaTime) > 0.0001f;
            bool normalGotoMonitor = false;

            if (!timerateGotoMonitor && !stateChanged) {
                if (TimeElapse <= NearestTime) {
                    // nothing happens
                }
                else if (TimeElapse > NearestTime + 1) {
                    normalGotoMonitor = true;
                }
                else {
                    bool found = false;
                    foreach (KeyValuePair<int, List<int>> pair in ColorSwapTime) {
                        List<int> list = pair.Value;
                        if (list.IsNotNullOrEmpty() && list[0] < TimeElapse) {
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
                        if (NearestTime < TimeElapse) {
                            normalGotoMonitor = true;
                        }
                    }
                }
            }

            if (timerateGotoMonitor || normalGotoMonitor || stateChanged) {
                stateChanged = false;
                LastDeltaTime = Engine.DeltaTime;
                bool hasData;
                if (cbmType == CBMType.Vanilla) {
                    VanillaCasstteBlockManagerSimulator.Initialize(cbm, out currColorIndex, out maxBeat, out beatIndexMax, out tempoMult, out VanillaIndexPeriod);
                    hasData = VanillaCasstteBlockManagerSimulator.UpdateLoop(2 * loop, ref ColorSwapTime);
                }
                else if (cbmType == CBMType.SJ) {
                    SJWonkyCassetteBlockControllerSimulator.Initialize(cbm, out currColorIndex, out maxBeat, out beatIndexMax, out maxBeatTimer);
                    hasData = SJWonkyCassetteBlockControllerSimulator.UpdateLoop(2 * loop, ref ColorSwapTime);
                }
                else {
                    QMWonkyCassetteBlockControllerSimulator.Initialize(cbm, out currColorIndex, out maxBeat, out beatIndexMax, out maxBeatTimer);
                    hasData = QMWonkyCassetteBlockControllerSimulator.UpdateLoop(2 * loop, ref ColorSwapTime);
                }
                if (hasData && ColorSwapTime.IsNotNullOrEmpty()) {
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
            if (DebugRendered && ShowExtraInfo) {
                UpdateExtraInfo();
            }
        }

        public int TimeElapse = 0;

        public int NearestTime = 0;

        public float LastDeltaTime = 0f;

        public int VanillaIndexPeriod = 8;

        public static Dictionary<int, List<int>> ColorSwapTime = new();

        private static Vector2 textOffset = new Vector2(40f, -10f);

        public static bool stateChanged = false;

        public static void SetStateChanged() {
            stateChanged = true;
            needReAlignment = true;
        }

        public static void FreezeAdvanceTime(CassetteBlockVisualizer visualizer, float time) {
            if (VanillaCasstteBlockManagerSimulator.GetLoopCount(time, out int count)) {
                for (int i = 0; i < count; i++) {
                    visualizer.Update();
                }
            }
            else {
                stateChanged = true;
                // freeze time may not be an exact multiple of Engine.DeltaTime * tempoMult, so we cant handle it using several updates
                // goto sync with cbm (on which the freeze advance time has already been applied)
            }
        }

        public void UpdateExtraInfo() {
            if (cbmType == CBMType.Vanilla) {
                CassetteBlockManager manager = (CassetteBlockManager)cbm;
                beatTimer = 60f * manager.beatTimer;
                beatIndex = manager.beatIndex;
                if (VanillaCasstteBlockManagerSimulator.jungle_SwingBlockType is { } type && Engine.Scene.Tracker.Entities[type].Count > 0) {
                    maxBeatTimer = 10f * ((beatIndex % 2 == 0) ? 1.32f : 0.68f);
                }
                else {
                    maxBeatTimer = 10f;
                }
            }
            else if (cbmType == CBMType.SJ || cbmType == CBMType.QM) {
                EverestModuleSession session = null;
                if (cbmType == CBMType.SJ && ModUtils.GetType("StrawberryJam2021", "Celeste.Mod.StrawberryJam2021.StrawberryJam2021Module")?.GetPropertyValue<EverestModuleSession>("Session") is EverestModuleSession sessionSJ) {
                    session = sessionSJ;
                }
                else if (cbmType == CBMType.QM && ModUtils.GetType("QuantumMechanics", "Celeste.Mod.QuantumMechanics.QuantumMechanicsModule")?.GetPropertyValue<EverestModuleSession>("Session") is EverestModuleSession sessionQM) {
                    session = sessionQM;
                }
                if (session is null) {
                    return;
                }
                beatTimer = 60f * session.GetFieldValue<float>("CassetteBeatTimer");
                beatIndex = session.GetFieldValue<int>("CassetteWonkyBeatIndex");
                minorControllerData.Clear();
                Type type = cbmType == CBMType.SJ ? SJWonkyCassetteBlockControllerSimulator.MinorSimulator.minor_type : QMWonkyCassetteBlockControllerSimulator.MinorSimulator.minor_type;
                if (type is null) {
                    return;
                }
                if (Engine.Scene.Tracker.Entities.TryGetValue(type, out List<Entity> sourceList) && sourceList.Count > 0) {
                    foreach (Entity entity in sourceList) {
                        minorControllerData.Add(new Tuple<int, int, int, float, float>(
                            entity.GetFieldValue<int>("ControllerIndex"),
                            entity.GetFieldValue<int>("CassetteWonkyBeatIndex"),
                            entity.GetFieldValue<int>("maxBeats"),
                            60f * entity.GetFieldValue<float>("CassetteBeatTimer"),
                            60f * entity.GetFieldValue<float>("beatIncrement")
                        ));
                    }
                }
            }
        }

        private static readonly Vector2 centerLeft = Vector2.UnitY * 0.5f;
        private static readonly Vector2 center = Vector2.One * 0.5f;
        private static readonly Vector2 centerRight = new Vector2(1f, 0.5f);
        public override void Render() {
            if (!DebugRendered) {
                return;
            }
            if (cbmType == CBMType.Vanilla) {
                HandleAlignment();
                VanillaRender();
            }
            else if (cbmType == CBMType.SJ || cbmType == CBMType.QM) {
                HandleAlignment();
                SJ_Render(cbmType == CBMType.SJ);
            }
        }

        private void VanillaRender() {
            Vector2 pos = Position;
            for (int i = 0; i < maxBeat; i++) {
                Monocle.Draw.Rect(pos, 20f, 20f, VanillaColorChooser(i, i == currColorIndex));
                Monocle.Draw.HollowRect(pos - Vector2.One, 22f, 22f, Color.Black);
                if (ColorSwapTime[i] is { } list && list.Count > 0) {
                    Message.RenderMessage((list[0] - TimeElapse).ToString(), pos + textOffset, Vector2.Zero, Vector2.One * 0.7f);
                }

                pos += Vector2.UnitY * 40f;
            }
            if (!ShowExtraInfo) {
                return;
            }
            if (Alignment is Alignments.TopLeft or Alignments.BottomLeft) {
                pos += new Vector2(60f, 10f);
            }
            else {
                pos += new Vector2(20f, 10f);
            }
            Message.RenderMessageJetBrainsMono($"[{beatIndex % VanillaIndexPeriod}/{VanillaIndexPeriod}]", pos, centerLeft, Vector2.One, 2f, Color.White, Color.Black);
            Message.RenderMessageJetBrainsMono($"beat ", pos, centerRight, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
            pos.Y += 30f;
            Message.RenderMessageJetBrainsMono($"[{beatIndex}/{beatIndexMax}]", pos, centerLeft, Vector2.One, 2f, Color.White, Color.Black);
            Message.RenderMessageJetBrainsMono("index ", pos, centerRight, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
            pos.Y += 30f;
            Message.RenderMessageJetBrainsMono($"{beatTimer,5:0.00}/{maxBeatTimer,5:0.00}", pos - Vector2.UnitX * 10f, centerLeft, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
            Message.RenderMessageJetBrainsMono("timer ", pos, centerRight, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
            if (tempoMult != 1f) {
                pos.Y += 30f;
                Message.RenderMessageJetBrainsMono(tempoMult.ToString("0.00"), pos + Vector2.UnitX, centerLeft, Vector2.One, 2f, Color.White, Color.Black);
                Message.RenderMessageJetBrainsMono("tempo ", pos, centerRight, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
            }
        }

        private Vector2 VanillaRenderTotalOffset() {
            // the offset from position to bottom left of the content

            Vector2 delta = new Vector2(20f, -20f);
            delta.Y += 40f * maxBeat;
            if (ShowExtraInfo) {
                if (Alignment is Alignments.TopLeft or Alignments.BottomLeft) {
                    delta += new Vector2(0f, 90f);
                }
                else {
                    delta += new Vector2(-40f, 90f);
                }
                if (tempoMult != 1f) {
                    delta.Y += 30f;
                }
            }
            return delta;
        }

        private void SJ_Render(bool isSJ) {
            Dictionary<int, Color> beatColors = isSJ ? SJbeatColors : QMbeatColors;
            Vector2 pos = Position;
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
            if (!ShowExtraInfo) {
                return;
            }
            if (Alignment is Alignments.TopLeft or Alignments.BottomLeft) {
                pos += new Vector2(140f, 22f);
            }
            else {
                pos += new Vector2(-240f, 22f);
            }
            Message.RenderMessageJetBrainsMono($"[{beatIndex}/{beatIndexMax}]", pos, centerLeft, Vector2.One, 2f, Color.White, Color.Black);
            Message.RenderMessageJetBrainsMono("index ", pos, centerRight, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
            pos.Y += 30f;
            Message.RenderMessageJetBrainsMono($"{beatTimer,5:0.00}/{maxBeatTimer,5:0.00}", pos + Vector2.UnitX * 2f, centerLeft, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
            Message.RenderMessageJetBrainsMono("timer ", pos, centerRight, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
            if (minorControllerData.IsNotNullOrEmpty()) {
                bool moreThanOne = minorControllerData.Count > 1;
                pos.Y += 30f;
                Message.RenderMessageJetBrainsMono("main", pos, centerLeft, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
                foreach (Tuple<int, int, int, float, float> tuple in minorControllerData) {
                    pos.Y += 20f;
                    Monocle.Draw.Line(pos - Vector2.UnitX * 80f, pos + Vector2.UnitX * 140f, Color.White);
                    pos.Y += 20f;
                    Message.RenderMessageJetBrainsMono(moreThanOne ? $"minor {tuple.Item1}" : "minor", pos, centerLeft, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
                    pos.Y += 30f;
                    Message.RenderMessageJetBrainsMono($"[{tuple.Item2}/{tuple.Item3}]", pos, centerLeft, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
                    Message.RenderMessageJetBrainsMono("index ", pos, centerRight, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
                    pos.Y += 30f;
                    Message.RenderMessageJetBrainsMono($"{tuple.Item4,5:0.00}/{tuple.Item5,5:0.00}", pos + Vector2.UnitX * 2f, centerLeft, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
                    Message.RenderMessageJetBrainsMono("timer ", pos, centerRight, Vector2.One * 0.8f, 2f, Color.White, Color.Black);
                }
            }
        }

        private Vector2 SJRenderTotalOffset(bool isSJ) {
            Dictionary<int, Color> beatColors = isSJ ? SJbeatColors : QMbeatColors;
            Vector2 iniOffset = textOffset;
            Vector2 contentOffset = Vector2.Zero;

            void GetXY(Vector2 vec) {
                contentOffset.X = Math.Min(contentOffset.X, vec.X);
                contentOffset.Y = Math.Max(contentOffset.Y, vec.Y);
            }

            int highestIndex = 0;
            foreach (int index in ColorSwapTime.Keys) {
                if (Math.Abs(index) > SJWonkyCassetteBlockControllerSimulator.minorOffset / 2) {
                    int controllerIndex = (int)Math.Round((float)index / (float)SJWonkyCassetteBlockControllerSimulator.minorOffset);
                    GetXY(new Vector2(140f - 160f * controllerIndex, 40f * (highestIndex + 2)));
                    if (ColorSwapTime[index] is { } list && list.Count > 0) {
                        int y = index - controllerIndex * SJWonkyCassetteBlockControllerSimulator.minorOffset;
                        GetXY(new Vector2(-27f - 160f * (controllerIndex - 1), 40f * (highestIndex + y + 3)));
                        if (beatColors.TryGetValue(index, out Color color)) {
                            GetXY(new Vector2(-160f * controllerIndex + 80f, 12f + 40f * (highestIndex + y + 3)));
                        }
                    }
                }
                else if (ColorSwapTime[index] is { } list && list.Count > 0) {
                    GetXY(new Vector2(-27f, 40f * index));
                    highestIndex = Math.Max(highestIndex, index);
                    if (beatColors.TryGetValue(index, out Color color)) {
                        GetXY(new Vector2(-80f, 12f + 40f * index));
                    }
                }
            }

            return iniOffset + contentOffset + new Vector2(10f, 20f);
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

        public void HandleAlignment() {
            if (Scene is not Level level || level.Transitioning) {
                return;
            }
            if (needReAlignment) {
                if (Alignment == Alignments.None) {
                    Position = PositionWithoutAlignment;
                    needReAlignment = false;
                    return;
                }
                if (Alignment == Alignments.TopRight) {
                    Position = TopRightInScreen;
                    needReAlignment = false;
                    return;
                }
                Vector2 offset;
                if (cbmType == CBMType.Vanilla) {
                    offset = VanillaRenderTotalOffset();
                }
                else if (cbmType == CBMType.SJ || cbmType == CBMType.QM) {
                    offset = SJRenderTotalOffset(cbmType == CBMType.SJ);
                }
                else {
                    needReAlignment = false;
                    return;
                }
                if (Alignment == Alignments.BottomRight) {
                    Position.X = TopRightInScreen.X;
                    Position.Y = 1080f - 20f - offset.Y;
                    needReAlignment = false;
                    return;
                }
                if (Alignment == Alignments.TopLeft) {
                    float speedrunOffset = Settings.Instance.SpeedrunClock switch {
                        SpeedrunType.Chapter => 100f,
                        SpeedrunType.File => 120f,
                        _ => 0f,
                    };
                    Position.Y = TopRightInScreen.Y + speedrunOffset;
                    Position.X = 40f - offset.X;
                    needReAlignment = false;
                    return;
                }
                if (Alignment == Alignments.BottomLeft) {
                    Position.X = 40f - offset.X;
                    Position.Y = 1080f - 20f - offset.Y;
                    needReAlignment = false;
                    return;
                }
            }
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