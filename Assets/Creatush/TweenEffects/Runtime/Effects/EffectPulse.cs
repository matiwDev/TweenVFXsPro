using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffects
{
    [AddComponentMenu("Creatush/Tween Effects/Behaviors/VFX Pulse")]
    public class EffectPulse : VFXBehaviour
    {
        [Header("Pulse Settings")]
        [SerializeField] private float pulseScaleFactor = 0.1f;
        [SerializeField] private Vector3 baseScale = Vector3.one;
        [SerializeField] private bool loop = true;
        [SerializeField] private int loopCount = -1;

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            Vector3 targetScale = baseScale * (1f + pulseScaleFactor);

            Sequence seq = DOTween.Sequence();

            seq.Append(target.DOScale(targetScale, duration * 0.5f).SetEase(easeType));
            seq.Append(target.DOScale(baseScale, duration * 0.5f).SetEase(easeType));

            if (loop) seq.SetLoops(loopCount, LoopType.Yoyo);

            return seq;
        }
    }
}