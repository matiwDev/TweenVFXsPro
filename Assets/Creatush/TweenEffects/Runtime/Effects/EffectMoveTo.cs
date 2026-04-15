using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffects
{
    [AddComponentMenu("Creatush/Tween Effects/Behaviors/VFX Move To")]
    public class EffectMoveTo : VFXBehaviour
    {
        public enum MoveMode { Offset, TargetTransform }

        [Header("Movement Mode")]
        [SerializeField] private MoveMode mode = MoveMode.Offset;

        [Tooltip("Used if mode is Offset.")]
        [SerializeField] private Vector3 targetOffset;

        [Tooltip("Used if mode is TargetTransform. The object will fly to this RectTransform's position.")]
        [SerializeField] private RectTransform targetDestination;

        [Header("Settings")]
        [SerializeField] private bool useLocalPosition = true;

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            Sequence seq = DOTween.Sequence();
            Vector3 endPos;

            if (mode == MoveMode.TargetTransform && targetDestination != null)
            {
                // If it's a UI element moving to another UI element, 
                // we use the destination's position.
                endPos = targetDestination.position;

                // If the user wants local movement but provided a world target, 
                // we move in world space anyway to ensure accuracy, 
                // OR we convert the world position to the parent's local space.
                if (useLocalPosition && target.parent != null)
                {
                    endPos = target.parent.InverseTransformPoint(endPos);
                    seq.Append(target.DOLocalMove(endPos, duration).SetEase(easeType));
                }
                else
                {
                    seq.Append(target.DOMove(endPos, duration).SetEase(easeType));
                }
            }
            else
            {
                // Fallback to the original Offset logic
                endPos = useLocalPosition ? target.localPosition + targetOffset : target.position + targetOffset;

                if (useLocalPosition)
                    seq.Append(target.DOLocalMove(endPos, duration).SetEase(easeType));
                else
                    seq.Append(target.DOMove(endPos, duration).SetEase(easeType));
            }

            return seq;
        }
    }
}