using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [System.Serializable]
    public class EffectColorTint : EffectDefinition
    {
        [Header("Color Settings")]
        [SerializeField] private Color startColor = Color.white;
        [SerializeField] private Color endColor = new Color(1f, 0.3f, 0.3f, 1f);

        public override float GetDuration() => duration;

        public override Sequence BuildSequence(EffectContext ctx)
        {
            if (ctx.target == null) return null;

            // Works with any Unity UI Graphic (Image, Text, RawImage, etc.) —
            // searches the target and its children, since the graphic to tint
            // is often a child of whatever the step's Target actually points at.
            var graphic = ctx.target.GetComponentInChildren<Graphic>(true);
            if (graphic == null)
            {
                Debug.LogWarning($"[EffectColorTint] No Graphic component found on '{ctx.target.name}' or its children.", ctx.target);
                return DOTween.Sequence();
            }

            graphic.color = startColor;

            Sequence seq = DOTween.Sequence();
            seq.Append(ApplyEase(graphic.DOColor(endColor, duration)));

            return FinaliseSequence(seq, ctx.owner);
        }
    }
}
