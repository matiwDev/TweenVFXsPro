using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [System.Serializable]
    public class EffectFillAmount : EffectDefinition
    {
        [Header("Fill Settings")]
        [SerializeField, Range(0f, 1f)] private float startFill = 0f;
        [SerializeField, Range(0f, 1f)] private float endFill = 1f;

        public override float GetDuration() => duration;

        public override Sequence BuildSequence(EffectContext ctx)
        {
            if (ctx.target == null) return null;

            var image = ctx.target.GetComponentInChildren<Image>(true);
            if (image == null)
            {
                Debug.LogWarning($"[EffectFillAmount] No Image component found on '{ctx.target.name}' or its children.", ctx.target);
                return DOTween.Sequence();
            }

            if (image.type != Image.Type.Filled)
            {
                Debug.LogWarning($"[EffectFillAmount] Image on '{ctx.target.name}' is not set to Filled type. " +
                                  "Change Image Type to Filled in the Inspector.", ctx.target);
                return DOTween.Sequence();
            }

            image.fillAmount = startFill;

            Sequence seq = DOTween.Sequence();
            seq.Append(ApplyEase(image.DOFillAmount(endFill, duration)));

            return FinaliseSequence(seq, ctx.owner);
        }
    }
}
