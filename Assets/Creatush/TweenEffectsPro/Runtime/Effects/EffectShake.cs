using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    // Shake uses DOTween's stochastic algorithm — the base ease fields
    // don't apply here. Shape the feel via Strength and Vibrato instead.

    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Shake")]
    public class EffectShake : VFXBehaviour
    {
        [Header("Position Shake")]
        [SerializeField, Tooltip("Enable position shake.")]
        private bool shakePosition = false;

        [SerializeField, Tooltip("Per-axis position shake strength in local units.")]
        private Vector3 positionStrength = new Vector3(10f, 10f, 0f);

        [Header("Rotation Shake")]
        [SerializeField, Tooltip("Enable rotation shake.")]
        private bool shakeRotation = true;

        [SerializeField, Tooltip("Per-axis rotation shake amplitude in degrees.\n" +
                                  "Set Z only for a 2D screen-shake feel.\n" +
                                  "Use all three for a full 3D tumble.")]
        private Vector3 rotationStrength = new Vector3(0f, 0f, 8f);

        [Header("Scale Shake")]
        [SerializeField, Tooltip("Enable scale shake.")]
        private bool shakeScale = false;

        [SerializeField, Tooltip("Uniform scale shake strength.")]
        private float scaleStrength = 0.05f;

        [Header("Shake Feel")]
        [SerializeField, Tooltip("Number of oscillations. Higher = more frantic.")]
        private int vibrato = 10;

        [SerializeField, Range(0f, 1f),
         Tooltip("How random the shake direction is.\n" +
                 "0 = strictly follows the strength axes, 1 = fully random.")]
        private float randomness = 0.5f;

        [SerializeField, Tooltip("Fade out the shake smoothly at the end rather than cutting.")]
        private bool fadeOut = true;

        // ── VFXBehaviour ──────────────────────────────────────────────────────

        public override float GetDuration() { return duration; }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            Vector3    originScale = target.localScale;
            Quaternion originRot   = target.localRotation;
            Vector3    originPos   = target.localPosition;

            Sequence seq = DOTween.Sequence();

            if (shakePosition)
                seq.Join(target.DOShakePosition(duration, positionStrength,
                    vibrato, randomness, snapping: false, fadeOut));

            if (shakeRotation)
                seq.Join(target.DOShakeRotation(duration, rotationStrength,
                    vibrato, randomness, fadeOut));

            if (shakeScale)
                seq.Join(target.DOShakeScale(duration, scaleStrength,
                    vibrato, randomness, fadeOut));

            // Restore state cleanly — DOShake doesn't guarantee exact return to origin
            seq.OnComplete(() =>
            {
                target.localPosition = originPos;
                target.localRotation = originRot;
                target.localScale    = originScale;
            });

            return FinaliseSequence(seq);
        }
    }
}
