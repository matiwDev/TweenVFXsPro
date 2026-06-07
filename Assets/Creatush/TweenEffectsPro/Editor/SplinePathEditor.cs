using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Creatush.TweenEffectsPro;

namespace Creatush.TweenEffectsPro.Editor
{
    [CustomEditor(typeof(SplinePath))]
    public class SplinePathEditor : UnityEditor.Editor
    {
        // ── State ─────────────────────────────────────────────────────────────
        private SplinePath _spline;
        private SplinePathSO _asset;
        private int _selectedKnot = -1;

        // Cached styles
        private GUIStyle _headerStyle;
        private GUIStyle _knotBoxStyle;
        private GUIStyle _boldLabel;

        // ── Constants ─────────────────────────────────────────────────────────
        private const float CURVE_STEPS = 60f;
        private const float TANGENT_HANDLE_SIZE = 0.06f;
        private const float KNOT_HANDLE_SIZE = 0.08f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _spline = (SplinePath)target;
            _asset = _spline?.Asset;
            Tools.hidden = false;
        }

        private void OnDisable()
        {
            Tools.hidden = false;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            if (_spline == null) return;
            _asset = _spline.Asset;
            if (_asset == null) return;

            EnsureStyles();
            serializedObject.Update();

            DrawPathSettings();
            EditorGUILayout.Space(6);
            DrawKnotList();
            EditorGUILayout.Space(6);
            DrawAddRemoveButtons();

            if (GUI.changed)
            {
                _asset.MarkDirty();
                EditorUtility.SetDirty(_spline);
                SceneView.RepaintAll();
            }
        }

        private void DrawPathSettings()
        {
            EditorGUILayout.LabelField("⚙️  Path Settings", _headerStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginChangeCheck();

            _asset.closedLoop = EditorGUILayout.Toggle(
                new GUIContent("Closed Loop", "Connect the last knot back to the first."),
                _asset.closedLoop);

            _asset.arcPrecision = EditorGUILayout.IntSlider(
                new GUIContent("Arc Precision", "Higher = more accurate constant-speed remapping. " +
                               "Increase for very long or complex paths."),
                _asset.arcPrecision, 50, 500);

            EditorGUILayout.Space(4);
            _asset.pathColor = EditorGUILayout.ColorField("Path Colour", _asset.pathColor);
            _asset.tangentColor = EditorGUILayout.ColorField("Tangent Colour", _asset.tangentColor);
            _asset.pointSize = EditorGUILayout.Slider("Point Size", _asset.pointSize, 4f, 24f);

            if (EditorGUI.EndChangeCheck())
                _asset.MarkDirty();

            EditorGUILayout.Space(4);

            // Playback override
            _asset.overridePlayback = EditorGUILayout.Toggle(
                new GUIContent("Override Playback",
                    "When enabled, EffectFollowSpline instances referencing this asset " +
                    "will use the defaults below instead of their own settings."),
                _asset.overridePlayback);

            if (_asset.overridePlayback)
            {
                EditorGUI.indentLevel++;
                _asset.defaultSpeedMode = (SplinePathSO.SpeedMode)EditorGUILayout.EnumPopup("Speed Mode", _asset.defaultSpeedMode);
                _asset.defaultLoopMode = (SplinePathSO.LoopMode)EditorGUILayout.EnumPopup("Loop Mode", _asset.defaultLoopMode);
                _asset.defaultLoopCount = EditorGUILayout.IntField("Loop Count", _asset.defaultLoopCount);
                _asset.defaultStartOffset = EditorGUILayout.Slider("Start Offset", _asset.defaultStartOffset, 0f, 1f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawKnotList()
        {
            var knots = _asset.knots;

            EditorGUILayout.LabelField($"🔵  Knots  ({knots.Count})", _headerStyle);

            if (knots.Count == 0)
            {
                EditorGUILayout.HelpBox("No knots yet. Click 'Add Knot' to start.", MessageType.Info);
                return;
            }

            for (int i = 0; i < knots.Count; i++)
            {
                var k = knots[i];

                EditorGUILayout.BeginVertical(_knotBoxStyle);
                EditorGUILayout.BeginHorizontal();

                // Expand / collapse
                string label = $"Knot {i}";
                k.isExpanded = EditorGUILayout.Foldout(k.isExpanded, label, true);

                // Select in scene
                bool isSelected = _selectedKnot == i;
                var selStyle = isSelected
                    ? new GUIStyle(EditorStyles.miniButton) { normal = { textColor = new Color(0.3f, 0.8f, 1f) } }
                    : EditorStyles.miniButton;

                if (GUILayout.Button(isSelected ? "● Selected" : "○ Select", selStyle, GUILayout.Width(80)))
                {
                    _selectedKnot = isSelected ? -1 : i;
                    SceneView.RepaintAll();
                }

                // Delete
                GUI.color = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    Undo.RecordObject(_spline, "Delete Knot");
                    knots.RemoveAt(i);
                    _asset.MarkDirty();
                    if (_selectedKnot >= knots.Count) _selectedKnot = knots.Count - 1;
                    EditorUtility.SetDirty(_spline);
                    SceneView.RepaintAll();
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();

                if (k.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    EditorGUI.BeginChangeCheck();
                    k.point = EditorGUILayout.Vector3Field("Position", k.point);
                    k.tangentOut = EditorGUILayout.Vector3Field("Tangent Out", k.tangentOut);
                    k.tangentIn = EditorGUILayout.Vector3Field("Tangent In", k.tangentIn);
                    k.roll = EditorGUILayout.Slider("Roll (°)", k.roll, -180f, 180f);

                    if (EditorGUI.EndChangeCheck())
                        _asset.MarkDirty();

                    EditorGUI.indentLevel--;
                }

                knots[i] = k;
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        private void DrawAddRemoveButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("＋  Add Knot", GUILayout.Height(28)))
            {
                Undo.RecordObject(_spline, "Add Knot");
                var knots = _asset.knots;

                Vector3 newPos = knots.Count > 0
                    ? knots[knots.Count - 1].point + new Vector3(80f, 0f, 0f)
                    : Vector3.zero;

                knots.Add(new SplinePathSO.SplineKnot(newPos));
                _asset.MarkDirty();
                _selectedKnot = knots.Count - 1;
                EditorUtility.SetDirty(_spline);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("〰  Smooth", GUILayout.Height(28)))
                SmoothAllTangents();

            if (_asset.knots.Count > 0)
            {
                GUI.color = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("✕  Clear All", GUILayout.Height(28)))
                {
                    if (EditorUtility.DisplayDialog("Clear Spline",
                        "Delete all knots? This cannot be undone.", "Clear", "Cancel"))
                    {
                        Undo.RecordObject(_spline, "Clear Knots");
                        _asset.knots.Clear();
                        _asset.MarkDirty();
                        _selectedKnot = -1;
                        EditorUtility.SetDirty(_spline);
                        SceneView.RepaintAll();
                    }
                }
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox(
                "Smooth: recalculates all tangents using the Catmull-Rom algorithm " +
                "for natural flowing curves.", MessageType.None);
        }

        /// <summary>
        /// Recalculates tangents using Catmull-Rom so the curve flows smoothly
        /// through every knot. The tangent at knot i points from knot i-1 to i+1,
        /// scaled by a third of the distance for a natural feel.
        /// </summary>
        private void SmoothAllTangents()
        {
            var knots = _asset.knots;
            if (knots.Count < 2) return;

            Undo.RecordObject(_spline, "Smooth Spline Tangents");

            for (int i = 0; i < knots.Count; i++)
            {
                var k = knots[i];

                // Neighbours — wrap for closed loop, clamp for open
                Vector3 prev = knots[Mathf.Max(0, i - 1)].point;
                Vector3 next = knots[Mathf.Min(knots.Count - 1, i + 1)].point;

                if (_asset.closedLoop)
                {
                    prev = knots[(i - 1 + knots.Count) % knots.Count].point;
                    next = knots[(i + 1) % knots.Count].point;
                }

                // Catmull-Rom tangent direction
                Vector3 tangentDir = (next - prev).normalized;
                float scale = Vector3.Distance(prev, next) / 3f;

                k.tangentOut = tangentDir * scale;
                k.tangentIn = -tangentDir * scale;

                knots[i] = k;
            }

            _asset.MarkDirty();
            EditorUtility.SetDirty(_spline);
            SceneView.RepaintAll();
        }

        // ── Scene View ────────────────────────────────────────────────────────

        private void OnSceneGUI()
        {
            if (_spline == null) return;
            _asset = _spline.Asset;
            if (_asset == null || _asset.knots.Count < 1) return;

            // Parent matrix: knot coordinates are local to the SplinePath's parent
            Matrix4x4 matrix = _spline.transform.parent != null
                ? _spline.transform.parent.localToWorldMatrix
                : Matrix4x4.identity;

            DrawCurve(matrix);
            DrawKnotHandles(matrix);
        }

        private void DrawCurve(Matrix4x4 matrix)
        {
            var knots = _asset.knots;
            if (knots.Count < 2) return;

            int segments = _asset.closedLoop ? knots.Count : knots.Count - 1;

            Handles.color = _asset.pathColor;

            for (int s = 0; s < segments; s++)
            {
                int i0 = s % knots.Count;
                int i1 = (s + 1) % knots.Count;

                Vector3 p0 = matrix.MultiplyPoint3x4(knots[i0].point);
                Vector3 p1 = matrix.MultiplyPoint3x4(knots[i0].point + knots[i0].tangentOut);
                Vector3 p2 = matrix.MultiplyPoint3x4(knots[i1].point + knots[i1].tangentIn);
                Vector3 p3 = matrix.MultiplyPoint3x4(knots[i1].point);

                Handles.DrawBezier(p0, p3, p1, p2, _asset.pathColor, null, 2f);
            }
        }

        private void DrawKnotHandles(Matrix4x4 matrix)
        {
            var knots = _asset.knots;
            float hSize;

            for (int i = 0; i < knots.Count; i++)
            {
                var k = knots[i];
                bool isSelected = _selectedKnot == i;

                Vector3 worldPoint = matrix.MultiplyPoint3x4(k.point);

                // ── Knot position handle ──────────────────────────────────────
                hSize = HandleUtility.GetHandleSize(worldPoint) * KNOT_HANDLE_SIZE;
                Handles.color = isSelected
                    ? new Color(0.3f, 0.9f, 1f)
                    : new Color(0.9f, 0.9f, 0.2f);

                if (Handles.Button(worldPoint, Quaternion.identity, hSize, hSize * 1.5f,
                    Handles.SphereHandleCap))
                {
                    _selectedKnot = isSelected ? -1 : i;
                    Repaint();
                }

                // Label
                Handles.Label(worldPoint + Vector3.up * hSize * 3f,
                    $"  {i}", EditorStyles.miniLabel);

                if (isSelected)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newWorld = Handles.PositionHandle(worldPoint, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_spline, "Move Knot");
                        k.point = matrix.inverse.MultiplyPoint3x4(newWorld);
                        knots[i] = k;
                        _asset.MarkDirty();
                        EditorUtility.SetDirty(_spline);
                    }

                    // ── Tangent handles (shown only for selected knot) ─────────
                    DrawTangentHandle(i, ref k, matrix, isTangentOut: true);
                    DrawTangentHandle(i, ref k, matrix, isTangentOut: false);
                    knots[i] = k;
                }
            }
        }

        private void DrawTangentHandle(int knotIndex, ref SplinePathSO.SplineKnot k,
                                        Matrix4x4 matrix, bool isTangentOut)
        {
            Vector3 knotWorld = matrix.MultiplyPoint3x4(k.point);
            Vector3 tangentLocal = isTangentOut ? k.tangentOut : k.tangentIn;
            Vector3 handleWorld = matrix.MultiplyPoint3x4(k.point + tangentLocal);

            float hSize = HandleUtility.GetHandleSize(handleWorld) * TANGENT_HANDLE_SIZE;

            // Connecting line
            Handles.color = _asset.tangentColor;
            Handles.DrawDottedLine(knotWorld, handleWorld, 4f);

            // Handle cap — Unity 2022 FreeMoveHandle requires rotation parameter
            EditorGUI.BeginChangeCheck();
            Vector3 newHandleWorld = Handles.FreeMoveHandle(
                handleWorld,
                Quaternion.identity,
                hSize,
                Vector3.zero,
                Handles.CircleHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_spline, isTangentOut ? "Move Tangent Out" : "Move Tangent In");
                Vector3 newLocal = matrix.inverse.MultiplyPoint3x4(newHandleWorld) - k.point;
                if (isTangentOut) k.tangentOut = newLocal;
                else k.tangentIn = newLocal;
                _asset.MarkDirty();
                EditorUtility.SetDirty(_spline);
            }
        }

        // ── Styles ────────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            _knotBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(6, 6, 4, 4)
            };

            _boldLabel = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold
            };
        }
    }
}
