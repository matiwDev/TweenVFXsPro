using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Plays a sprite sheet animation on a UI Image.
    /// No Animator, no Animation component required.
    ///
    /// Supports:
    ///   - Forward and reverse playback
    ///   - Finite or infinite looping with configurable interval between loops
    ///   - Per-loop and on-complete events
    ///   - Ping-pong playback mode
    ///   - Speed multiplier at runtime
    /// </summary>
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Sprite Animation")]
    [RequireComponent(typeof(Image))]
    public class EffectSpriteAnimation : VFXBehaviour
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

        [SerializeField, Tooltip("Hold the last frame when the animation ends (non-loop).")]
        private bool holdLastFrame = true;

        // ── Events ────────────────────────────────────────────────────────────

        [Header("Events")]
        [SerializeField, Tooltip("Fired when the animation plays through all frames once.")]
        private UnityEvent onAnimationComplete;

        [SerializeField, Tooltip("Fired at the end of each loop iteration.\n" +
                                  "Only fires when loop count is not -1 (infinite).")]
        private UnityEvent onLoopEnd;

        // ── Internal ──────────────────────────────────────────────────────────

        private Image _image;
        private int _completedLoops;

        // ── VFXBehaviour ──────────────────────────────────────────────────────

        public override float GetDuration()
        {
            if (frames == null || frames.Length == 0) return 0f;
            float singlePlayDuration = frames.Length / (float)Mathf.Max(1, frameRate) / Mathf.Max(0.1f, speed);
            return singlePlayDuration;
        }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            _image = target.GetComponent<Image>();
            if (_image == null)
            {
                Debug.LogWarning($"[EffectSpriteAnimation] No Image found on '{target.name}'.", this);
                return FinaliseSequence(DOTween.Sequence());
            }

            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning($"[EffectSpriteAnimation] No frames assigned on '{name}'.", this);
                return FinaliseSequence(DOTween.Sequence());
            }

            _completedLoops = 0;

            Sequence seq = DOTween.Sequence();
            seq.Append(BuildAnimationTween(target));
            return FinaliseSequence(seq);
        }

        // ── Animation tween ───────────────────────────────────────────────────

        private Tween BuildAnimationTween(Transform target)
        {
            int frameCount = frames.Length;
            float frameDur = 1f / Mathf.Max(1, frameRate) / Mathf.Max(0.1f, speed);
            float totalDur = frameDur * frameCount;

            Sprite[] playbackFrames = BuildPlaybackFrames();

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
                _completedLoops++;

                onAnimationComplete?.Invoke();

                // Fire onLoopEnd only for finite loops (so user can track progress)
                if (loop && loopCount > 0)
                    onLoopEnd?.Invoke();

                // Hold last frame if not looping
                if (!loop && holdLastFrame)
                    _image.sprite = playbackFrames[playbackFrames.Length - 1];
            });

            // Apply loop settings directly to the tween rather than the sequence
            // so loopInterval works correctly between frame cycles
            if (loop)
            {
                LoopType lt = playbackMode == PlaybackMode.PingPong
                    ? LoopType.Yoyo
                    : LoopType.Restart;

                if (loopInterval > 0f)
                {
                    // For interval support we wrap in a sequence
                    Sequence loopSeq = DOTween.Sequence();
                    loopSeq.Append(t);
                    loopSeq.AppendInterval(loopInterval);
                    loopSeq.SetLoops(loopCount, LoopType.Restart);
                    return loopSeq;
                }

                t.SetLoops(loopCount, lt);
            }

            return t;
        }

        private Sprite[] BuildPlaybackFrames()
        {
            Sprite[] result = new Sprite[frames.Length];
            frames.CopyTo(result, 0);

            if (playbackMode == PlaybackMode.Reverse)
                System.Array.Reverse(result);
            // PingPong is handled by DOTween LoopType.Yoyo on the tween

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
