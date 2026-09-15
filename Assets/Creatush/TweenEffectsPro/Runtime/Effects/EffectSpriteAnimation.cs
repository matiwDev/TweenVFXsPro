using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Plays a sprite sheet animation on a UI Image, once per play.
    /// No Animator, no Animation component required.
    ///
    /// Supports:
    ///   - Forward, Reverse, and PingPong (forward then back) playback order
    ///   - Hold-last-frame or not once the pass completes
    ///   - On-complete event
    ///   - Speed multiplier at runtime
    ///
    /// Repeated/continuous playback is a whole-sequence concern: enable Loop
    /// on the owning MasterSequenceController to replay this (and any other steps in
    /// the same controller) rather than looping this effect on its own.
    /// </summary>
    [System.Serializable]
    public class EffectSpriteAnimation : EffectDefinition
    {
        // ── Frames ────────────────────────────────────────────────────────────

        [Header("Frames")]
        [SerializeField, Tooltip("Ordered sprite frames. First frame is shown at t=0.")]
        private Sprite[] frames;

        [SerializeField, Min(1), Tooltip("Frames per second.")]
        private int frameRate = 12;

        // ── Playback ──────────────────────────────────────────────────────────

        [Header("Playback")]
        [SerializeField]
        private PlaybackMode playbackMode = PlaybackMode.Forward;

        [SerializeField, Range(0.1f, 8f),
         Tooltip("Playback speed multiplier. 2 = twice as fast, 0.5 = half speed.")]
        private float speed = 1f;

        [SerializeField, Tooltip("Keep showing the last frame once the pass completes, " +
                                  "instead of whatever the Image showed beforehand.")]
        private bool holdLastFrame = true;

        // ── Events ────────────────────────────────────────────────────────────

        [Header("Events")]
        [SerializeField, Tooltip("Fired when the animation finishes its one pass through the frames.")]
        private UnityEvent onAnimationComplete;

        // ── Internal ──────────────────────────────────────────────────────────

        private Image _image;
        private Sprite _spriteBeforePlay;

        // ── EffectDefinition ──────────────────────────────────────────────────

        public override float GetDuration()
        {
            int n = PlaybackFrameCount();
            if (n == 0) return 0f;
            return n / (float)Mathf.Max(1, frameRate) / Mathf.Max(0.1f, speed);
        }

        public override Sequence BuildSequence(EffectContext ctx)
        {
            Transform target = ctx.target;
            if (target == null) return null;

            _image = target.GetComponentInChildren<Image>(true);
            if (_image == null)
            {
                Debug.LogWarning($"[EffectSpriteAnimation] No Image found on '{target.name}' or its children.", target);
                return FinaliseSequence(DOTween.Sequence(), ctx.owner);
            }

            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning($"[EffectSpriteAnimation] No frames assigned on '{target.name}'.", target);
                return FinaliseSequence(DOTween.Sequence(), ctx.owner);
            }

            _spriteBeforePlay = _image.sprite;

            Sequence seq = DOTween.Sequence();
            seq.Append(BuildAnimationTween());
            return FinaliseSequence(seq, ctx.owner);
        }

        // ── Animation tween ───────────────────────────────────────────────────

        private Tween BuildAnimationTween()
        {
            Sprite[] playbackFrames = BuildPlaybackFrames();
            float totalDur = GetDuration();

            int frameIndex = 0;
            Tween t = DOTween.To(
                getter: () => frameIndex,
                setter: v =>
                {
                    frameIndex = Mathf.Clamp(v, 0, playbackFrames.Length - 1);
                    _image.sprite = playbackFrames[frameIndex];
                },
                endValue: playbackFrames.Length - 1,
                duration: totalDur
            ).SetEase(Ease.Linear);

            t.OnComplete(() =>
            {
                onAnimationComplete?.Invoke();

                if (!holdLastFrame)
                    _image.sprite = _spriteBeforePlay;
            });

            return t;
        }

        /// <summary>Frame count actually played, including PingPong's return trip.</summary>
        private int PlaybackFrameCount()
        {
            if (frames == null) return 0;
            int n = frames.Length;
            return playbackMode == PlaybackMode.PingPong && n > 1 ? 2 * n - 1 : n;
        }

        /// <summary>
        /// Builds the frame sequence actually shown, one pass, no repetition:
        /// Forward as-is, Reverse flipped, PingPong forward then back to the
        /// first frame (a single round trip, not an oscillation).
        /// </summary>
        private Sprite[] BuildPlaybackFrames()
        {
            int n = frames.Length;

            if (playbackMode == PlaybackMode.PingPong && n > 1)
            {
                var pingPong = new Sprite[2 * n - 1];
                for (int i = 0; i < n; i++) pingPong[i] = frames[i];
                for (int i = 1; i < n; i++) pingPong[n - 1 + i] = frames[n - 1 - i];
                return pingPong;
            }

            Sprite[] result = new Sprite[n];
            frames.CopyTo(result, 0);

            if (playbackMode == PlaybackMode.Reverse)
                System.Array.Reverse(result);

            return result;
        }

        // ── Runtime control ───────────────────────────────────────────────────

        /// <summary>Change playback speed at runtime.</summary>
        public void SetSpeed(float multiplier)
        {
            speed = Mathf.Max(0.1f, multiplier);
        }
    }

    public enum PlaybackMode
    {
        Forward,
        Reverse,
        PingPong
    }
}
