using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [System.Serializable]
    public class EffectMove : EffectDefinition
    {
        [Header("Move Settings")]
        [SerializeField, Tooltip("Local-space position the target snaps to at the start.")]
        private Vector3 startPosition = Vector3.zero;

        [SerializeField, Tooltip("Local-space position the target moves to by the end.")]
        private Vector3 endPosition = new Vector3(0f, 100f, 0f);

        public override float GetDuration() => duration;

        public override Sequence BuildSequence(EffectContext ctx)
        {
            if (ctx.target == null) return null;

            ctx.target.localPosition = startPosition;

            Sequence seq = DOTween.Sequence();
            seq.Append(ApplyEase(ctx.target.DOLocalMove(endPosition, duration)));

            return FinaliseSequence(seq, ctx.owner);
        }
    }
}
