using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [RequireComponent(typeof(UnityEngine.UI.Graphic))]
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Color Tint")]
    public class EffectColorTint : VFXBehaviour
    {
        [Header("Color Settings")]
        [SerializeField] private Color startColor = Color.white;
        [SerializeField] private Color endColor = new Color(1f, 0.3f, 0.3f, 1f);


        public override float GetDuration() { return duration; }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            // Works with any Unity UI Graphic (Image, Text, RawImage, etc.)
            var graphic = target.GetComponent<Graphic>();
            if (graphic == null)
            {
                Debug.LogWarning($"[EffectColorTint] No Graphic component found on '{target.name}'.", target);
                return DOTween.Sequence();
            }

            graphic.color = startColor;

            Sequence seq = DOTween.Sequence();
            seq.Append(ApplyEase(graphic.DOColor(endColor, duration)));

            return FinaliseSequence(seq);
        }
    }
}
