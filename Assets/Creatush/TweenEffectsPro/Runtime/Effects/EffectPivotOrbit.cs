using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Orbits the target around a pivot offset from its own position.
    /// A temporary pivot GameObject is created at runtime for the duration
    /// of the effect and cleaned up automatically on complete or kill.
    /// </summary>
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Pivot Orbit")]
    public class EffectPivotOrbit : VFXBehaviour
    {
        [Header("Orbit Settings")]
        [SerializeField, Tooltip("Offset from the target's world position that acts as the pivot.")]
        private Vector3 pivotOffset = new Vector3(100f, 0f, 0f);

        [SerializeField, Tooltip("Degrees to rotate around the pivot. 360 = full orbit.")]
        private float degrees = 360f;

        [SerializeField, Tooltip("World-space axis to rotate around.")]
        private Vector3 axis = Vector3.forward;


        public override float GetDuration() { return duration; }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            GameObject pivotGO       = new GameObject("_OrbitPivot");
            Transform  pivot         = pivotGO.transform;
            Transform  originalParent = target.parent;
            int        originalIdx   = target.GetSiblingIndex();

            pivot.SetParent(target.parent, worldPositionStays: true);
            pivot.position = target.position + pivotOffset;
            target.SetParent(pivot, worldPositionStays: true);

            void Restore()
            {
                if (target == null) return;
                target.SetParent(originalParent, worldPositionStays: true);
                target.SetSiblingIndex(originalIdx);
                Object.Destroy(pivotGO);
            }

            Sequence seq = DOTween.Sequence();
            seq.Append(ApplyEase(
                pivot.DORotate(axis.normalized * degrees, duration, RotateMode.FastBeyond360)));

            seq.OnComplete(() => Restore());
            seq.OnKill(()    => Restore());

            return FinaliseSequence(seq);
        }
    }
}
