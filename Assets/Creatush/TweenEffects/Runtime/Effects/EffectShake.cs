using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffects
{
    [AddComponentMenu("Creatush/Tween Effects/Behaviors/VFX Shake")]
    public class EffectShake : VFXBehaviour
    {
        [Header("Shake Settings")]
        [SerializeField] private float rotationStrength = 10f;
        [SerializeField] private float scaleStrength = 0.05f;
        [SerializeField] private int vibrato = 10;
        [SerializeField] private bool loopIndefinitely = false;

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            Sequence seq = DOTween.Sequence();

            // Append the shake logic
            seq.Append(target.DOShakeRotation(duration, rotationStrength, vibrato));
            seq.Join(target.DOShakeScale(duration, scaleStrength, vibrato));

            if (loopIndefinitely)
            {
                seq.SetLoops(-1, LoopType.Restart);
            }

            return seq;
        }
    }
}