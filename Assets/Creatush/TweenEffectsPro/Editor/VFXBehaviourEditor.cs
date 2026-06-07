using UnityEngine;
using UnityEditor;
using Creatush.TweenEffectsPro;

namespace Creatush.TweenEffectsPro.Editor
{
    [CustomEditor(typeof(VFXBehaviour), true)]
    [CanEditMultipleObjects]
    public class VFXBehaviourEditor : UnityEditor.Editor
    {
        // ── Cached properties ─────────────────────────────────────────────────
        private SerializedProperty _duration;
        private SerializedProperty _easeMode;
        private SerializedProperty _ease;
        private SerializedProperty _easeCurve;
        private SerializedProperty _loop;
        private SerializedProperty _loopCount;
        private SerializedProperty _loopInterval;

        // ── Foldout state (SessionState so it persists across selection) ──────
        private static readonly string KEY_TIMING = "VFXEd.Timing";
        private static readonly string KEY_LOOP = "VFXEd.Loop";
        private static readonly string KEY_EFFECT = "VFXEd.Effect";

        private bool _foldTiming;
        private bool _foldLoop;
        private bool _foldEffect;

        // ── Cached settings ───────────────────────────────────────────────────
        private TweenSettingsSO _settings;

        // ── Styles (cached to avoid per-frame allocation) ─────────────────────
        private GUIStyle _foldoutStyle;
        private GUIStyle _sectionStyle;

        private void OnEnable()
        {
            if (serializedObject == null) return;

            _duration = serializedObject.FindProperty("duration");
            _easeMode = serializedObject.FindProperty("easeMode");
            _ease = serializedObject.FindProperty("ease");
            _easeCurve = serializedObject.FindProperty("easeCurve");
            _loop = serializedObject.FindProperty("loop");
            _loopCount = serializedObject.FindProperty("loopCount");
            _loopInterval = serializedObject.FindProperty("loopInterval");

            _settings = Resources.Load<TweenSettingsSO>("TweenSettings");

            // Restore foldout state from session
            _foldTiming = SessionState.GetBool(KEY_TIMING, true);
            _foldLoop = SessionState.GetBool(KEY_LOOP, false);
            _foldEffect = SessionState.GetBool(KEY_EFFECT, true);
        }

        private void EnsureStyles()
        {
            if (_foldoutStyle != null) return;

            _foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11,
            };

            _sectionStyle = new GUIStyle(EditorStyles.helpBox);
        }

        // ── Main Inspector ────────────────────────────────────────────────────
        public override void OnInspectorGUI()
        {
            if (target == null) return;
            EnsureStyles();

            serializedObject.Update();

            DrawGlobalSettingsPanel();
            DrawControllerCheck();

            EditorGUILayout.Space(4);

            DrawFoldoutSection("🕒  Timing & Easing", KEY_TIMING, ref _foldTiming, DrawTimingEasing);
            DrawFoldoutSection("🔁  Loop", KEY_LOOP, ref _foldLoop, DrawLoop);
            DrawFoldoutSection("🎨  Effect Settings", KEY_EFFECT, ref _foldEffect, DrawEffectSettings);

            serializedObject.ApplyModifiedProperties();
        }

        // ── Foldout helper ────────────────────────────────────────────────────

        private void DrawFoldoutSection(string label, string key, ref bool state, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(_sectionStyle);

            bool newState = EditorGUILayout.Foldout(state, label, true, _foldoutStyle);
            if (newState != state)
            {
                state = newState;
                SessionState.SetBool(key, state);
            }

            if (state)
            {
                EditorGUILayout.Space(2);
                drawContent();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        // ── Section content ───────────────────────────────────────────────────

        private void DrawTimingEasing()
        {
            EditorGUILayout.PropertyField(_duration);

            if (_settings != null && !Mathf.Approximately(_settings.globalTimeScale, 1f))
            {
                float effective = _duration.floatValue / _settings.globalTimeScale;
                EditorGUILayout.LabelField(
                    "  Effective Duration",
                    $"{effective:0.##}s  (global scale × {_settings.globalTimeScale})",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_easeMode);

            bool useCurve = _easeMode != null &&
                            _easeMode.enumValueIndex == (int)EaseMode.Curve;

            if (useCurve)
                EditorGUILayout.PropertyField(_easeCurve, new GUIContent("Ease Curve"));
            else
                EditorGUILayout.PropertyField(_ease, new GUIContent("Ease Preset"));
        }

        private void DrawLoop()
        {
            EditorGUILayout.PropertyField(_loop,
                new GUIContent("Loop", "Repeat this effect after it completes."));

            if (_loop != null && _loop.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(_loopCount,
                    new GUIContent("Count", "Number of repetitions. -1 = infinite."));
                GUILayout.Space(8);
                EditorGUILayout.PropertyField(_loopInterval,
                    new GUIContent("Interval (s)",
                        "Seconds between end of one play and start of the next."));
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawEffectSettings()
        {
            DrawPropertiesExcluding(serializedObject,
                "m_Script",
                "duration",
                "easeMode", "ease", "easeCurve",
                "loop", "loopCount", "loopInterval");
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
                EditorGUILayout.LabelField("⚙️  Global Settings", _foldoutStyle,
                    GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Select", GUILayout.Width(52), GUILayout.Height(18)))
                {
                    Selection.activeObject = _settings;
                    EditorGUIUtility.PingObject(_settings);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                DrawChip("Time Scale", _settings.globalTimeScale.ToString("0.##"),
                    !Mathf.Approximately(_settings.globalTimeScale, 1f));
                DrawChip("Unscaled", _settings.useUnscaledTime ? "ON" : "off",
                    _settings.useUnscaledTime);
                DrawChip("Safe Mode", _settings.safeMode ? "ON" : "off", false);
                DrawChip("Capacity",
                    $"{_settings.tweenersCapacity}/{_settings.sequencesCapacity}", false);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        // ── Controller check ──────────────────────────────────────────────────

        private void DrawControllerCheck()
        {
            if (targets.Length > 1) return; // skip for multi-select

            var behaviour = (VFXBehaviour)target;
            if (HasController(behaviour)) return;

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "⚠️ No Controller found on this object or any parent.",
                MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Single", GUILayout.Height(24)))
                AddController<SingleEffectController>(behaviour);
            if (GUILayout.Button("Add Sequence", GUILayout.Height(24)))
                AddController<SequenceEffectsController>(behaviour);
            if (GUILayout.Button("Add Parallel", GUILayout.Height(24)))
                AddController<ParallelEffectsController>(behaviour);
            if (GUILayout.Button("Add Stagger", GUILayout.Height(24)))
                AddController<StaggerEffectsController>(behaviour);
            EditorGUILayout.EndHorizontal();
        }

        private static bool HasController(VFXBehaviour behaviour)
        {
            // Search self + all parents (GetComponentInParent includes self)
            if (behaviour.GetComponentInParent<SingleEffectController>() != null) return true;
            if (behaviour.GetComponentInParent<SequenceEffectsController>() != null) return true;
            if (behaviour.GetComponentInParent<ParallelEffectsController>() != null) return true;
            if (behaviour.GetComponentInParent<StaggerEffectsController>() != null) return true;
            return false;
        }

        private static void AddController<T>(VFXBehaviour behaviour) where T : Component
        {
            Undo.AddComponent<T>(behaviour.gameObject);
            // Force the inspector to repaint so the warning disappears immediately
            EditorUtility.SetDirty(behaviour.gameObject);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void DrawChip(string label, string value, bool highlight)
        {
            // Reuse a local style — no static to avoid cross-instance state
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = highlight
                    ? new Color(1f, 0.85f, 0.2f)
                    : new Color(0.6f, 0.6f, 0.6f) }
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
    }
}
