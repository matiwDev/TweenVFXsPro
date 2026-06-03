using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Abstract base for all effects in Creatush TweenEffects Pro.
    ///
    /// Preset layer
    /// ─────────────
    /// Assign an EffectDefinitionSO to share configuration across prefabs.
    /// When a preset is assigned its values are copied into the local fields
    /// at the start of BuildSequence — local fields still show in the Inspector
    /// for per-instance tweaking, but the preset wins on play unless overridden.
    ///
    /// Duration
    /// ─────────
    /// Subclasses must implement GetDuration() returning the wall-clock length
    /// of one play. Controllers use this instead of building and killing a
    /// throwaway sequence just to read the duration.
    ///
    /// Leak prevention
    /// ───────────────
    /// FinaliseSequence() links every sequence to this GameObject via
    /// SetLink(KillOnDisable). OnDisable belt-and-suspenders kills any remainder.
    /// </summary>
    public abstract class VFXBehaviour : MonoBehaviour
    {

        // ── Timing ────────────────────────────────────────────────────────────

        [Header("Timing")]
        [SerializeField, Min(0.01f),
         Tooltip("How long one play of this effect takes, in seconds.")]
        protected float duration = 0.5f;

        // ── Ease ──────────────────────────────────────────────────────────────

        [Header("Ease")]
        [SerializeField,
         Tooltip("Preset: pick a standard DOTween ease.\n" +
                 "Curve: draw your own — X = normalised time (0–1), Y = normalised value (0–1).")]
        protected EaseMode easeMode = EaseMode.Preset;

        [SerializeField]
        protected Ease ease = Ease.OutQuad;

        [CurvePreset,
         SerializeField,
         Tooltip("Active when Ease Mode is Curve.\n" +
                 "X = normalised time (0–1)  Y = normalised value (0–1).\n" +
                 "Push Y above 1 for overshoot. Hard step = snap.")]
        protected AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ── Loop ──────────────────────────────────────────────────────────────

        [Header("Loop")]
        [SerializeField]
        protected bool loop = false;

        [SerializeField, Tooltip("-1 = infinite.")]
        protected int loopCount = -1;

        [SerializeField, Min(0f),
         Tooltip("Seconds between end of one loop and start of the next.")]
        protected float loopInterval = 0f;

        // ── Abstract contract ─────────────────────────────────────────────────

        /// <summary>
        /// Build and return a DOTween Sequence for the given target.
        /// Always pass the result through FinaliseSequence() before returning.
        /// </summary>
        public abstract Sequence BuildSequence(int index, int totalCount, Transform target);

        /// <summary>
        /// Returns the wall-clock duration of one play of this effect in seconds.
        /// Used by controllers to calculate stagger and loop timing without
        /// allocating a throwaway Sequence.
        ///
        /// For simple effects: return duration.
        /// For multi-leg effects: return sum of all leg durations.
        /// Loop interval is NOT included — controllers handle that separately.
        /// </summary>
        public abstract float GetDuration();

        // ── Leak prevention ───────────────────────────────────────────────────

        protected void OnDisable() => DOTween.Kill(gameObject);

        // ── Shared helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Applies ease, loop, unscaled-time, and SetLink to a completed Sequence.
        /// Call this as the final step before returning from BuildSequence().
        /// </summary>
        protected Sequence FinaliseSequence(Sequence seq)
        {
            ApplyLoop(seq);

            var settings = TweenSettingsSO.Instance;
            if (settings != null && settings.useUnscaledTime)
                seq.SetUpdate(UpdateType.Normal, isIndependentUpdate: true);

            seq.SetLink(gameObject, LinkBehaviour.KillOnDisable);
            return seq;
        }

        /// <summary>
        /// Applies the configured ease to a Tweener and returns it for fluent chaining.
        /// seq.Append(ApplyEase(target.DOScale(...)));
        /// </summary>
        protected Tweener ApplyEase(Tweener t)
        {
            return easeMode == EaseMode.Curve
                ? t.SetEase(easeCurve)
                : t.SetEase(ease);
        }

        private void ApplyLoop(Sequence seq)
        {
            if (!loop) return;
            if (loopInterval > 0f) seq.AppendInterval(loopInterval);
            seq.SetLoops(loopCount, LoopType.Restart);
        }

        // ── Editor utility ────────────────────────────────────────────────────

        [ContextMenu("Test Effect Locally")]
        public void TestPlay()
        {
            if (!Application.isPlaying) return;
            BuildSequence(0, 1, transform).Play();
        }
    }

    public enum EaseMode { Preset, Curve }
}
