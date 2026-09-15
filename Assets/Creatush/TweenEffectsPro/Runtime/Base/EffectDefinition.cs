using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    public enum EaseMode { Preset, Curve }

    /// <summary>
    /// Abstract base for every effect in TweenEffects Pro.
    ///
    /// Effects are plain data, not MonoBehaviours: each one is a [Serializable]
    /// class held inline on an MasterSequenceController's step (or wrapped in a shared
    /// EffectPresetSO asset for reuse across prefabs/controllers). An effect
    /// carries no scene identity of its own — BuildSequence() is always given
    /// an explicit EffectContext (target transform, owning GameObject, and
    /// stagger index/count) instead of reading its own transform/gameObject.
    ///
    /// Duration
    /// ─────────
    /// Subclasses must implement GetDuration(), computed from serialized
    /// fields only (no scene lookups) — MasterSequenceController uses it to plan
    /// stagger/loop timing without building a throwaway Sequence.
    ///
    /// Leak prevention
    /// ───────────────
    /// FinaliseSequence() links every sequence to the context's owner
    /// GameObject via SetLink(KillOnDisable), matching the previous
    /// MonoBehaviour-based behaviour without needing OnDisable on the effect.
    ///
    /// Looping
    /// ───────
    /// Effects do not loop themselves — looping is a whole-sequence concern,
    /// configured once on the owning MasterSequenceController (Auto Play > Loop) so a
    /// controller with several steps loops as one coherent unit instead of
    /// each step repeating on its own, independently-timed cycle.
    /// </summary>
    [System.Serializable]
    public abstract class EffectDefinition
    {
        // ── Timing ────────────────────────────────────────────────────────────

        [SerializeField, Min(0.01f),
         Tooltip("How long one play of this effect takes, in seconds.")]
        protected float duration = 0.5f;

        // ── Ease ──────────────────────────────────────────────────────────────

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

        // ── Abstract contract ─────────────────────────────────────────────────

        /// <summary>
        /// Build and return a DOTween Sequence for the given context.
        /// Always pass the result through FinaliseSequence() before returning.
        /// </summary>
        public abstract Sequence BuildSequence(EffectContext ctx);

        /// <summary>
        /// Returns the wall-clock duration of one play of this effect in seconds.
        /// For simple effects: return duration. For multi-leg effects: return the
        /// sum of all leg durations. Loop interval is NOT included.
        /// </summary>
        public abstract float GetDuration();

        // ── Shared helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Applies unscaled-time (if configured) and SetLink to a completed
        /// Sequence. Call this as the final step before returning from
        /// BuildSequence().
        /// </summary>
        protected Sequence FinaliseSequence(Sequence seq, GameObject owner)
        {
            var settings = TweenSettingsSO.Instance;
            if (settings != null && settings.useUnscaledTime)
                seq.SetUpdate(UpdateType.Normal, isIndependentUpdate: true);

            if (owner != null)
                seq.SetLink(owner, LinkBehaviour.KillOnDisable);

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
    }
}
