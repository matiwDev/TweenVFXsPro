using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace Creatush.TweenEffectsPro.Editor
{
    // ── Single Effect Controller ──────────────────────────────────────────────

    [CustomEditor(typeof(SingleEffectController))]
    [CanEditMultipleObjects]
    public class SingleControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (target == null) return;
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🎮 SINGLE EFFECT PLAYER", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Triggers a single behavior. Perfect for buttons and simple UI feedback.",
                MessageType.Info);
            DrawDefaultInspector();
        }
    }

    // ── Sequence Effects Controller ───────────────────────────────────────────

    [CustomEditor(typeof(SequenceEffectsController))]
    public class SequenceControllerEditor : UnityEditor.Editor
    {
        // ── Serialized properties ─────────────────────────────────────────────
        private ReorderableList    _list;
        private SerializedProperty _sequenceSteps;
        private SerializedProperty _targetOverride;
        private SerializedProperty _autoPlay;
        private SerializedProperty _onStart;
        private SerializedProperty _onEnable;
        private SerializedProperty _loop;
        private SerializedProperty _loopCount;
        private SerializedProperty _loopInterval;
        private SerializedProperty _outAnim;
        private SerializedProperty _outEnabled;
        private SerializedProperty _outReverse;
        private SerializedProperty _outSpeed;
        private SerializedProperty _outOnComplete;

        // ── Foldout state ─────────────────────────────────────────────────────
        private bool _autoPlayFoldout = true;
        private bool _timelineFoldout = true;

        // ── Timeline constants ────────────────────────────────────────────────
        private const float LABEL_W  = 110f;
        private const float ROW_H    = 18f;
        private const float ROW_GAP  = 2f;
        private const float RULER_H  = 20f;
        private const float MIN_SECS = 0.1f;

        private static readonly Color C_BG      = new Color(0.15f, 0.15f, 0.15f, 1f);
        private static readonly Color C_ALT     = new Color(0.17f, 0.17f, 0.17f, 1f);
        private static readonly Color C_BAR     = new Color(0.27f, 0.60f, 0.95f, 0.90f);
        private static readonly Color C_JOIN    = new Color(0.35f, 0.85f, 0.55f, 0.90f);
        private static readonly Color C_DELAY   = new Color(1.00f, 0.85f, 0.30f, 0.55f);
        private static readonly Color C_RULER   = new Color(0.10f, 0.10f, 0.10f, 1f);
        private static readonly Color C_TICK_LB = new Color(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Color C_GRID    = new Color(1.00f, 1.00f, 1.00f, 0.06f);

        // ── Init ──────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            if (target == null || serializedObject == null) return;

            _sequenceSteps  = serializedObject.FindProperty("sequenceSteps");
            _targetOverride = serializedObject.FindProperty("targetOverride");
            _autoPlay       = serializedObject.FindProperty("autoPlay");

            if (_autoPlay != null)
            {
                _onStart      = _autoPlay.FindPropertyRelative("onStart");
                _onEnable     = _autoPlay.FindPropertyRelative("onEnable");
                _loop         = _autoPlay.FindPropertyRelative("loop");
                _loopCount    = _autoPlay.FindPropertyRelative("loopCount");
                _loopInterval = _autoPlay.FindPropertyRelative("loopInterval");
            }

            _outAnim = serializedObject.FindProperty("outAnim");
            if (_outAnim != null)
            {
                _outEnabled    = _outAnim.FindPropertyRelative("enabled");
                _outReverse    = _outAnim.FindPropertyRelative("reverse");
                _outSpeed      = _outAnim.FindPropertyRelative("speed");
                _outOnComplete = _outAnim.FindPropertyRelative("OnOutComplete");
            }

            BuildList();
        }

        private void BuildList()
        {
            if (_sequenceSteps == null) return;

            _list = new ReorderableList(serializedObject, _sequenceSteps, true, true, true, true);

            _list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "🎬 Animation Sequence Steps", EditorStyles.boldLabel);

            _list.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 4f;

            _list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (_sequenceSteps == null || index >= _sequenceSteps.arraySize) return;

                var el = _sequenceSteps.GetArrayElementAtIndex(index);
                rect.y += 2f;

                const float sp = 8f;
                float bw = rect.width * 0.55f;
                float dw = rect.width * 0.20f - sp;

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, bw, EditorGUIUtility.singleLineHeight),
                    el.FindPropertyRelative("behavior"), GUIContent.none);

                float dx = rect.x + bw + sp;
                EditorGUI.PropertyField(
                    new Rect(dx, rect.y, dw, EditorGUIUtility.singleLineHeight),
                    el.FindPropertyRelative("delay"), GUIContent.none);

                float jx = rect.x + bw + dw + sp * 2f;
                EditorGUI.LabelField(new Rect(jx, rect.y, 32f, EditorGUIUtility.singleLineHeight), "Join");
                EditorGUI.PropertyField(
                    new Rect(jx + 32f, rect.y, 20f, EditorGUIUtility.singleLineHeight),
                    el.FindPropertyRelative("joinPrevious"), GUIContent.none);
            };
        }

        // ── Main Inspector ────────────────────────────────────────────────────
        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;

            serializedObject.Update();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("⛓️ SEQUENCE ORCHESTRATOR", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawAutoPlayBlock();
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(_targetOverride,
                new GUIContent("Target Override",
                    "Apply all effects to this transform instead of each behaviour's own."));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);
            DrawColumnHeaders();

            if (_list != null && _sequenceSteps != null)
            {
                try { _list.DoLayoutList(); }
                catch { }
            }

            EditorGUILayout.Space(8);
            DrawTimeline();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("▶  Play",    GUILayout.Height(26)))
                    ((SequenceEffectsController)target).Play();
                if (GUILayout.Button("◀  Reverse", GUILayout.Height(26)))
                    ((SequenceEffectsController)target).PlayReverse();
                if (GUILayout.Button("■  Stop",    GUILayout.Height(26)))
                    ((SequenceEffectsController)target).Stop();
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ── Auto Play block ───────────────────────────────────────────────────
        private void DrawAutoPlayBlock()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _autoPlayFoldout = EditorGUILayout.Foldout(
                _autoPlayFoldout, "⚡  Auto Play Triggers", true, EditorStyles.foldoutHeader);

            if (!_autoPlayFoldout || _autoPlay == null)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(2);

            // ── On Start ──────────────────────────────────────────────────────
            DrawTriggerRow(_onStart, "On Start",
                "Play once on Start() — first activation only.");

            // ── On Enable ─────────────────────────────────────────────────────
            DrawTriggerRow(_onEnable, "On Enable",
                "Play every time this GameObject is enabled.");

            EditorGUILayout.Space(2);
            EditorGUILayout.EndVertical();

            // ── Out Animation block ───────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🚪  Out Animation", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            if (_outEnabled != null)
            {
                DrawTriggerRow(_outEnabled, "Enabled",
                    "Call Hide() instead of SetActive(false) to play this animation before deactivating.");

                if (_outEnabled.boolValue)
                {
                    EditorGUI.indentLevel++;

                    DrawTriggerRow(_outReverse, "Reverse",
                        "Play the sequence reversed as the exit animation.");

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(
                        new GUIContent("Speed", "Playback speed multiplier. 1 = same, 1.5 = 50% faster."),
                        GUILayout.Width(44));
                    EditorGUILayout.PropertyField(_outSpeed, GUIContent.none, GUILayout.Width(48));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(2);
                    EditorGUILayout.PropertyField(_outOnComplete,
                        new GUIContent("On Out Complete",
                            "Fired when the out animation finishes. " +
                            "Wire deactivation of parent objects or scene transitions here."));

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();

            // ── Loop block ────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔁  Loop", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            DrawTriggerRow(_loop, "Enabled",
                "Repeat the sequence while this object is active.");

            bool loopOn = _loop != null && _loop.boolValue;
            if (loopOn)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(
                    new GUIContent("Count", "Number of times to loop. -1 = infinite."),
                    GUILayout.Width(48));
                EditorGUILayout.PropertyField(_loopCount, GUIContent.none, GUILayout.Width(44));

                GUILayout.Space(12);

                EditorGUILayout.LabelField(
                    new GUIContent("Interval (s)", "Gap between end of one run and start of the next."),
                    GUILayout.Width(74));
                EditorGUILayout.PropertyField(_loopInterval, GUIContent.none, GUILayout.Width(44));

                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.EndVertical();
        }

        // Single trigger row: full-width toggle using standard property field style.
        private static void DrawTriggerRow(SerializedProperty prop, string label, string tooltip)
        {
            if (prop == null) return;
            EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
        }

        // ── Timeline panel ────────────────────────────────────────────────────
        private void DrawTimeline()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _timelineFoldout = EditorGUILayout.Foldout(
                _timelineFoldout, "⏱  Timeline Preview", true, EditorStyles.foldoutHeader);

            if (!_timelineFoldout || _sequenceSteps == null)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(4);

            var steps = BuildTimelineSteps();

            if (steps.Count == 0)
            {
                EditorGUILayout.HelpBox("Add steps above to see the timeline.", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            float totalDur = Mathf.Max(MIN_SECS, CalcTotalDuration(steps));
            float panelH   = steps.Count * (ROW_H + ROW_GAP) + RULER_H + 2f;

            // GetRect must always execute — never skip it.
            Rect panel = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none,
                GUILayout.Height(panelH),
                GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                float trackX = panel.x + LABEL_W;
                float trackW = panel.width - LABEL_W;

                EditorGUI.DrawRect(panel, C_BG);

                for (int i = 0; i < steps.Count; i++)
                {
                    var   s    = steps[i];
                    float rowY = panel.y + i * (ROW_H + ROW_GAP);

                    // Row background
                    EditorGUI.DrawRect(new Rect(panel.x, rowY, panel.width, ROW_H),
                                       i % 2 == 0 ? C_BG : C_ALT);

                    // Label
                    GUI.Label(
                        new Rect(panel.x + 2f, rowY + 1f, LABEL_W - 6f, ROW_H - 2f),
                        s.label,
                        new GUIStyle(EditorStyles.miniLabel) { clipping = TextClipping.Clip });

                    // Delay stripe — drawn from step start minus delay to step start
                    if (s.delay > 0f)
                    {
                        float animStart  = trackX + (s.startTime          / totalDur) * trackW;
                        float delayStart = trackX + ((s.startTime - s.delay) / totalDur) * trackW;
                        delayStart = Mathf.Max(trackX, delayStart);
                        EditorGUI.DrawRect(
                            new Rect(delayStart, rowY + ROW_H * 0.30f,
                                     animStart - delayStart, ROW_H * 0.40f),
                            C_DELAY);
                    }

                    // Animation bar
                    if (s.duration > 0f)
                    {
                        float bx = trackX + (s.startTime / totalDur) * trackW;
                        float bw = Mathf.Max(2f, (s.duration / totalDur) * trackW);
                        Rect  br = new Rect(bx, rowY + 2f, bw, ROW_H - 4f);
                        EditorGUI.DrawRect(br, s.isJoined ? C_JOIN : C_BAR);

                        if (bw > 24f)
                            GUI.Label(
                                new Rect(br.x + 3f, br.y, br.width - 4f, br.height),
                                $"{s.duration:0.##}s",
                                new GUIStyle(EditorStyles.miniLabel)
                                {
                                    clipping  = TextClipping.Clip,
                                    fontStyle = FontStyle.Bold,
                                    normal    = { textColor = Color.white }
                                });
                    }
                }

                DrawRuler(trackX, panel.y + steps.Count * (ROW_H + ROW_GAP), trackW, totalDur);
            }

            // Legend — layout calls, always execute.
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            DrawLegendSwatch(C_BAR,   "Sequential");
            DrawLegendSwatch(C_JOIN,  "Joined");
            DrawLegendSwatch(C_DELAY, "Delay");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ── Timeline data ─────────────────────────────────────────────────────

        private struct TimelineStep
        {
            public string label;
            public float  startTime; // when the animation bar begins (after delay)
            public float  duration;
            public float  delay;
            public bool   isJoined;
        }

        private List<TimelineStep> BuildTimelineSteps()
        {
            var   result = new List<TimelineStep>();
            float cursor = 0f;

            var durField = typeof(VFXBehaviour).GetField(
                "duration",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            for (int i = 0; i < _sequenceSteps.arraySize; i++)
            {
                var el        = _sequenceSteps.GetArrayElementAtIndex(i);
                var bhvProp   = el.FindPropertyRelative("behavior");
                var delayProp = el.FindPropertyRelative("delay");
                var joinProp  = el.FindPropertyRelative("joinPrevious");

                float delay = delayProp != null ? delayProp.floatValue : 0f;
                bool  join  = joinProp  != null ? joinProp.boolValue   : false;

                float  dur = 0f;
                string lbl = bhvProp?.objectReferenceValue != null
                    ? bhvProp.objectReferenceValue.GetType().Name
                    : "(empty)";

                if (bhvProp?.objectReferenceValue is VFXBehaviour vfx && durField != null)
                    dur = (float)durField.GetValue(vfx);

                // startTime = when the animation bar begins (delay already consumed)
                float start;
                if (join)
                    start = result.Count > 0
                        ? result[result.Count - 1].startTime + delay
                        : cursor + delay;
                else
                {
                    start  = cursor + delay;  // delay pushes cursor, start is after it
                    cursor = start + dur;
                }

                result.Add(new TimelineStep
                {
                    label     = lbl,
                    startTime = start,
                    duration  = dur,
                    delay     = delay,
                    isJoined  = join
                });
            }

            return result;
        }

        private static float CalcTotalDuration(List<TimelineStep> steps)
        {
            float max = 0f;
            foreach (var s in steps) max = Mathf.Max(max, s.startTime + s.duration);
            return max;
        }

        private static void DrawRuler(float x, float y, float w, float totalDur)
        {
            EditorGUI.DrawRect(new Rect(x, y, w, RULER_H), C_RULER);
            int ticks = Mathf.Clamp(Mathf.RoundToInt(w / 50f), 2, 20);
            for (int t = 0; t <= ticks; t++)
            {
                float frac = (float)t / ticks;
                float tx   = x + frac * w;
                float secs = frac * totalDur;
                EditorGUI.DrawRect(new Rect(tx, y, 1f, 6f), C_GRID);
                GUI.Label(
                    new Rect(tx + 2f, y + 2f, 38f, 14f),
                    secs < 1f ? $"{secs * 1000f:0}ms" : $"{secs:0.#}s",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = C_TICK_LB } });
            }
        }

        private static void DrawLegendSwatch(Color color, string label)
        {
            Rect r = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f));
            r.y += 2f;
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(r, color);
            GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(64f));
            GUILayout.Space(8f);
        }

        private static void DrawColumnHeaders()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            float w = EditorGUIUtility.currentViewWidth - 55f;
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.gray }
            };
            GUILayout.Label("EFFECT BEHAVIOR", style, GUILayout.Width(w * 0.55f));
            GUILayout.Label("DELAY (S)",        style, GUILayout.Width(w * 0.20f));
            GUILayout.Label("JOIN",             style, GUILayout.Width(w * 0.15f));
            EditorGUILayout.EndHorizontal();
        }
    }
}
