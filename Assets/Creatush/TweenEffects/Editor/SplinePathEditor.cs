using UnityEngine;
using UnityEditor;
using DG.Tweening; // Explicitly included for DOVirtual

namespace Creatush.TweenEffects.Editor
{
    [CustomEditor(typeof(SplinePath))]
    public class SplinePathEditor : UnityEditor.Editor
    {
        private SplinePath spline;
        private enum PlayState { Stopped, Playing, Paused }
        private PlayState currentState = PlayState.Stopped;
        private float previewT = 0;
        private double lastUpdateTime;
        private bool showKnots = true;

        private void OnEnable() => spline = (SplinePath)target;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.color = new Color(0.4f, 1f, 1f);
            GUILayout.Label("SPLINE ENGINE V2", EditorStyles.boldLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndVertical();

            DrawTransportBar();

            EditorGUILayout.LabelField("General Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pathColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pointSize"));

            EditorGUILayout.Space(10);

            showKnots = EditorGUILayout.BeginFoldoutHeaderGroup(showKnots, "Path Nodes & Coordinates");
            if (showKnots)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                for (int i = 0; i < spline.knots.Count; i++)
                {
                    var knot = spline.knots[i];
                    EditorGUILayout.BeginHorizontal();
                    knot.isExpanded = EditorGUILayout.Foldout(knot.isExpanded, $"Node {i}", true);
                    if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        Undo.RecordObject(spline, "Remove Node");
                        spline.knots.RemoveAt(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (knot.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        knot.point = EditorGUILayout.Vector3Field("Position", knot.point);
                        knot.tangentOut = EditorGUILayout.Vector3Field("Tangent Out", knot.tangentOut);
                        EditorGUI.indentLevel--;
                        EditorGUILayout.Space(5);
                    }
                    spline.knots[i] = knot;
                }

                if (GUILayout.Button("+ Add New Node", GUILayout.Height(25)))
                {
                    Undo.RecordObject(spline, "Add Node");
                    Vector3 last = spline.knots.Count > 0 ? spline.knots[spline.knots.Count - 1].point : Vector3.zero;
                    spline.knots.Add(new SplinePath.SplineKnot(last + Vector3.right * 100));
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                spline.BakePath();
                EditorUtility.SetDirty(spline);
            }
        }

        private void DrawTransportBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string playLabel = (currentState == PlayState.Paused) ? "Resume" : (currentState == PlayState.Playing ? "Pause" : "Play");

            if (GUILayout.Button(playLabel, EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                if (currentState == PlayState.Playing) currentState = PlayState.Paused;
                else StartPreview();
            }

            if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(60))) StopPreview();

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Progress: {previewT:F2}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void StartPreview()
        {
            if (currentState == PlayState.Stopped) previewT = 0;
            currentState = PlayState.Playing;
            lastUpdateTime = EditorApplication.timeSinceStartup;
            EditorApplication.update -= UpdatePreview;
            EditorApplication.update += UpdatePreview;
            spline.BakePath();
        }

        private void StopPreview()
        {
            currentState = PlayState.Stopped;
            previewT = 0;
            EditorApplication.update -= UpdatePreview;
            ResetObject();
        }

        private void UpdatePreview()
        {
            if (spline == null || currentState != PlayState.Playing) return;

            double deltaTime = EditorApplication.timeSinceStartup - lastUpdateTime;
            lastUpdateTime = EditorApplication.timeSinceStartup;

            var behavior = spline.GetComponent<FollowSplinePath>();
            float dur = (behavior != null) ? behavior.duration : 2f;
            previewT += (float)(deltaTime / dur);
            float t = Mathf.Clamp01(previewT);

            float easedT = t;
            SplinePath.SpeedMode mode = SplinePath.SpeedMode.ConstantAcrossPath;

            if (behavior != null)
            {
                mode = behavior.speedMode;
                if (mode == SplinePath.SpeedMode.ConstantAcrossPath)
                {
                    easedT = DOVirtual.EasedValue(0, 1, t, behavior.easeType);
                }
                else
                {
                    float segs = spline.knots.Count - 1;
                    float sT = t * segs;
                    int sIdx = Mathf.FloorToInt(sT);
                    float easedLocal = DOVirtual.EasedValue(0, 1, sT - sIdx, behavior.easeType);
                    easedT = (sIdx + easedLocal) / segs;
                }
            }

            Vector3 pos = spline.GetPointOnPath(Mathf.Clamp01(easedT), mode);

            RectTransform rect = spline.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = pos;
            else spline.transform.localPosition = pos;

            if (behavior != null)
                spline.transform.localScale = Vector3.one * behavior.scaleOverPath.Evaluate(t);

            if (previewT >= 1.0f) StopPreview();
            SceneView.RepaintAll();
        }

        private void ResetObject()
        {
            if (spline.knots.Count == 0) return;
            RectTransform rect = spline.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = spline.knots[0].point;
            else spline.transform.localPosition = spline.knots[0].point;
            spline.transform.localScale = Vector3.one;
        }

        private void OnSceneGUI()
        {
            if (spline == null || spline.knots.Count < 2) return;
            Matrix4x4 tr = (spline.transform.parent != null) ? spline.transform.parent.localToWorldMatrix : Matrix4x4.identity;

            Handles.color = spline.pathColor;
            // FIXED: Passing SpeedMode.EqualPerSegment instead of 'false'
            Vector3 last = tr.MultiplyPoint3x4(spline.GetPointOnPath(0, SplinePath.SpeedMode.EqualPerSegment));
            for (int i = 1; i <= 50; i++)
            {
                Vector3 next = tr.MultiplyPoint3x4(spline.GetPointOnPath(i / 50f, SplinePath.SpeedMode.EqualPerSegment));
                Handles.DrawLine(last, next, 3f);
                last = next;
            }

            for (int i = 0; i < spline.knots.Count; i++)
            {
                var knot = spline.knots[i];
                Vector3 wPos = tr.MultiplyPoint3x4(knot.point);
                Vector3 wTan = tr.MultiplyPoint3x4(knot.point + knot.tangentOut);

                EditorGUI.BeginChangeCheck();
                Handles.color = spline.pathColor;
                Vector3 nPos = Handles.FreeMoveHandle(wPos, Quaternion.identity, spline.pointSize * 1.5f, Vector3.zero, Handles.SphereHandleCap);

                Handles.color = Color.white;
                Handles.DrawDottedLine(wPos, wTan, 4f);
                Vector3 nTan = Handles.FreeMoveHandle(wTan, Quaternion.identity, spline.pointSize * 0.5f, Vector3.zero, Handles.RectangleHandleCap);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(spline, "Edit Path");
                    knot.point = (spline.transform.parent != null) ? spline.transform.parent.InverseTransformPoint(nPos) : nPos;
                    knot.tangentOut = ((spline.transform.parent != null) ? spline.transform.parent.InverseTransformPoint(nTan) : nTan) - knot.point;
                    spline.knots[i] = knot;
                    spline.BakePath();
                    EditorUtility.SetDirty(spline);
                }
            }
        }
    }
}