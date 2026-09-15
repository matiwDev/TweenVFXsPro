using UnityEditor;
using UnityEngine;

namespace Creatush.TweenEffectsPro.Editor
{
    /// <summary>
    /// Custom editor for EffectPresetSO. The plain [SerializeReference] field
    /// on its own shows nothing useful in the default inspector — there's no
    /// built-in way to pick a concrete EffectDefinition type without already
    /// knowing to right-click the field. This draws the same type-picker used
    /// when adding a step to an MasterSequenceController, so a fresh preset asset is
    /// actually configurable.
    /// </summary>
    [CustomEditor(typeof(EffectPresetSO))]
    public class EffectPresetSOEditor : UnityEditor.Editor
    {
        private static readonly Color C_DIVIDER = new Color(0.5f, 0.5f, 0.5f, 0.25f);

        private SerializedProperty _effect;
        private GUIStyle _titleStyle;

        private void OnEnable()
        {
            _effect = serializedObject.FindProperty("effect");
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;
            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(0, 0, 2, 2)
            };
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null || _effect == null) return;

            EnsureStyles();
            serializedObject.Update();

            bool hasEffect = !string.IsNullOrEmpty(_effect.managedReferenceFullTypename);
            string typeLabel = EffectTypeMenu.NiceName(_effect);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Effect Preset", _titleStyle);
            Rect lineRect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(lineRect, C_DIVIDER);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Effect Type", hasEffect ? typeLabel : "(none)");

            string buttonLabel = hasEffect ? "Change" : "Set";
            Rect btnRect = GUILayoutUtility.GetRect(
                new GUIContent(buttonLabel), GUI.skin.button, GUILayout.Width(70));

            if (GUI.Button(btnRect, buttonLabel))
            {
                EffectTypeMenu.ShowPicker(btnRect, pickedType =>
                {
                    serializedObject.Update();
                    _effect.managedReferenceValue = System.Activator.CreateInstance(pickedType);
                    serializedObject.ApplyModifiedProperties();
                });
            }
            EditorGUILayout.EndHorizontal();

            if (hasEffect)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.PropertyField(_effect, GUIContent.none, true);
            }
            else
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Pick an effect type above, then configure it here — the same effect " +
                    "you'd add inline on an MasterSequenceController step. Drag this asset into a " +
                    "step's Preset field to use it there.",
                    MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
