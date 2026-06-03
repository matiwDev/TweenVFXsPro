using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Move")]
    public class EffectMove : VFXBehaviour
    {
        [Header("Move Settings")]
        [SerializeField, Tooltip("Local-space position the target snaps to at the start.")]
        private Vector3 startPosition = Vector3.zero;

        [SerializeField, Tooltip("Local-space position the target moves to by the end.")]
        private Vector3 endPosition = new Vector3(0f, 100f, 0f);


        public override float GetDuration() { return duration; }

                public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            target.localPosition = startPosition;

            Sequence seq = DOTween.Sequence();
            seq.Append(ApplyEase(target.DOLocalMove(endPosition, duration)));

            return FinaliseSequence(seq);
        }
    }
}
