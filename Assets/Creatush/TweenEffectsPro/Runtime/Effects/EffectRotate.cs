using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [System.Serializable]
    public class EffectRotate : EffectDefinition
    {
        [Header("Rotate Settings")]
        [SerializeField, Tooltip("Local-space Euler angles the target snaps to at the start.")]
        private Vector3 startRotation = Vector3.zero;

        [SerializeField, Tooltip("Local-space Euler angles the target rotates to by the end.")]
        private Vector3 endRotation = new Vector3(0f, 0f, 360f);

        [SerializeField, Tooltip(
            "Full360: shortest path, stops at end.\n" +
            "FastBeyond360: spins N full turns — use for continuous spin effects.\n" +
            "LocalAxisAdd: adds the end value to the current rotation each loop.")]
        private RotateMode rotateMode = RotateMode.FastBeyond360;

        public override float GetDuration() => duration;

        public override Sequence BuildSequence(EffectContext ctx)
        {
            if (ctx.target == null) return null;

            ctx.target.localEulerAngles = startRotation;

            Sequence seq = DOTween.Sequence();
            seq.Append(ApplyEase(
                ctx.target.DOLocalRotate(endRotation, duration, rotateMode)));

            return FinaliseSequence(seq, ctx.owner);
        }
    }
}
