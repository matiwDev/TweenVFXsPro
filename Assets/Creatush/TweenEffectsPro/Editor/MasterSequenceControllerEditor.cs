using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace Creatush.TweenEffectsPro.Editor
{
    /// <summary>
    /// The single inspector for TweenEffects Pro: MasterSequenceController.
    /// Replaces BaseControllerEditor.cs (Single/Sequence) and the fact that
    /// Parallel/Stagger never had a custom editor at all — every step now
    /// gets the same treatment regardless of target mode.
    /// </summary>
    [CustomEditor(typeof(MasterSequenceController))]
    public class MasterSequenceControllerEditor : UnityEditor.Editor
    {
        // ── Serialized properties ─────────────────────────────────────────────
        private ReorderableList _list;
        private SerializedProperty _steps;
        private SerializedProperty _autoPlay;
        private SerializedProperty _onStart;
        private SerializedProperty _onEnable;
        private SerializedProperty _loop;
        private SerializedProperty _loopCount;
        private SerializedProperty _loopInterval;
        private SerializedProperty _loopType;
        private SerializedProperty _triggers;
        private SerializedProperty _playTrigger;
        private SerializedProperty _killTrigger;
        private SerializedProperty _onSequenceComplete;

        // ── Foldout state ─────────────────────────────────────────────────────
        private bool _autoPlayFoldout = true;
        private bool _timelineFoldout = true;

        // ── Cached settings ───────────────────────────────────────────────────
        private TweenSettingsSO _settings;
        private GUIStyle _sectionStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _mutedLabelStyle;

        private static readonly Color C_DIVIDER = new Color(0.5f, 0.5f, 0.5f, 0.25f);

        // ── Timeline constants ────────────────────────────────────────────────
        private const float LABEL_W = 110f;
        private const float ROW_H = 18f;
        private const float ROW_GAP = 2f;
        private const float RULER_H = 20f;
        private const float MIN_SECS = 0.1f;

        private static readonly Color C_BG = new Color(0.15f, 0.15f, 0.15f, 1f);
        private static readonly Color C_ALT = new Color(0.17f, 0.17f, 0.17f, 1f);
        // Single-hue-family palette (blue) so the timeline reads as one
        // cohesive, professional surface — bars differ by lightness/tint
        // rather than by hue, with a neutral grey reserved for delay spans.
        private static readonly Color C_BAR = new Color(0.30f, 0.55f, 0.85f, 0.95f);
        private static readonly Color C_JOIN = new Color(0.50f, 0.78f, 0.92f, 0.95f);
        private static readonly Color C_STAGGER = new Color(0.33f, 0.33f, 0.68f, 0.95f);
        private static readonly Color C_DELAY = new Color(0.65f, 0.65f, 0.65f, 0.45f);
        private static readonly Color C_RULER = new Color(0.10f, 0.10f, 0.10f, 1f);
        private static readonly Color C_TICK_LB = new Color(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Color C_GRID = new Color(1.00f, 1.00f, 1.00f, 0.06f);

        // ── Init ──────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (target == null || serializedObject == null) return;

            _steps = serializedObject.FindProperty("steps");
            _autoPlay = serializedObject.FindProperty("autoPlay");

            if (_autoPlay != null)
            {
                _onStart = _autoPlay.FindPropertyRelative("onStart");
                _onEnable = _autoPlay.FindPropertyRelative("onEnable");
                _loop = _autoPlay.FindPropertyRelative("loop");
                _loopCount = _autoPlay.FindPropertyRelative("loopCount");
                _loopInterval = _autoPlay.FindPropertyRelative("loopInterval");
                _loopType = _autoPlay.FindPropertyRelative("loopType");
            }

            _triggers = serializedObject.FindProperty("triggers");
            if (_triggers != null)
            {
                _playTrigger = _triggers.FindPropertyRelative("playTrigger");
                _killTrigger = _triggers.FindPropertyRelative("killTrigger");
            }

            _onSequenceComplete = serializedObject.FindProperty("onSequenceComplete");

            _settings = Resources.Load<TweenSettingsSO>("TweenSettings");

            BuildList();
        }

        private void EnsureStyles()
        {
            if (_sectionStyle != null) return;

            _sectionStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(8, 8, 6, 8) };

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(0, 0, 2, 2)
            };

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel);

            _mutedLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = EditorStyles.miniLabel.normal.textColor }
            };
        }

        /// <summary>Bold title with a thin divider underneath — used for static (non-foldout) section headers, so every section reads consistently instead of each rolling its own label style.</summary>
        private void DrawSectionHeader(string title)
        {
            EditorGUILayout.LabelField(title, _sectionHeaderStyle);
            Rect lineRect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(lineRect, C_DIVIDER);
            EditorGUILayout.Space(2);
        }

        // ── ReorderableList ───────────────────────────────────────────────────

        private void BuildList()
        {
            if (_steps == null) return;

            _list = new ReorderableList(serializedObject, _steps, true, true, true, true);

            _list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Effect Steps", EditorStyles.boldLabel);

            _list.elementHeightCallback = index => ElementHeight(index);
            _list.drawElementCallback = (rect, index, isActive, isFocused) => DrawElement(rect, index);

            // Custom "+" — pick an effect type instead of adding an empty step.
            _list.onAddDropdownCallback = (buttonRect, list) => ShowAddEffectMenu(buttonRect);
        }

        private SerializedProperty ElementAt(int index) => _steps.GetArrayElementAtIndex(index);

        private float ElementHeight(int index)
        {
            var el = ElementAt(index);
            float lineH = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;

            float h = lineH + gap; // header row (foldout + type + preset)
            h += lineH + gap;      // timing row (delay + join + target mode)

            var targetModeProp = el.FindPropertyRelative("targetMode");
            var mode = (EffectTargetMode)targetModeProp.enumValueIndex;

            if (mode == EffectTargetMode.Single)
            {
                h += lineH + gap; // target override
            }
            else
            {
                var targetsProp = el.FindPropertyRelative("targets");
                if (mode == EffectTargetMode.Multiple)
                    h += EditorGUI.GetPropertyHeight(targetsProp, true) + gap;
                else
                    h += lineH + gap; // Children: just the parent override field

                h += lineH + gap; // stagger interval / reverse / random row
            }

            if (el.isExpanded)
            {
                var presetProp = el.FindPropertyRelative("preset");
                if (presetProp.objectReferenceValue == null)
                {
                    var effectProp = el.FindPropertyRelative("effect");
                    h += EditorGUI.GetPropertyHeight(effectProp, true) + gap;
                }
                else
                {
                    h += lineH + gap; // "using preset" note
                }
            }

            return h + 6f;
        }

        private void DrawElement(Rect rect, int index)
        {
            var el = ElementAt(index);
            float lineH = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;
            float y = rect.y + 3f;
            float x = rect.x;
            float w = rect.width;

            var effectProp = el.FindPropertyRelative("effect");
            var presetProp = el.FindPropertyRelative("preset");
            var delayProp = el.FindPropertyRelative("delay");
            var targetModeProp = el.FindPropertyRelative("targetMode");
            var targetOverrideProp = el.FindPropertyRelative("targetOverride");
            var targetsProp = el.FindPropertyRelative("targets");
            var staggerProp = el.FindPropertyRelative("staggerInterval");
            var reverseProp = el.FindPropertyRelative("reverseOrder");
            var randomProp = el.FindPropertyRelative("randomOrder");

            // ── Header row: foldout + effect type name + preset field ─────────
            string typeLabel = presetProp.objectReferenceValue != null
                ? $"{NiceEffectName(effectProp)} (preset)"
                : NiceEffectName(effectProp);

            Rect foldRect = new Rect(x, y, w * 0.55f, lineH);
            el.isExpanded = EditorGUI.Foldout(foldRect, el.isExpanded,
                string.IsNullOrEmpty(typeLabel) ? "(no effect)" : typeLabel, true);

            Rect presetRect = new Rect(x + w * 0.55f, y, w * 0.45f, lineH);
            EditorGUI.PropertyField(presetRect, presetProp, GUIContent.none);

            y += lineH + gap;

            // ── Timing row: delay, target mode ──────────────────────────────────
            float halfW = w * 0.55f - 4f;
            GUIContent delayLabel = new GUIContent("Delay",
                "Seconds from the end of the timeline so far. 0 = right after; " +
                "positive adds a gap; negative overlaps/joins earlier steps.");
            EditorGUI.LabelField(new Rect(x, y, 40f, lineH), delayLabel);
            EditorGUI.PropertyField(new Rect(x + 40f, y, halfW - 40f, lineH), delayProp, GUIContent.none);

            Rect modeRect = new Rect(x + halfW + 8f, y, w - halfW - 8f, lineH);
            EditorGUI.PropertyField(modeRect, targetModeProp, GUIContent.none);

            y += lineH + gap;

            var mode = (EffectTargetMode)targetModeProp.enumValueIndex;

            // ── Target row(s) ───────────────────────────────────────────────────
            if (mode == EffectTargetMode.Single)
            {
                EditorGUI.PropertyField(new Rect(x, y, w, lineH), targetOverrideProp,
                    new GUIContent("Target", "Defaults to this GameObject if empty. The effect finds whatever component it needs (Image, Graphic, CanvasGroup, etc.) on this GameObject or its children."));
                y += lineH + gap;
            }
            else if (mode == EffectTargetMode.Multiple)
            {
                float targetsH = EditorGUI.GetPropertyHeight(targetsProp, true);
                EditorGUI.PropertyField(new Rect(x, y, w, targetsH), targetsProp, true);
                y += targetsH + gap;
            }
            else // Children
            {
                EditorGUI.PropertyField(new Rect(x, y, w, lineH), targetOverrideProp,
                    new GUIContent("Parent", "Children of this transform are targeted. Defaults to this GameObject if empty."));
                y += lineH + gap;
            }

            if (mode != EffectTargetMode.Single)
            {
                float staggerW = w * 0.4f;
                EditorGUI.PropertyField(new Rect(x, y, staggerW, lineH), staggerProp,
                    new GUIContent("Stagger"));
                EditorGUI.PropertyField(new Rect(x + staggerW + 4f, y, (w - staggerW - 8f) * 0.5f, lineH),
                    reverseProp, new GUIContent("Reverse"));
                EditorGUI.PropertyField(new Rect(x + staggerW + 4f + (w - staggerW - 8f) * 0.5f + 4f, y,
                    (w - staggerW - 8f) * 0.5f, lineH), randomProp, new GUIContent("Random"));
                y += lineH + gap;
            }

            // ── Expanded: effect fields or preset note ─────────────────────────
            if (el.isExpanded)
            {
                if (presetProp.objectReferenceValue == null)
                {
                    float effH = EditorGUI.GetPropertyHeight(effectProp, true);
                    EditorGUI.PropertyField(new Rect(x, y, w, effH), effectProp, GUIContent.none, true);
                    y += effH + gap;
                }
                else
                {
                    EditorGUI.LabelField(new Rect(x, y, w, lineH),
                        "Using preset — inline effect fields are ignored.", EditorStyles.miniLabel);
                    y += lineH + gap;
                }
            }
        }

        private static string NiceEffectName(SerializedProperty effectProp) => EffectTypeMenu.NiceName(effectProp);

        private void ShowAddEffectMenu(Rect buttonRect)
        {
            EffectTypeMenu.ShowPicker(buttonRect, captured =>
            {
                serializedObject.Update();
                int newIndex = _steps.arraySize;
                _steps.arraySize++;
                var newEl = _steps.GetArrayElementAtIndex(newIndex);
                newEl.FindPropertyRelative("effect").managedReferenceValue =
                    System.Activator.CreateInstance(captured);
                newEl.FindPropertyRelative("delay").floatValue = 0f;
                newEl.FindPropertyRelative("targetMode").enumValueIndex = (int)EffectTargetMode.Single;
                newEl.isExpanded = true;
                serializedObject.ApplyModifiedProperties();
            });
        }

        // ── Main Inspector ────────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;
            EnsureStyles();

            serializedObject.Update();

            DrawGlobalSettingsPanel();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Master Sequence Controller", _titleStyle);
            EditorGUILayout.Space(4);

            DrawAutoPlayBlock();
            EditorGUILayout.Space(8);

            if (_list != null && _steps != null)
            {
                try { _list.DoLayoutList(); }
                catch { }
            }

            EditorGUILayout.Space(8);
            DrawTimeline();

            if (_onSequenceComplete != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.PropertyField(_onSequenceComplete,
                    new GUIContent("On Sequence Complete",
                        "Fired once this controller's sequence finishes playing — after every loop " +
                        "iteration if looping. Never fires when Loop Count is -1 (infinite)."));
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play", GUILayout.Height(26)))
                    ((MasterSequenceController)target).Play();
                if (GUILayout.Button("Reverse", GUILayout.Height(26)))
                    ((MasterSequenceController)target).PlayReverse();
                if (GUILayout.Button("Stop", GUILayout.Height(26)))
                    ((MasterSequenceController)target).Stop();
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ── Global Settings panel ─────────────────────────────────────────────

        private void DrawGlobalSettingsPanel()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(_sectionStyle);

            if (_settings == null)
            {
                EditorGUILayout.HelpBox(
                    "No TweenSettings asset found in Resources. " +
                    "Create one to configure global time scale, capacity, and safe mode.",
                    MessageType.Info);

                if (GUILayout.Button("Create TweenSettings Asset", GUILayout.Height(22)))
                    CreateSettingsAsset();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Select", GUILayout.Width(52), GUILayout.Height(18)))
                {
                    Selection.activeObject = _settings;
                    EditorGUIUtility.PingObject(_settings);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                DrawChip("Time Scale", _settings.globalTimeScale.ToString("0.##"),
                    !Mathf.Approximately(_settings.globalTimeScale, 1f));
                DrawChip("Unscaled", _settings.useUnscaledTime ? "ON" : "off", _settings.useUnscaledTime);
                DrawChip("Safe Mode", _settings.safeMode ? "ON" : "off", false);
                DrawChip("Capacity", $"{_settings.tweenersCapacity}/{_settings.sequencesCapacity}", false);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private static void DrawChip(string label, string value, bool highlight)
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = highlight ? new Color(1f, 0.85f, 0.2f) : new Color(0.6f, 0.6f, 0.6f) }
            };
            GUILayout.Label($"{label}: {value}", style);
            GUILayout.Space(6);
        }

        private void CreateSettingsAsset()
        {
            const string dir = "Assets/Resources";
            const string path = dir + "/TweenSettings.asset";

            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            var asset = ScriptableObject.CreateInstance<TweenSettingsSO>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            _settings = asset;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"[TweenEffects Pro] TweenSettings asset created at {path}");
        }

        // ── Auto Play block ───────────────────────────────────────────────────

        private void DrawAutoPlayBlock()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);

            _autoPlayFoldout = EditorGUILayout.Foldout(
                _autoPlayFoldout, "Auto Play Triggers", true, EditorStyles.foldoutHeader);

            if (!_autoPlayFoldout || _autoPlay == null)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(2);

            DrawTriggerRow(_onStart, "On Start", "Play once on Start() — first activation only.");
            DrawTriggerRow(_onEnable, "On Enable", "Play every time this GameObject is enabled.");

            EditorGUILayout.Space(2);
            EditorGUILayout.EndVertical();

            // ── Triggers block ────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(_sectionStyle);
            DrawSectionHeader("Triggers");

            if (_playTrigger != null && _killTrigger != null)
            {
                EditorGUILayout.PropertyField(_playTrigger,
                    new GUIContent("Play Trigger",
                        "Identifier external systems use to Play() this controller by name — " +
                        "PlayByTrigger(name). Leave blank if unused."));
                EditorGUILayout.PropertyField(_killTrigger,
                    new GUIContent("Kill Trigger",
                        "Identifier external systems use to Stop() this controller by name — " +
                        "KillByTrigger(name). Useful for ending a looping sequence."));

                if (Application.isPlaying)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_playTrigger.stringValue)))
                    {
                        if (GUILayout.Button("Test Play Trigger", GUILayout.Height(20)))
                            ((MasterSequenceController)target).PlayByTrigger(_playTrigger.stringValue);
                    }
                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_killTrigger.stringValue)))
                    {
                        if (GUILayout.Button("Test Kill Trigger", GUILayout.Height(20)))
                            ((MasterSequenceController)target).KillByTrigger(_killTrigger.stringValue);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();

            // ── Loop block ────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(_sectionStyle);
            DrawSectionHeader("Loop");

            DrawTriggerRow(_loop, "Enabled", "Repeat the sequence while this object is active.");

            bool loopOn = _loop != null && _loop.boolValue;
            if (loopOn)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(
                    new GUIContent("Count", "Number of times to loop. -1 = infinite."), GUILayout.Width(48));
                EditorGUILayout.PropertyField(_loopCount, GUIContent.none, GUILayout.Width(44));

                GUILayout.Space(12);

                EditorGUILayout.LabelField(
                    new GUIContent("Interval (s)", "Gap between end of one run and start of the next."),
                    GUILayout.Width(74));
                EditorGUILayout.PropertyField(_loopInterval, GUIContent.none, GUILayout.Width(44));

                EditorGUILayout.EndHorizontal();

                if (_loopType != null)
                {
                    // No custom GUIContent here — PropertyField pulls both the
                    // label and the [Tooltip] straight off the loopType field
                    // on ControllerAutoPlay, so the Restart/Yoyo/Incremental
                    // explanation stays attached without duplicating it here.
                    EditorGUILayout.PropertyField(_loopType);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.EndVertical();
        }

        private static void DrawTriggerRow(SerializedProperty prop, string label, string tooltip)
        {
            if (prop == null) return;
            EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
        }

        // ── Timeline panel ────────────────────────────────────────────────────

        private struct TimelineStep
        {
            public string label;
            public float startTime;
            public float duration;
            public float delay;
            public bool isJoined;
            public bool isStagger;
        }

        private void DrawTimeline()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);

            _timelineFoldout = EditorGUILayout.Foldout(
                _timelineFoldout, "Timeline Preview", true, EditorStyles.foldoutHeader);

            if (!_timelineFoldout)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(4);

            var controller = target as MasterSequenceController;
            var steps = BuildTimelineSteps(controller);

            if (steps.Count == 0)
            {
                EditorGUILayout.HelpBox("Add steps above to see the timeline.", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            float totalDur = Mathf.Max(MIN_SECS, CalcTotalDuration(steps));
            float panelH = steps.Count * (ROW_H + ROW_GAP) + RULER_H + 2f;

            Rect panel = GUILayoutUtility.GetRect(
                GUIContent.none, GUIStyle.none, GUILayout.Height(panelH), GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                float trackX = panel.x + LABEL_W;
                float trackW = panel.width - LABEL_W;

                EditorGUI.DrawRect(panel, C_BG);

                for (int i = 0; i < steps.Count; i++)
                {
                    var s = steps[i];
                    float rowY = panel.y + i * (ROW_H + ROW_GAP);

                    EditorGUI.DrawRect(new Rect(panel.x, rowY, panel.width, ROW_H),
                                       i % 2 == 0 ? C_BG : C_ALT);

                    GUI.Label(
                        new Rect(panel.x + 2f, rowY + 1f, LABEL_W - 6f, ROW_H - 2f),
                        s.label,
                        new GUIStyle(EditorStyles.miniLabel) { clipping = TextClipping.Clip });

                    if (s.delay > 0f)
                    {
                        float animStart = trackX + (s.startTime / totalDur) * trackW;
                        float delayStart = trackX + ((s.startTime - s.delay) / totalDur) * trackW;
                        delayStart = Mathf.Max(trackX, delayStart);
                        EditorGUI.DrawRect(
                            new Rect(delayStart, rowY + ROW_H * 0.30f, animStart - delayStart, ROW_H * 0.40f),
                            C_DELAY);
                    }

                    if (s.duration > 0f)
                    {
                        float bx = trackX + (s.startTime / totalDur) * trackW;
                        float bw = Mathf.Max(2f, (s.duration / totalDur) * trackW);
                        Rect br = new Rect(bx, rowY + 2f, bw, ROW_H - 4f);
                        Color barColor = s.isStagger ? C_STAGGER : (s.isJoined ? C_JOIN : C_BAR);
                        EditorGUI.DrawRect(br, barColor);

                        if (bw > 24f)
                            GUI.Label(
                                new Rect(br.x + 3f, br.y, br.width - 4f, br.height),
                                $"{s.duration:0.##}s",
                                new GUIStyle(EditorStyles.miniLabel)
                                {
                                    clipping = TextClipping.Clip,
                                    fontStyle = FontStyle.Bold,
                                    normal = { textColor = Color.white }
                                });
                    }
                }

                DrawRuler(trackX, panel.y + steps.Count * (ROW_H + ROW_GAP), trackW, totalDur);
            }

            EditorGUILayout.EndVertical();
        }

        private List<TimelineStep> BuildTimelineSteps(MasterSequenceController controller)
        {
            var result = new List<TimelineStep>();
            if (controller == null) return result;

            // Reads the live deserialized objects directly (SerializeReference
            // fields are real managed instances in edit mode too) — simpler and
            // more robust than reflecting field values off SerializedProperty.
            var steps = controller.Steps;
            if (steps == null) return result;

            float cursor = 0f;

            foreach (var step in steps)
            {
                var def = step?.ResolvedEffect;
                string lbl = def != null ? def.GetType().Name : "(empty)";
                if (lbl.StartsWith("Effect")) lbl = lbl.Substring("Effect".Length);

                float dur = def != null ? step.GetStepDuration(controller.transform) : 0f;
                bool isStagger = step != null && step.targetMode != EffectTargetMode.Single;
                float delay = step?.delay ?? 0f;

                // Mirrors MasterSequenceController.PlayInternal exactly: insert at
                // Max(0, cursor + delay), then cursor tracks the furthest
                // point reached so far.
                float start = Mathf.Max(0f, cursor + delay);
                cursor = Mathf.Max(cursor, start + dur);

                result.Add(new TimelineStep
                {
                    label = lbl,
                    startTime = start,
                    duration = dur,
                    delay = delay,
                    isJoined = delay < 0f,
                    isStagger = isStagger
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
                float tx = x + frac * w;
                float secs = frac * totalDur;
                EditorGUI.DrawRect(new Rect(tx, y, 1f, 6f), C_GRID);
                GUI.Label(
                    new Rect(tx + 2f, y + 2f, 38f, 14f),
                    secs < 1f ? $"{secs * 1000f:0}ms" : $"{secs:0.#}s",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = C_TICK_LB } });
            }
        }

        // ── Scene View gizmos ─────────────────────────────────────────────────
        // Ported from the old EffectRewardFlyEditor — drawn here per-step now
        // that EffectRewardFly is data on a step rather than its own component.

        private void OnSceneGUI()
        {
            var controller = target as MasterSequenceController;
            if (controller == null || controller.Steps == null) return;

            foreach (var step in controller.Steps)
            {
                if (step?.ResolvedEffect is EffectRewardFly rf)
                {
                    foreach (var t in step.ResolveTargets(controller.transform))
                        DrawRewardFlyGizmo(rf, t);
                }
            }
        }

        private static void DrawRewardFlyGizmo(EffectRewardFly fly, Transform source)
        {
            if (fly == null || source == null || fly.Destination == null || fly.SpawnParent == null) return;

            RectTransform parent = fly.SpawnParent;
            if (parent == null) return;

            Matrix4x4 prevMatrix = Handles.matrix;
            try
            {
                Handles.matrix = parent.localToWorldMatrix;

                Vector2 srcLocal = parent.InverseTransformPoint(source.position);
                Vector2 dstLocal = parent.InverseTransformPoint(fly.Destination.position);

                Handles.color = new Color(1f, 1f, 0.3f, 0.15f);
                Handles.DrawSolidDisc((Vector3)srcLocal, Vector3.back, fly.SpawnRadius);
                Handles.color = new Color(1f, 1f, 0.3f, 0.85f);
                Handles.DrawWireDisc((Vector3)srcLocal, Vector3.back, fly.SpawnRadius);

                Handles.color = new Color(0.3f, 1f, 0.4f);
                Handles.DrawSolidDisc((Vector3)dstLocal, Vector3.back, 8f);

                float fh = fly.FloatHeight;
                Handles.color = new Color(0.9f, 0.6f, 1f, 0.6f);
                Handles.DrawDottedLine(
                    new Vector3(srcLocal.x, srcLocal.y - fh, 0f),
                    new Vector3(srcLocal.x, srcLocal.y + fh, 0f), 4f);

                Vector2 floatLocal = srcLocal + Vector2.up * fh;

                float[] heights = { fly.ArcHeight, fly.ArcHeight + fly.ArcVariance, fly.ArcHeight - fly.ArcVariance };
                Color[] cols =
                {
                    new Color(0.3f, 0.8f, 1f, 1.0f),
                    new Color(0.3f, 0.8f, 1f, 0.35f),
                    new Color(0.3f, 0.8f, 1f, 0.35f),
                };

                for (int a = 0; a < heights.Length; a++)
                    DrawArc((Vector3)floatLocal, (Vector3)dstLocal, heights[a], cols[a]);

                var lbl = new GUIStyle(EditorStyles.miniLabel);

                lbl.normal.textColor = new Color(1f, 1f, 0.3f);
                Handles.Label(new Vector3(srcLocal.x + fly.SpawnRadius + 4f, srcLocal.y, 0f), "Source", lbl);

                lbl.normal.textColor = new Color(0.3f, 1f, 0.4f);
                Handles.Label(new Vector3(dstLocal.x + 10f, dstLocal.y, 0f), "Destination", lbl);

                lbl.normal.textColor = new Color(0.9f, 0.6f, 1f);
                Handles.Label(new Vector3(srcLocal.x + 4f, srcLocal.y + fh, 0f), $"Float ±{fh:0}", lbl);

                lbl.normal.textColor = new Color(0.3f, 0.8f, 1f);
                Handles.Label(new Vector3(
                    (floatLocal.x + dstLocal.x) * 0.5f + 4f,
                    (floatLocal.y + dstLocal.y) * 0.5f + fly.ArcHeight, 0f),
                    $"Arc {fly.ArcHeight:0}", lbl);
            }
            finally
            {
                Handles.matrix = prevMatrix;
            }
        }

        private static void DrawArc(Vector3 start, Vector3 end, float arcH, Color color)
        {
            const int steps = 32;
            Handles.color = color;
            Vector3 prev = SampleParabola(start, end, arcH, 0f);
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 curr = SampleParabola(start, end, arcH, t);
                Handles.DrawLine(prev, curr);
                prev = curr;
            }
        }

        private static Vector3 SampleParabola(Vector3 start, Vector3 end, float arcH, float t)
        {
            Vector3 straight = Vector3.Lerp(start, end, t);
            float parabola = 4f * t * (1f - t) * arcH;
            return straight + Vector3.up * parabola;
        }
    }
}
