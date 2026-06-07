using UnityEngine;
using UnityEditor;
using Creatush.TweenEffectsPro;

namespace Creatush.TweenEffectsPro.Editor
{
    [CustomEditor(typeof(EffectRewardFly))]
    public class EffectRewardFlyEditor : UnityEditor.Editor
    {
        // ── Properties ────────────────────────────────────────────────────────
        private SerializedProperty _itemPrefab, _itemCount, _spawnParent;
        private SerializedProperty _spriteFrames, _spriteFrameRate;
        private SerializedProperty _spawnRadius, _spawnRandomness, _spawnStagger;
        private SerializedProperty _popInOnSpawn, _popInDuration;
        private SerializedProperty _floatDuration, _floatHeight, _floatSpeed;
        private SerializedProperty _destination, _flyDuration;
        private SerializedProperty _arcHeight, _arcVariance, _flyEase;
        private SerializedProperty _flyArrivalScale, _flyStagger;
        private SerializedProperty _onAllArrived, _onItemArrived;

        // ── Foldout state ─────────────────────────────────────────────────────
        private static bool _fItem = true;
        private static bool _fSprite = false;
        private static bool _fSpawn = true;
        private static bool _fFloat = true;
        private static bool _fFly = true;
        private static bool _fEvents = false;

        // ── Cached styles ─────────────────────────────────────────────────────
        private GUIStyle _header;
        private GUIStyle _box;

        private void OnEnable()
        {
            _itemPrefab = P("itemPrefab");
            _itemCount = P("itemCount");
            _spawnParent = P("spawnParent");
            _spriteFrames = P("spriteFrames");
            _spriteFrameRate = P("spriteFrameRate");
            _spawnRadius = P("spawnRadius");
            _spawnRandomness = P("spawnRandomness");
            _spawnStagger = P("spawnStagger");
            _popInOnSpawn = P("popInOnSpawn");
            _popInDuration = P("popInDuration");
            _floatDuration = P("floatDuration");
            _floatHeight = P("floatHeight");
            _floatSpeed = P("floatSpeed");
            _destination = P("destination");
            _flyDuration = P("flyDuration");
            _arcHeight = P("arcHeight");
            _arcVariance = P("arcVariance");
            _flyEase = P("flyEase");
            _flyArrivalScale = P("flyArrivalScale");
            _flyStagger = P("flyStagger");
            _onAllArrived = P("onAllArrived");
            _onItemArrived = P("onItemArrived");
        }

        private SerializedProperty P(string n) => serializedObject.FindProperty(n);

        private void EnsureStyles()
        {
            if (_header != null) return;
            _header = new GUIStyle(EditorStyles.foldoutHeader)
            { fontStyle = FontStyle.Bold, fontSize = 11 };
            _box = new GUIStyle(EditorStyles.helpBox);
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            EnsureStyles();
            serializedObject.Update();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("🎁  Reward Fly",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });
            EditorGUILayout.Space(4);

            Section("📦  Items", ref _fItem, DrawItems);
            Section("🎞  Sprite Sheet", ref _fSprite, DrawSprite);
            Section("✨  Spawn", ref _fSpawn, DrawSpawn);
            Section("🌊  Float", ref _fFloat, DrawFloat);
            Section("🚀  Fly", ref _fFly, DrawFly);
            Section("📣  Events", ref _fEvents, DrawEvents);

            EditorGUILayout.Space(4);

            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("▶  Play", GUILayout.Height(28)))
                    ((EffectRewardFly)target).Play();
                if (GUILayout.Button("■  Stop", GUILayout.Height(28), GUILayout.Width(70)))
                    ((EffectRewardFly)target).Stop();
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void Section(string label, ref bool state, System.Action draw)
        {
            EditorGUILayout.BeginVertical(_box);
            bool next = EditorGUILayout.Foldout(state, label, true, _header);
            if (next != state) state = next;
            if (state) { EditorGUILayout.Space(2); draw(); EditorGUILayout.Space(2); }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        // ── Section content ───────────────────────────────────────────────────

        private void DrawItems()
        {
            EditorGUILayout.PropertyField(_itemPrefab,
                new GUIContent("Prefab", "Item to spawn from the pool."));
            EditorGUILayout.PropertyField(_itemCount,
                new GUIContent("Count"));
            EditorGUILayout.PropertyField(_spawnParent,
                new GUIContent("Spawn Parent",
                    "RectTransform parent for pooled items. Must be inside a Canvas."));
            if (_spawnParent.objectReferenceValue == null)
                EditorGUILayout.HelpBox(
                    "Spawn Parent required — assign a Canvas or RectTransform inside one.",
                    MessageType.Warning);
        }

        private void DrawSprite()
        {
            EditorGUILayout.PropertyField(_spriteFrames,
                new GUIContent("Frames",
                    "Sprite sheet frames cycled while the item is visible. " +
                    "Leave empty to use the prefab sprite as-is."));
            if (_spriteFrames.arraySize > 0)
                EditorGUILayout.PropertyField(_spriteFrameRate,
                    new GUIContent("Frame Rate", "Frames per second."));
        }

        private void DrawSpawn()
        {
            EditorGUILayout.PropertyField(_spawnRadius,
                new GUIContent("Radius", "Scatter radius in canvas units."));
            EditorGUILayout.PropertyField(_spawnRandomness,
                new GUIContent("Randomness", "0 = tight cluster, 1 = full radius."));
            EditorGUILayout.PropertyField(_spawnStagger,
                new GUIContent("Spawn Stagger", "Seconds between item spawns."));
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(_popInOnSpawn,
                new GUIContent("Pop In", "Scale from 0 on spawn."));
            if (_popInOnSpawn.boolValue)
                EditorGUILayout.PropertyField(_popInDuration,
                    new GUIContent("  Duration"));
        }

        private void DrawFloat()
        {
            EditorGUILayout.PropertyField(_floatDuration,
                new GUIContent("Duration (s)", "How long items float before flying."));
            EditorGUILayout.PropertyField(_floatHeight,
                new GUIContent("Height", "Oscillation amplitude in canvas units."));
            EditorGUILayout.PropertyField(_floatSpeed,
                new GUIContent("Speed", "Oscillation cycles per second."));
        }

        private void DrawFly()
        {
            EditorGUILayout.PropertyField(_destination,
                new GUIContent("Destination", "Where items fly to."));
            if (_destination.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Destination required.", MessageType.Warning);

            EditorGUILayout.PropertyField(_flyDuration,
                new GUIContent("Duration (s)"));
            EditorGUILayout.PropertyField(_flyEase);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Arc", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_arcHeight,
                new GUIContent("Height",
                    "Peak arc height in canvas units. 0 = straight line. " +
                    "Visible as a blue arc in the Scene View."));
            EditorGUILayout.PropertyField(_arcVariance,
                new GUIContent("Variance",
                    "Random height variation per item. Shown as faded arcs in Scene View."));

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_flyArrivalScale,
                new GUIContent("Arrival Scale", "0 = shrink to nothing on arrival."));
            EditorGUILayout.PropertyField(_flyStagger,
                new GUIContent("Fly Stagger", "Stagger between items starting their fly."));
        }

        private void DrawEvents()
        {
            EditorGUILayout.PropertyField(_onAllArrived);
            EditorGUILayout.PropertyField(_onItemArrived);
        }

        // ── Scene View Gizmo ──────────────────────────────────────────────────

        private void OnSceneGUI()
        {
            if (target == null) return;
            var fly = (EffectRewardFly)target;
            if (fly == null || fly.Destination == null || fly.SpawnParent == null) return;

            RectTransform parent = fly.SpawnParent;
            if (parent == null) return;

            // Always reset matrix at the end — use try/finally to guarantee it
            Matrix4x4 prevMatrix = Handles.matrix;
            try
            {
                Handles.matrix = parent.localToWorldMatrix;

                Vector2 srcLocal = parent.InverseTransformPoint(fly.transform.position);
                Vector2 dstLocal = parent.InverseTransformPoint(fly.Destination.position);

                // ── Spawn radius ──────────────────────────────────────────────────
                Handles.color = new Color(1f, 1f, 0.3f, 0.15f);
                Handles.DrawSolidDisc((Vector3)srcLocal, Vector3.back, fly.SpawnRadius);
                Handles.color = new Color(1f, 1f, 0.3f, 0.85f);
                Handles.DrawWireDisc((Vector3)srcLocal, Vector3.back, fly.SpawnRadius);

                // ── Destination dot ───────────────────────────────────────────────
                Handles.color = new Color(0.3f, 1f, 0.4f);
                Handles.DrawSolidDisc((Vector3)dstLocal, Vector3.back, 8f);

                // ── Float range indicator ─────────────────────────────────────────
                float fh = fly.FloatHeight;
                Handles.color = new Color(0.9f, 0.6f, 1f, 0.6f);
                Handles.DrawDottedLine(
                    new Vector3(srcLocal.x, srcLocal.y - fh, 0f),
                    new Vector3(srcLocal.x, srcLocal.y + fh, 0f), 4f);

                // ── Arc previews ──────────────────────────────────────────────────
                Vector2 floatLocal = srcLocal + Vector2.up * fh;

                float[] heights = { fly.ArcHeight, fly.ArcHeight + fly.ArcVariance,
                                 fly.ArcHeight - fly.ArcVariance };
                Color[] cols =
                {
                new Color(0.3f, 0.8f, 1f, 1.0f),
                new Color(0.3f, 0.8f, 1f, 0.35f),
                new Color(0.3f, 0.8f, 1f, 0.35f),
            };

                for (int a = 0; a < heights.Length; a++)
                    DrawArc((Vector3)floatLocal, (Vector3)dstLocal, heights[a], cols[a]);

                // ── Labels ────────────────────────────────────────────────────────
                var lbl = new GUIStyle(EditorStyles.miniLabel);

                lbl.normal.textColor = new Color(1f, 1f, 0.3f);
                Handles.Label(new Vector3(srcLocal.x + fly.SpawnRadius + 4f, srcLocal.y, 0f),
                    "Source", lbl);

                lbl.normal.textColor = new Color(0.3f, 1f, 0.4f);
                Handles.Label(new Vector3(dstLocal.x + 10f, dstLocal.y, 0f),
                    "Destination", lbl);

                lbl.normal.textColor = new Color(0.9f, 0.6f, 1f);
                Handles.Label(new Vector3(srcLocal.x + 4f, srcLocal.y + fh, 0f),
                    $"Float ±{fh:0}", lbl);

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
