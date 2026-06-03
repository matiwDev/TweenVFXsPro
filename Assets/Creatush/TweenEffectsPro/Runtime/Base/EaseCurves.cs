using UnityEngine;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// A library of hand-tuned AnimationCurves that complement the effect system.
    /// Access via EaseCurves.BackOut, EaseCurves.Bounce, etc.
    ///
    /// All curves use normalised axes: X = time (0–1), Y = value (0–1).
    /// Values above 1 produce overshoot; values below 0 produce anticipation.
    /// </summary>
    public static class EaseCurves
    {
        // ── Standard ─────────────────────────────────────────────────────────

        /// <summary>Smooth S-curve. Natural, neutral feel.</summary>
        public static AnimationCurve SmoothStep => new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f));

        /// <summary>Fast start, gradual ease to end. Great for exits.</summary>
        public static AnimationCurve EaseOut => new AnimationCurve(
            new Keyframe(0f, 0f, 2.5f, 2.5f),
            new Keyframe(1f, 1f, 0f,   0f));

        /// <summary>Slow start, fast arrival. Great for entries.</summary>
        public static AnimationCurve EaseIn => new AnimationCurve(
            new Keyframe(0f, 0f, 0f,   0f),
            new Keyframe(1f, 1f, 2.5f, 2.5f));

        /// <summary>Linear — no easing at all.</summary>
        public static AnimationCurve Linear => new AnimationCurve(
            new Keyframe(0f, 0f, 1f, 1f),
            new Keyframe(1f, 1f, 1f, 1f));

        // ── Overshoot ────────────────────────────────────────────────────────

        /// <summary>Overshoots the target then settles. Classic pop feel.</summary>
        public static AnimationCurve BackOut => new AnimationCurve(
            new Keyframe(0f,    0f,    0f,    0f),
            new Keyframe(0.7f,  1.1f,  1.2f,  1.2f),
            new Keyframe(1f,    1f,    0f,    0f));

        /// <summary>Pulls back before launching — telegraphed movement.</summary>
        public static AnimationCurve BackIn => new AnimationCurve(
            new Keyframe(0f,    0f,    0f,    0f),
            new Keyframe(0.25f, -0.1f, 0f,    0f),
            new Keyframe(1f,    1f,    2.5f,  2.5f));

        /// <summary>Pull back then overshoot — full anticipation + follow-through.</summary>
        public static AnimationCurve BackInOut => new AnimationCurve(
            new Keyframe(0f,    0f,    0f,    0f),
            new Keyframe(0.2f,  -0.08f,0f,    0f),
            new Keyframe(0.75f, 1.08f, 1f,    1f),
            new Keyframe(1f,    1f,    0f,    0f));

        // ── Bounce ───────────────────────────────────────────────────────────

        /// <summary>Bounces at the end — ball landing on a surface.</summary>
        public static AnimationCurve BounceOut => new AnimationCurve(
            new Keyframe(0f,    0f,    0f,    4f),
            new Keyframe(0.6f,  1f,    0f,    0f),
            new Keyframe(0.75f, 0.88f, 0f,    0f),
            new Keyframe(0.88f, 1f,    0f,    0f),
            new Keyframe(0.95f, 0.96f, 0f,    0f),
            new Keyframe(1f,    1f,    0f,    0f));

        /// <summary>Bounces at the start — reverse ball landing.</summary>
        public static AnimationCurve BounceIn => new AnimationCurve(
            new Keyframe(0f,    0f,    0f,    0f),
            new Keyframe(0.05f, 0.04f, 0f,    0f),
            new Keyframe(0.12f, 0f,    0f,    0f),
            new Keyframe(0.25f, 0.12f, 0f,    0f),
            new Keyframe(0.4f,  0f,    0f,    0f),
            new Keyframe(1f,    1f,    4f,    0f));

        // ── Elastic ──────────────────────────────────────────────────────────

        /// <summary>Elastic snap at arrival — spring settling into place.</summary>
        public static AnimationCurve ElasticOut => new AnimationCurve(
            new Keyframe(0f,    0f,    0f,    0f),
            new Keyframe(0.5f,  1.15f, 2f,    2f),
            new Keyframe(0.75f, 0.92f, 0f,    0f),
            new Keyframe(0.88f, 1.03f, 0f,    0f),
            new Keyframe(1f,    1f,    0f,    0f));

        /// <summary>Elastic windup before movement — coiled spring releasing.</summary>
        public static AnimationCurve ElasticIn => new AnimationCurve(
            new Keyframe(0f,    0f,    0f,    0f),
            new Keyframe(0.12f, 0.08f, 0f,    0f),
            new Keyframe(0.25f, -0.03f,0f,    0f),
            new Keyframe(0.5f,  -0.15f,2f,    2f),
            new Keyframe(1f,    1f,    0f,    0f));

        // ── Specialty ────────────────────────────────────────────────────────

        /// <summary>Sharp acceleration to a hard stop — snappy UI feel.</summary>
        public static AnimationCurve Snap => new AnimationCurve(
            new Keyframe(0f,    0f,    0f,    5f),
            new Keyframe(0.5f,  0.97f, 0.5f,  0.5f),
            new Keyframe(1f,    1f,    0f,    0f));

        /// <summary>
        /// Spring: overshoots, oscillates, settles.
        /// Great for notification pop-ins and attention effects.
        /// </summary>
        public static AnimationCurve Spring => new AnimationCurve(
            new Keyframe(0f,    0f,    0f,    0f),
            new Keyframe(0.4f,  1.12f, 2f,    2f),
            new Keyframe(0.6f,  0.94f, 0f,    0f),
            new Keyframe(0.75f, 1.04f, 0f,    0f),
            new Keyframe(0.88f, 0.98f, 0f,    0f),
            new Keyframe(1f,    1f,    0f,    0f));

        /// <summary>
        /// Anticipate: slight reverse before the main move, then fast arrival.
        /// Gives actions a sense of weight and intent.
        /// </summary>
        public static AnimationCurve Anticipate => new AnimationCurve(
            new Keyframe(0f,    0f,    0f,    0f),
            new Keyframe(0.15f, -0.05f,0f,    0f),
            new Keyframe(1f,    1f,    3f,    0f));

        /// <summary>All named presets, in display order for the Inspector picker.</summary>
        public static readonly (string name, System.Func<AnimationCurve> factory)[] All =
        {
            ("Smooth Step",  () => SmoothStep),
            ("Ease Out",     () => EaseOut),
            ("Ease In",      () => EaseIn),
            ("Linear",       () => Linear),
            ("Back Out",     () => BackOut),
            ("Back In",      () => BackIn),
            ("Back In Out",  () => BackInOut),
            ("Bounce Out",   () => BounceOut),
            ("Bounce In",    () => BounceIn),
            ("Elastic Out",  () => ElasticOut),
            ("Elastic In",   () => ElasticIn),
            ("Snap",         () => Snap),
            ("Spring",       () => Spring),
            ("Anticipate",   () => Anticipate),
        };
    }
}
