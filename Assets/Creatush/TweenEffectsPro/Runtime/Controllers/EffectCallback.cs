using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// A zero-duration VFXBehaviour that fires a UnityEvent when the sequence
    /// reaches it. Use this to trigger any external system — EffectRewardFly,
    /// audio, haptics, score updates — from inside a SequenceEffectsController.
    ///
    /// Example setup:
    ///   SequenceEffectsController steps:
    ///     1. EffectScalePop   — panel entry
    ///     2. EffectCallback   — OnTrigger → EffectRewardFly.Play()
    ///     3. EffectFade       — panel exit
    /// </summary>
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Callback")]
    public class EffectCallback : VFXBehaviour
    {
        [Header("Callback")]
        [SerializeField, Tooltip("Fired when the sequence reaches this step.")]
        private UnityEvent onTrigger;

        [SerializeField, Min(0f),
         Tooltip("Optional wait time after the callback fires before the sequence continues.\n" +
                 "0 = fire and immediately continue to the next step.")]
        private float waitAfter = 0f;

        public override float GetDuration() { return waitAfter; }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            Sequence seq = DOTween.Sequence();

            seq.AppendCallback(() => onTrigger?.Invoke());

            if (waitAfter > 0f)
                seq.AppendInterval(waitAfter);

            return FinaliseSequence(seq);
        }
    }
}
