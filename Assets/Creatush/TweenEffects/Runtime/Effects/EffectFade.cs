using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffects
{
    [AddComponentMenu("Creatush/Tween Effects/Behaviors/VFX Fade")]
    public class EffectFade : VFXBehaviour
    {
        [Header("Alpha Settings")]
        [SerializeField, Range(0f, 1f)] private float startAlpha = 0f;
        [SerializeField, Range(0f, 1f)] private float endAlpha = 1f;

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            Sequence seq = DOTween.Sequence();

            if (target.TryGetComponent(out CanvasGroup group))
            {
                group.alpha = startAlpha;
                seq.Append(group.DOFade(endAlpha, duration).SetEase(easeType));
            }
            else if (target.TryGetComponent(out SpriteRenderer sprite))
            {
                Color c = sprite.color;
                c.a = startAlpha;
                sprite.color = c;
                seq.Append(sprite.DOFade(endAlpha, duration).SetEase(easeType));
            }

            return seq;
        }
    }
}