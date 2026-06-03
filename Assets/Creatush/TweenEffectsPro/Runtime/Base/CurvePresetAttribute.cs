using UnityEngine;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Add this attribute to an AnimationCurve field to show
    /// the EaseCurves preset picker above it in the Inspector.
    ///
    /// Usage:
    ///   [CurvePreset, SerializeField] private AnimationCurve easeCurve;
    /// </summary>
    public class CurvePresetAttribute : PropertyAttribute { }
}
