using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Fill Amount")]
    public class EffectFillAmount : VFXBehaviour
    {
        [Header("Fill Settings")]
        [SerializeField, Range(0f, 1f)] private float startFill = 0f;
        [SerializeField, Range(0f, 1f)] private float endFill = 1f;


        public override float GetDuration() { return duration; }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            var image = target.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogWarning($"[EffectFillAmount] No Image component found on '{target.name}'.", target);
                return DOTween.Sequence();
            }

            if (image.type != Image.Type.Filled)
            {
                Debug.LogWarning($"[EffectFillAmount] Image on '{target.name}' is not set to Filled type. " +
                                  "Change Image Type to Filled in the Inspector.", target);
                return DOTween.Sequence();
            }

            image.fillAmount = startFill;

            Sequence seq = DOTween.Sequence();
            seq.Append(ApplyEase(image.DOFillAmount(endFill, duration)));

            return FinaliseSequence(seq);
        }
    }
}
