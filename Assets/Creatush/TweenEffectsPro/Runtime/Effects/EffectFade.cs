using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Fade")]
    public class EffectFade : VFXBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("Alpha at the start of the effect.")]
        private float startAlpha = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("Alpha at the end of the effect.")]
        private float endAlpha = 1f;


        public override float GetDuration() { return duration; }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            var cg = target.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                Debug.LogWarning($"[EffectFade] No CanvasGroup found on '{target.name}'. " +
                                  "Add one or use EffectColorTint for non-canvas objects.", target);
                return DOTween.Sequence();
            }

            cg.alpha = startAlpha;

            Sequence seq = DOTween.Sequence();
            seq.Append(ApplyEase(cg.DOFade(endAlpha, duration)));

            return FinaliseSequence(seq);
        }
    }
}
