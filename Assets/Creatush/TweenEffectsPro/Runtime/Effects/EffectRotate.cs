using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Rotate")]
    public class EffectRotate : VFXBehaviour
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


        public override float GetDuration() { return duration; }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            target.localEulerAngles = startRotation;

            Sequence seq = DOTween.Sequence();
            seq.Append(ApplyEase(
                target.DOLocalRotate(endRotation, duration, rotateMode)));

            return FinaliseSequence(seq);
        }
    }
}
