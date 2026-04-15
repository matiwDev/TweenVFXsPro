using UnityEngine;
using UnityEditor;

namespace Creatush.TweenEffects.Editor
{
    [CustomEditor(typeof(VFXBehaviour), true)]
    [CanEditMultipleObjects]
    public class VFXBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (target == null) return;
            VFXBehaviour behavior = (VFXBehaviour)target;

            // --- SMART CONTROLLER CHECK ---
            var single = behavior.GetComponentInParent<SingleEffectController>();
            var seq = behavior.GetComponentInParent<SequenceEffectsController>();

            if (single == null && seq == null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("⚠️ No Controller found on this object or any parent.", MessageType.Warning);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Add Single Player", GUILayout.Height(25)))
                {
                    Undo.AddComponent<SingleEffectController>(behavior.gameObject);
                }

                if (GUILayout.Button("Add Sequence Controller", GUILayout.Height(25)))
                {
                    Undo.AddComponent<SequenceEffectsController>(behavior.gameObject);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("Use 'Single' for one-off effects or 'Sequence' to chain behaviors.", MessageType.None);
                EditorGUILayout.Space(5);
            }

            serializedObject.Update();

            // --- TIMING & EASING SECTION ---
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🕒 TIMING & EASING", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("easeType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));

            EditorGUILayout.EndVertical();

            // --- EFFECT SETTINGS SECTION ---
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🎨 EFFECT SETTINGS", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.textArea);

            DrawPropertiesExcluding(serializedObject, "m_Script", "easeType", "duration");

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}