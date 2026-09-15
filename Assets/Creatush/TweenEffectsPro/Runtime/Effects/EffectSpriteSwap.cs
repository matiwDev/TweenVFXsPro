using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Drives a frame-by-frame sprite animation on a UI Image — no Animator,
    /// no Animation component, no controller required.
    /// Frames are played in order from the sprites array at the rate determined
    /// by duration / frame count. Pairs naturally with the owning MasterSequenceController's Loop for
    /// idle sprite animations (coin spin, button shimmer, loading icon, etc.).
    /// </summary>
    [System.Serializable]
    public class EffectSpriteSwap : EffectDefinition
    {
        [Header("Sprite Settings")]
        [SerializeField, Tooltip("Frames to play in order. Drag your sprite sheet frames here.")]
        private Sprite[] frames;

        [SerializeField, Tooltip("Play frames in reverse order.")]
        private bool reverse = false;

        [SerializeField, Tooltip("Explicit Image override, for when it lives outside the step Target's own hierarchy entirely. Leave empty to find one on the Target or its children.")]
        private Image targetImage;

        public override float GetDuration() => duration;

        public override Sequence BuildSequence(EffectContext ctx)
        {
            Transform target = ctx.target;
            if (target == null) return null;

            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning($"[EffectSpriteSwap] No frames assigned on '{target.name}'.", target);
                return FinaliseSequence(DOTween.Sequence(), ctx.owner);
            }

            var image = targetImage != null
                ? targetImage
                : target.GetComponentInChildren<Image>(true);

            if (image == null)
            {
                Debug.LogWarning($"[EffectSpriteSwap] No Image found on '{target.name}' or its children.", target);
                return FinaliseSequence(DOTween.Sequence(), ctx.owner);
            }

            Sprite[] ordered = new Sprite[frames.Length];
            frames.CopyTo(ordered, 0);
            if (reverse) System.Array.Reverse(ordered);

            // Duration per frame — evenly distributed across total duration.
            float frameDuration = duration / ordered.Length;

            Sequence seq = DOTween.Sequence();

            foreach (var sprite in ordered)
            {
                var captured = sprite; // closure capture
                seq.AppendCallback(() => image.sprite = captured);
                seq.AppendInterval(frameDuration);
            }

            return FinaliseSequence(seq, ctx.owner);
        }
    }
}
