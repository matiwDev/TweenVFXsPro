using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Creatush.TweenEffectsPro.Editor
{
    /// <summary>
    /// Shared effect-type listing/naming/picker used anywhere a concrete
    /// EffectDefinition needs to be chosen for a [SerializeReference] field —
    /// an MasterSequenceController step's inline effect, or an EffectPresetSO's
    /// wrapped effect. One place to keep the "nice name" formatting and the
    /// type-picker dropdown consistent between both.
    /// </summary>
    internal static class EffectTypeMenu
    {
        public static List<System.Type> GetConcreteEffectTypes() =>
            TypeCache.GetTypesDerivedFrom<EffectDefinition>()
                .Where(t => !t.IsAbstract)
                .OrderBy(NiceName)
                .ToList();

        /// <summary>Human-readable name from a managed-reference SerializedProperty, e.g. "EffectFade" -> "Fade". "(no effect)" if unset.</summary>
        public static string NiceName(SerializedProperty effectProp)
        {
            if (effectProp == null || string.IsNullOrEmpty(effectProp.managedReferenceFullTypename))
                return "(no effect)";

            string full = effectProp.managedReferenceFullTypename;
            int spaceIdx = full.LastIndexOf(' ');
            string typeName = spaceIdx >= 0 ? full.Substring(spaceIdx + 1) : full;
            int dotIdx = typeName.LastIndexOf('.');
            if (dotIdx >= 0) typeName = typeName.Substring(dotIdx + 1);

            if (typeName.StartsWith("Effect")) typeName = typeName.Substring("Effect".Length);
            if (string.IsNullOrEmpty(typeName)) return "(no effect)";

            return Spaced(typeName);
        }

        /// <summary>Human-readable name from a concrete Type, e.g. typeof(EffectFade) -> "Fade".</summary>
        public static string NiceName(System.Type t)
        {
            string n = t.Name;
            if (n.StartsWith("Effect")) n = n.Substring("Effect".Length);
            if (string.IsNullOrEmpty(n)) n = t.Name;
            return Spaced(n);
        }

        private static string Spaced(string n)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < n.Length; i++)
            {
                char c = n[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(n[i - 1]))
                    sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Shows a dropdown of every concrete EffectDefinition type; onPicked receives the chosen Type (not yet instantiated) so the caller decides how to apply it.</summary>
        public static void ShowPicker(Rect buttonRect, System.Action<System.Type> onPicked)
        {
            var menu = new GenericMenu();
            foreach (var type in GetConcreteEffectTypes())
            {
                var captured = type;
                menu.AddItem(new GUIContent(NiceName(captured)), false, () => onPicked(captured));
            }
            menu.DropDown(buttonRect);
        }
    }
}
