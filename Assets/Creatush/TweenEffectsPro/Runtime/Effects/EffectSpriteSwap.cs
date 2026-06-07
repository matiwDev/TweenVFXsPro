using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Drives a frame-by-frame sprite animation on a UI Image — no Animator,
    /// no Animation component, no controller required.
    /// Frames are played in order from the sprites array at the rate determined
    /// by duration / frame count. Pairs naturally with the loop system for
    /// idle sprite animations (coin spin, button shimmer, loading icon, etc.).
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Sprite Swap")]
    public class EffectSpriteSwap : VFXBehaviour
    {
        [Header("Sprite Settings")]
        [SerializeField, Tooltip("Frames to play in order. Drag your sprite sheet frames here.")]
        private Sprite[] frames;

        [SerializeField, Tooltip("Play frames in reverse order.")]
        private bool reverse = false;

        [SerializeField, Tooltip("Target Image component. Defaults to one on the target Transform if empty.")]
        private Image targetImage;


        public override float GetDuration() { return duration; }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning($"[EffectSpriteSwap] No frames assigned on '{name}'.", this);
                return FinaliseSequence(DOTween.Sequence());
            }

            var image = targetImage != null
                ? targetImage
                : target.GetComponent<Image>();

            if (image == null)
            {
                Debug.LogWarning($"[EffectSpriteSwap] No Image found on '{target.name}'.", target);
                return FinaliseSequence(DOTween.Sequence());
            }

            var frameList = reverse
                ? System.Array.AsReadOnly(frames) // reversed below
                : System.Array.AsReadOnly(frames);

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

            return FinaliseSequence(seq);
        }
    }
}
