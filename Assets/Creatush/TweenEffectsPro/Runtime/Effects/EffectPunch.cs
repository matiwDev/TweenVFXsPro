using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Unified punch effect. Choose which axes to punch via the toggles.
    /// Any combination of position, rotation, and scale can run simultaneously.
    ///
    /// Punch effects use DOTween's elastic decay algorithm — ApplyEase is not
    /// applied here because external easing would compound with the internal
    /// oscillation and produce unpredictable results. Shape the feel via
    /// Vibrato and Elasticity instead.
    /// </summary>
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Punch")]
    public class EffectPunch : VFXBehaviour
    {
        [Header("Axes")]
        [SerializeField] private bool punchPosition = false;
        [SerializeField] private bool punchRotation = false;
        [SerializeField] private bool punchScale = true;

        [Header("Position Punch")]
        [SerializeField, Tooltip("Direction and strength in local space.")]
        private Vector3 positionPunch = new Vector3(0f, 30f, 0f);

        [Header("Rotation Punch")]
        [SerializeField, Tooltip("Axis and strength in degrees.")]
        private Vector3 rotationPunch = new Vector3(0f, 0f, 15f);

        [Header("Scale Punch")]
        [SerializeField, Tooltip("Per-axis scale delta. Positive = expand, negative = compress.")]
        private Vector3 scalePunch = new Vector3(0.2f, 0.2f, 0.2f);

        [Header("Punch Feel")]
        [SerializeField, Tooltip("Number of oscillations. Higher = more bouncy.")]
        private int vibrato = 10;

        [SerializeField, Range(0f, 1f),
         Tooltip("0 = no bounce back, 1 = full elastic return.")]
        private float elasticity = 0.5f;

        // ── VFXBehaviour ──────────────────────────────────────────────────────

        public override float GetDuration() => duration;

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            Sequence seq = DOTween.Sequence();

            if (punchPosition)
                seq.Join(target.DOPunchPosition(positionPunch, duration, vibrato, elasticity));

            if (punchRotation)
                seq.Join(target.DOPunchRotation(rotationPunch, duration, vibrato, elasticity));

            if (punchScale)
                seq.Join(target.DOPunchScale(scalePunch, duration, vibrato, elasticity));

            // If nothing is enabled, return an empty timed sequence so controllers
            // don't have an empty sequence with no duration.
            if (!punchPosition && !punchRotation && !punchScale)
                seq.AppendInterval(duration);

            return FinaliseSequence(seq);
        }
    }
}
