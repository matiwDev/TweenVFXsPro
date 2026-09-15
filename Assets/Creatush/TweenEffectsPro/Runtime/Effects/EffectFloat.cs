using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    // One rise-and-fall cycle per play. For a continuously idling float,
    // enable Loop on the owning MasterSequenceController (Auto Play > Loop, count -1) —
    // looping is a whole-sequence concern now, not something an effect opts
    // into on its own.
    // EaseMode defaults to Preset/InOutSine for the natural sine-wave feel,
    // but the designer can swap to a curve for custom rhythms.

    [System.Serializable]
    public class EffectFloat : EffectDefinition
    {
        [Header("Float Settings")]
        [SerializeField, Tooltip("How far the target moves up and down from its origin, in local units.")]
        private float amplitude = 20f;

        [SerializeField, Tooltip("Rotate slightly while floating for a more organic feel. Set to 0 to disable.")]
        private float tiltDegrees = 3f;

        public EffectFloat()
        {
            ease = Ease.InOutSine;
        }

        public override float GetDuration() => duration * 2f;

        public override Sequence BuildSequence(EffectContext ctx)
        {
            if (ctx.target == null) return null;
            Transform target = ctx.target;

            Vector3 origin = target.localPosition;
            Vector3 peak = origin + new Vector3(0f, amplitude, 0f);
            Vector3 tiltUp = new Vector3(tiltDegrees, 0f, 0f);
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

            return FinaliseSequence(seq, ctx.owner);
        }
    }
}
