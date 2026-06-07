using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [AddComponentMenu("Creatush/TweenEffects Pro/Sequence FXs Controller")]
    public class SequenceEffectsController : EffectControllerBase
    {
        [System.Serializable]
        public class SequenceStep
        {
            public VFXBehaviour behavior;
            public float delay = 0f;
            public bool joinPrevious = false;
        }

        [SerializeField] private List<SequenceStep> sequenceSteps = new List<SequenceStep>();
        [SerializeField] private Transform targetOverride;

        // ── EffectControllerBase ──────────────────────────────────────────────

        protected override void PlayInternal(bool reverse, float speed)
        {
            if (sequenceSteps == null || sequenceSteps.Count == 0) return;

            Sequence master = DOTween.Sequence();
            master.SetId(this);

            var steps = sequenceSteps.ToArray();
            if (reverse) System.Array.Reverse(steps);

            foreach (var step in steps)
            {
                if (step.behavior == null) continue;

                Transform t = targetOverride != null ? targetOverride : step.behavior.transform;
                Sequence stepSeq = step.behavior.BuildSequence(0, 1, t);

                if (reverse)
                {
                    stepSeq.Goto(stepSeq.Duration(false), andPlay: false);
                    stepSeq.timeScale = -speed;
                }

                float dur = stepSeq.Duration(true);
                if (dur >= 999998f || dur < 0)
                {
                    stepSeq.SetDelay(master.Duration(false) + step.delay).Play();
                }
                else if (step.joinPrevious)
                {
                    master.Insert(master.Duration(false) + step.delay, stepSeq);
                }
                else
                {
                    if (step.delay > 0f) master.AppendInterval(step.delay);
                    master.Append(stepSeq);
                }
            }

            master.timeScale = reverse ? speed : 1f;
            master.Play();
        }

        public override float GetDuration()
        {
            float total = 0f;
            foreach (var step in sequenceSteps)
            {
                if (step.behavior == null || step.joinPrevious) continue;
                total += step.delay + step.behavior.GetDuration();
            }
            return total;
        }

        public override void Stop()
        {
            base.Stop();
            foreach (var step in sequenceSteps)
                if (step.behavior != null) step.behavior.transform.DOKill();
            if (targetOverride != null) targetOverride.DOKill();
        }

        // Read-only for editor
        public IReadOnlyList<SequenceStep> Steps => sequenceSteps;
        public Transform TargetOverride => targetOverride;
    }
}
