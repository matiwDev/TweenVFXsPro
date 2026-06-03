using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Unified scale effect covering three common patterns:
    ///
    ///   ScaleIn   — animate from startScale to endScale once (entry animation).
    ///   Pulse     — animate to endScale and back to startScale, optionally looping.
    ///   Pop       — ScaleIn with an overshoot bounce for a springy feel.
    ///
    /// The Mode field selects the pattern. All three share the same
    /// start/end scale fields and respect the base ease and loop settings.
    /// </summary>
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Scale Pop")]
    public class EffectScalePop : VFXBehaviour
    {
        public enum ScaleMode
        {
            [Tooltip("Animate from Start Scale to End Scale once.")]
            ScaleIn,
            [Tooltip("Animate to End Scale then back — loops naturally.")]
            Pulse,
            [Tooltip("ScaleIn with an elastic overshoot. 'Bouncy' entry feel.")]
            Pop
        }

        [Header("Scale Settings")]
        [SerializeField] private ScaleMode mode       = ScaleMode.ScaleIn;
        [SerializeField] private Vector3   startScale = Vector3.zero;
        [SerializeField] private Vector3   endScale   = Vector3.one;

        [Header("Pop Settings")]
        [SerializeField, Range(1f, 3f),
         Tooltip("How far past End Scale the pop overshoots before settling.\n" +
                 "Only used in Pop mode.")]
        private float overshoot = 1.2f;

        [SerializeField, Min(0.01f),
         Tooltip("Duration of the settle-back phase after the overshoot.\n" +
                 "Only used in Pop mode.")]
        private float settleDuration = 0.15f;

        // ── VFXBehaviour ──────────────────────────────────────────────────────

        public override float GetDuration()
        {
            return mode switch
            {
                ScaleMode.Pulse => duration * 2f,
                ScaleMode.Pop   => duration + settleDuration,
                _               => duration
            };
        }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            target.localScale = startScale;

            Sequence seq = DOTween.Sequence();

            switch (mode)
            {
                case ScaleMode.ScaleIn:
                    seq.Append(ApplyEase(target.DOScale(endScale, duration)));
                    break;

                case ScaleMode.Pulse:
                    seq.Append(ApplyEase(target.DOScale(endScale,   duration)));
                    seq.Append(ApplyEase(target.DOScale(startScale, duration)));
                    break;

                case ScaleMode.Pop:
                    // Phase 1: scale to overshoot peak using user ease
                    Vector3 peak = endScale * overshoot;
                    seq.Append(ApplyEase(target.DOScale(peak,     duration)));
                    // Phase 2: settle to end scale with OutBack for the springy tail
                    seq.Append(target.DOScale(endScale, settleDuration).SetEase(Ease.OutBack));
                    break;
            }

            return FinaliseSequence(seq);
        }
    }
}
