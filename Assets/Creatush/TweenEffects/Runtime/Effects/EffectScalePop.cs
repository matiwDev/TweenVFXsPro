using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffects
{
    [AddComponentMenu("Creatush/Tween Effects/Behaviors/VFX Scale Pop")]
    public class EffectScalePop : VFXBehaviour
    {
        [Header("Scale Settings")]
        [SerializeField] private Vector3 startScale = Vector3.zero;
        [SerializeField] private Vector3 endScale = Vector3.one;

        [Range(1f, 2f)]
        [SerializeField, Tooltip("How much the scale bounces past the end value.")]
        private float overshoot = 1.2f;

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            // --- THE FIX ---
            // Force the scale to the start value immediately when the sequence is built.
            // This prevents the 1-frame "flicker" of the original size.
            target.localScale = startScale;

            Sequence seq = DOTween.Sequence();

            // 1. Pop to overshoot (Fast)
            seq.Append(target.DOScale(endScale * overshoot, duration * 0.6f).SetEase(Ease.OutQuad));

            // 2. Settle back to final scale (Smooth)
            seq.Append(target.DOScale(endScale, duration * 0.4f).SetEase(Ease.OutBack));

            return seq;
        }
    }
}