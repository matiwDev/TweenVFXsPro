using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Creatush.TweenEffects.Editor
{
    [CustomEditor(typeof(SingleEffectController))]
    [CanEditMultipleObjects]
    public class SingleControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (target == null) return;
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🎮 SINGLE EFFECT PLAYER", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Triggers a single behavior. Perfect for buttons and simple UI feedback.", MessageType.Info);
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(SequenceEffectsController))]
    public class SequenceControllerEditor : UnityEditor.Editor
    {
        private ReorderableList list;
        private SerializedProperty sequenceSteps;

        private void OnEnable()
        {
            // Critical Safety: Don't initialize if the target is being destroyed/reloaded
            if (target == null || serializedObject == null) return;

            sequenceSteps = serializedObject.FindProperty("sequenceSteps");
            if (sequenceSteps == null) return;

            list = new ReorderableList(serializedObject, sequenceSteps, true, true, true, true);

            list.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "🎬 Animation Sequence Steps", EditorStyles.boldLabel);
            };

            list.elementHeightCallback = (index) => EditorGUIUtility.singleLineHeight + 4;

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // Double safety check inside the loop
                if (sequenceSteps == null || index >= sequenceSteps.arraySize) return;

                var element = sequenceSteps.GetArrayElementAtIndex(index);
                rect.y += 2;

                float spacing = 8;
                float behaviorWidth = rect.width * 0.55f;
                float delayWidth = rect.width * 0.20f - spacing;

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, behaviorWidth, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("behavior"), GUIContent.none);

                float delayX = rect.x + behaviorWidth + spacing;
                EditorGUI.PropertyField(
                    new Rect(delayX, rect.y, delayWidth, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("delay"), GUIContent.none);

                float joinX = rect.x + behaviorWidth + delayWidth + (spacing * 2);
                EditorGUI.LabelField(new Rect(joinX, rect.y, 32, EditorGUIUtility.singleLineHeight), "Join");
                EditorGUI.PropertyField(
                    new Rect(joinX + 32, rect.y, 20, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("joinPrevious"), GUIContent.none);
            };
        }

        public override void OnInspectorGUI()
        {
            // 1. Fundamental Safety Check
            if (target == null || serializedObject == null) return;

            serializedObject.Update();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("⛓️ SEQUENCE ORCHESTRATOR", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetOverride"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 2. Column Labels using safe Layout calls
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            float totalWidth = EditorGUIUtility.currentViewWidth - 55;

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold, normal = { textColor = Color.gray } };

            GUILayout.Label("EFFECT BEHAVIOR", labelStyle, GUILayout.Width(totalWidth * 0.55f));
            GUILayout.Label("DELAY (S)", labelStyle, GUILayout.Width(totalWidth * 0.20f));
            GUILayout.Label("JOIN", labelStyle, GUILayout.Width(totalWidth * 0.15f));
            EditorGUILayout.EndHorizontal();

            // 3. Prevent drawing during Layout events if the list is null
            if (list != null && sequenceSteps != null)
            {
                try
                {
                    list.DoLayoutList();
                }
                catch
                {
                    // Silently catch layout mismatches during scene transitions
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}