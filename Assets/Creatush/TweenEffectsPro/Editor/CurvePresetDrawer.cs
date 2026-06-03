using UnityEngine;
using Creatush.TweenEffectsPro;
using UnityEditor;

namespace Creatush.TweenEffectsPro.Editor
{
    [CustomPropertyDrawer(typeof(CurvePresetAttribute))]
    public class CurvePresetDrawer : PropertyDrawer
    {
        private static readonly string[]  _names;
        private static          int       _selected = 0;
        private static          GUIStyle  _labelStyle;  // cached — never allocate in OnGUI

        private static GUIStyle LabelStyle
        {
            get
            {
                if (_labelStyle == null)
                    _labelStyle = new GUIStyle(EditorStyles.label);
                return _labelStyle;
            }
        }

        static CurvePresetDrawer()
        {
            _names = new string[EaseCurves.All.Length];
            for (int i = 0; i < EaseCurves.All.Length; i++)
                _names[i] = EaseCurves.All[i].name;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Picker row + standard curve field row
            return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing * 3f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.AnimationCurve)
            {
                EditorGUI.HelpBox(position,
                    "[CurvePreset] only works on AnimationCurve fields.", MessageType.Error);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            float lineH  = EditorGUIUtility.singleLineHeight;
            float gap    = EditorGUIUtility.standardVerticalSpacing;

            // ── Preset picker row ─────────────────────────────────────────────
            Rect pickerRect = new Rect(position.x, position.y, position.width, lineH);

            float labelW   = EditorGUIUtility.labelWidth;
            float buttonW  = 60f;
            float popupW   = pickerRect.width - labelW - buttonW - gap;

            EditorGUI.LabelField(
                new Rect(pickerRect.x, pickerRect.y, labelW, lineH),
                "Curve Preset");

            _selected = EditorGUI.Popup(
                new Rect(pickerRect.x + labelW, pickerRect.y, popupW, lineH),
                _selected, _names);

            if (GUI.Button(
                new Rect(pickerRect.x + labelW + popupW + gap, pickerRect.y, buttonW, lineH),
                "Apply"))
            {
                property.animationCurveValue = EaseCurves.All[_selected].factory();
            }

            // ── Curve field ───────────────────────────────────────────────────
            Rect curveRect = new Rect(
                position.x,
                position.y + lineH + gap,
                position.width,
                lineH);

            EditorGUI.PropertyField(curveRect, property, new GUIContent(" "));

            EditorGUI.EndProperty();
        }
    }
}
