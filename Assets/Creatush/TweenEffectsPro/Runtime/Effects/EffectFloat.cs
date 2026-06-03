using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    // Float is inherently looping — loop is enabled by default.
    // EaseMode defaults to Preset/InOutSine for the natural sine-wave feel,
    // but the designer can swap to a curve for custom rhythms.

    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Float")]
    public class EffectFloat : VFXBehaviour
    {
        [Header("Float Settings")]
        [SerializeField, Tooltip("How far the target moves up and down from its origin, in local units.")]
        private float amplitude = 20f;

        [SerializeField, Tooltip("Rotate slightly while floating for a more organic feel. Set to 0 to disable.")]
        private float tiltDegrees = 3f;

        public EffectFloat()
        {
            // Sensible defaults for an idle loop
            loop      = true;
            loopCount = -1;
            ease      = Ease.InOutSine;
        }


        public override float GetDuration() { return duration * 2f; }

                public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            Vector3 origin   = target.localPosition;
            Vector3 peak     = origin + new Vector3(0f, amplitude, 0f);
            Vector3 tiltUp   = new Vector3( tiltDegrees, 0f, 0f);
            Vector3 tiltDown = new Vector3(-tiltDegrees, 0f, 0f);

            Sequence seq = DOTween.Sequence();

            // Rise
            seq.Append(ApplyEase(target.DOLocalMove(peak, duration)));
            if (tiltDegrees != 0f)
                seq.Join(ApplyEase(target.DOLocalRotate(tiltUp, duration)));

            // Fall
            seq.Append(ApplyEase(target.DOLocalMove(origin, duration)));
            if (tiltDegrees != 0f)
                seq.Join(ApplyEase(target.DOLocalRotate(tiltDown, duration)));

            return FinaliseSequence(seq);
        }
    }
}
