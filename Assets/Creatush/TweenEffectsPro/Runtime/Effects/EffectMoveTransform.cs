using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Moves a target from one RectTransform's position to another's,
    /// driven by a single AnimationCurve.
    ///
    /// This is the simplest possible A→B move: drag two RectTransforms into the
    /// Inspector as anchors — no coordinates to type, no offset arithmetic.
    /// The target snaps to Origin on play and arrives at Destination by the end.
    ///
    /// Works in both local and anchored-position space automatically:
    /// if the target has a RectTransform, anchoredPosition is used (UI);
    /// otherwise localPosition is used (world-space objects).
    /// </summary>
    [System.Serializable]
    public class EffectMoveTransform : EffectDefinition
    {
        [Header("Waypoints")]
        [SerializeField,
         Tooltip("The target snaps here at the start of the effect. " +
                 "Drag any RectTransform in the scene — its anchored position is used as the origin.")]
        private RectTransform origin;

        [SerializeField,
         Tooltip("The target moves here by the end of the effect. " +
                 "Drag any RectTransform in the scene — its anchored position is used as the destination.")]
        private RectTransform destination;

        public override float GetDuration() => duration;

        public override Sequence BuildSequence(EffectContext ctx)
        {
            if (ctx.target == null) return null;

            if (origin == null || destination == null)
            {
                Debug.LogWarning($"[EffectMoveTransform] Origin or Destination is not assigned on '{ctx.target.name}'.", ctx.target);
                return FinaliseSequence(DOTween.Sequence(), ctx.owner);
            }

            var targetRect = ctx.target.GetComponent<RectTransform>();

            if (targetRect != null)
            {
                // UI path: drive anchoredPosition so layout is respected
                targetRect.anchoredPosition = origin.anchoredPosition;

                Sequence seq = DOTween.Sequence();
                seq.Append(ApplyEase(
                    targetRect.DOAnchorPos(destination.anchoredPosition, duration)));
                return FinaliseSequence(seq, ctx.owner);
            }
            else
            {
                // World-space path: drive localPosition
                ctx.target.localPosition = origin.position;

                Sequence seq = DOTween.Sequence();
                seq.Append(ApplyEase(
                    ctx.target.DOLocalMove(destination.position, duration)));
                return FinaliseSequence(seq, ctx.owner);
            }
        }
    }
}
