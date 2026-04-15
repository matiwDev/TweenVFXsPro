using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Creatush.TweenEffects
{
    [AddComponentMenu("Creatush/Tween Effects/Sequence FXs Controller")]
    public class SequenceEffectsController : MonoBehaviour
    {
        [System.Serializable]
        public class SequenceStep
        {
            public VFXBehaviour behavior;
            public float delay = 0f;
            public bool joinPrevious = false;
        }

        #region Serialized Fields
        [SerializeField] private List<SequenceStep> sequenceSteps = new List<SequenceStep>();
        [SerializeField] private Transform targetOverride;
        [SerializeField] private bool playOnStart = false;
        #endregion

        private void Start()
        {
            if (playOnStart) Play();
        }

        public void Play()
        {
            if (sequenceSteps == null || sequenceSteps.Count == 0) return;

            Sequence masterSequence = DOTween.Sequence();
            masterSequence.SetId(this);

            foreach (var step in sequenceSteps)
            {
                if (step.behavior == null) continue;

                Transform individualTarget = targetOverride != null ? targetOverride : step.behavior.transform;

                // 1. Build the behavior
                Sequence stepSeq = step.behavior.BuildSequence(0, 1, individualTarget);

                // 2. Handle infinite tweens by playing them independently
                if (stepSeq.Duration(true) >= 999998f || stepSeq.Duration(true) < 0)
                {
                    // Calculate when this should start relative to the master sequence
                    float startTime = masterSequence.Duration(false) + step.delay;

                    // Delay the infinite tween and play it independently
                    stepSeq.SetDelay(startTime).Play();
                }
                else
                {
                    // 3. Standard Timing for non-infinite tweens
                    if (step.joinPrevious)
                    {
                        masterSequence.Insert(masterSequence.Duration(false) + step.delay, stepSeq);
                    }
                    else
                    {
                        if (step.delay > 0) masterSequence.AppendInterval(step.delay);
                        masterSequence.Append(stepSeq);
                    }
                }
            }

            masterSequence.Play();
        }

        public void Stop()
        {
            DOTween.Kill(this);
            foreach (var step in sequenceSteps)
            {
                if (step.behavior != null) step.behavior.transform.DOKill();
            }
            if (targetOverride != null) targetOverride.DOKill();
            transform.DOKill();
        }

        private void OnDestroy() => Stop();
    }
}