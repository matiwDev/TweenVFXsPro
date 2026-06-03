using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace Creatush.TweenEffectsPro
{
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Crossfade")]
    public class EffectCrossfade : VFXBehaviour
    {
        [Header("Groups")]
        [SerializeField] private List<CanvasGroup> fadeOutGroups;
        [SerializeField] private List<CanvasGroup> fadeInGroups;

        [Header("Crossfade Timing")]
        [SerializeField, Min(0f),
         Tooltip("How far before the fade-out ends that the fade-in begins.")]
        private float overlap = 0.3f;

        [SerializeField, Min(0.01f)]
        private float fadeInDuration = 0.8f;

        // 'duration' from base drives the fade-out duration.


        public override float GetDuration() { return duration; }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            Sequence seq = DOTween.Sequence();

            foreach (var cg in fadeOutGroups)
                if (cg) seq.Join(ApplyEase(cg.DOFade(0f, duration)));

            float insertAt = Mathf.Clamp(duration - overlap, 0f, duration);
            seq.InsertCallback(insertAt, () =>
            {
                foreach (var cg in fadeInGroups)
                {
                    if (!cg) continue;
                    cg.alpha = 0f;
                    ApplyEase(cg.DOFade(1f, fadeInDuration));
                }
            });

            return FinaliseSequence(seq);
        }
    }
}
