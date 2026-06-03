using UnityEngine;
using Creatush.TweenEffectsPro;
using UnityEditor;
using System.Collections.Generic;

namespace Creatush.TweenEffectsPro.Editor
{
    [CustomPropertyDrawer(typeof(CatalogueKeyAttribute))]
    public class CatalogueKeyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(position, "[CatalogueKey] only works on string fields.", MessageType.Error);
                return;
            }

            var attr      = (CatalogueKeyAttribute)attribute;
            var catalogue = GetCatalogue(property, attr.catalogueFieldName);

            if (catalogue == null)
            {
                // No catalogue found — fall back to plain text field with a warning tint
                EditorGUI.BeginProperty(position, label, property);
                float w = position.width;
                EditorGUI.PropertyField(
                    new Rect(position.x, position.y, w * 0.75f, position.height),
                    property, label);
                EditorGUI.LabelField(
                    new Rect(position.x + w * 0.76f, position.y, w * 0.24f, position.height),
                    "No catalogue",
                    new GUIStyle(EditorStyles.miniLabel)
                        { normal = { textColor = new Color(1f, 0.6f, 0.2f) } });
                EditorGUI.EndProperty();
                return;
            }

            // Build key list from catalogue
            var keys    = new List<string> { "(none)" };
            foreach (var entry in catalogue.Entries)
                if (!string.IsNullOrEmpty(entry.key)) keys.Add(entry.key);

            EditorGUI.BeginProperty(position, label, property);

            int current = keys.IndexOf(property.stringValue);
            if (current < 0) current = 0;

            int selected = EditorGUI.Popup(position, label.text, current, keys.ToArray());
            property.stringValue = selected == 0 ? string.Empty : keys[selected];

            // Warn if the saved key no longer exists in the catalogue
            if (selected > 0 && catalogue.Get(property.stringValue) == null)
            {
                var warningRect = new Rect(
                    position.x, position.y + EditorGUIUtility.singleLineHeight + 2f,
                    position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.HelpBox(warningRect,
                    $"Key '{property.stringValue}' not found in catalogue.", MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUIUtility.singleLineHeight;
            // Extra row if key is missing from catalogue
            var attr      = (CatalogueKeyAttribute)attribute;
            var catalogue = GetCatalogue(property, attr.catalogueFieldName);
            if (catalogue != null
                && !string.IsNullOrEmpty(property.stringValue)
                && catalogue.Get(property.stringValue) == null)
                h += EditorGUIUtility.singleLineHeight + 4f;
            return h;
        }

        private static EffectCatalogue GetCatalogue(SerializedProperty property, string fieldName)
        {
            var targetObj = property.serializedObject.targetObject;
            var field     = targetObj.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);
            return field?.GetValue(targetObj) as EffectCatalogue;
        }
    }
}
