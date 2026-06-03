using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Runs all assigned effects simultaneously.
    /// Use when you need synced multi-part animations on different objects.
    /// For sequential chaining use SequenceEffectsController instead.
    /// </summary>
    [AddComponentMenu("Creatush/TweenEffects Pro/Parallel FXs Controller")]
    public class ParallelEffectsController : EffectControllerBase
    {
        [System.Serializable]
        public class ParallelStep
        {
            public VFXBehaviour behavior;
            [Tooltip("Override which transform this step animates. Defaults to the behaviour's own transform.")]
            public Transform targetOverride;
            [Min(0f), Tooltip("Seconds before this specific step fires within the burst.")]
            public float delay = 0f;
        }

        [SerializeField] private List<ParallelStep> steps = new List<ParallelStep>();

        // ── EffectControllerBase ──────────────────────────────────────────────

        protected override void PlayInternal(bool reverse, float speed)
        {
            if (steps == null || steps.Count == 0) return;

            Sequence master = DOTween.Sequence();
            master.SetId(this);

            foreach (var step in steps)
            {
                if (step.behavior == null) continue;

                Transform t = step.targetOverride != null
                    ? step.targetOverride : step.behavior.transform;

                Sequence stepSeq = step.behavior.BuildSequence(0, 1, t);

                if (reverse)
                {
                    stepSeq.Goto(stepSeq.Duration(false), andPlay: false);
                    stepSeq.timeScale = -speed;
                }

                master.Insert(step.delay, stepSeq);
            }

            master.timeScale = reverse ? speed : 1f;
            master.Play();
        }

        public override float GetDuration()
        {
            float max = 0f;
            foreach (var step in steps)
            {
                if (step.behavior == null) continue;
                max = Mathf.Max(max, step.delay + step.behavior.GetDuration());
            }
            return max;
        }

        public override void Stop()
        {
            base.Stop();
            foreach (var s in steps)
            {
                if (s.behavior != null) s.behavior.transform.DOKill();
                if (s.targetOverride != null) s.targetOverride.DOKill();
            }
        }
    }
}
