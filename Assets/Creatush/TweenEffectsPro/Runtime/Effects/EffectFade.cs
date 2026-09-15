using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [System.Serializable]
    public class EffectFade : EffectDefinition
    {
        [Header("Fade Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("Alpha at the start of the effect.")]
        private float startAlpha = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("Alpha at the end of the effect.")]
        private float endAlpha = 1f;

        public override float GetDuration() => duration;

        public override Sequence BuildSequence(EffectContext ctx)
        {
            if (ctx.target == null) return null;

            var cg = ctx.target.GetComponentInChildren<CanvasGroup>(true);
            if (cg == null)
            {
                Debug.LogWarning($"[EffectFade] No CanvasGroup found on '{ctx.target.name}' or its children. " +
                                  "Add one or use EffectColorTint for non-canvas objects.", ctx.target);
                return DOTween.Sequence();
            }

            cg.alpha = startAlpha;

            Sequence seq = DOTween.Sequence();
            seq.Append(ApplyEase(cg.DOFade(endAlpha, duration)));

            return FinaliseSequence(seq, ctx.owner);
        }
    }
}
