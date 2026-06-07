using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [AddComponentMenu("Creatush/TweenEffects Pro/Stagger FXs Controller")]
    public class StaggerEffectsController : EffectControllerBase
    {
        [Header("Effect")]
        [SerializeField] private VFXBehaviour behavior;

        [Header("Targets")]
        [SerializeField] private List<Transform> targets = new List<Transform>();
        [SerializeField] private bool useChildren = true;

        [Header("Stagger")]
        [SerializeField, Min(0f)] private float staggerInterval = 0.08f;
        [SerializeField] private bool reverseOrder = false;
        [SerializeField] private bool randomOrder = false;

        [Header("Events")]
        [SerializeField] private UnityEvent onAllComplete;

        private Coroutine _playCoroutine;

        // ── EffectControllerBase ──────────────────────────────────────────────

        protected override void PlayInternal(bool reverse, float speed)
        {
            if (_playCoroutine != null) StopCoroutine(_playCoroutine);
            _playCoroutine = StartCoroutine(PlayRoutine(reverse, speed));
        }

        public override float GetDuration()
        {
            if (behavior == null) return 0f;
            int count = targets.Count > 0
                ? targets.Count
                : (useChildren ? transform.childCount : 0);
            return behavior.GetDuration() + staggerInterval * Mathf.Max(0, count - 1);
        }

        public override void Stop()
        {
            base.Stop();
            if (_playCoroutine != null) { StopCoroutine(_playCoroutine); _playCoroutine = null; }
        }

        // ── Private ───────────────────────────────────────────────────────────

        private IEnumerator PlayRoutine(bool reverse, float speed)
        {
            if (behavior == null) yield break;

            List<Transform> ordered = BuildTargetList();
            if (ordered.Count == 0) yield break;

            int total = ordered.Count;
            for (int i = 0; i < total; i++)
            {
                Transform t = ordered[i];
                if (t == null) continue;

                Sequence seq = behavior.BuildSequence(i, total, t);
                if (reverse)
                {
                    seq.Goto(seq.Duration(false), andPlay: false);
                    seq.timeScale = -speed;
                }
                seq.Play();

                if (staggerInterval > 0f)
                    yield return new WaitForSeconds(staggerInterval);
            }

            // Wait for last effect to finish
            yield return new WaitForSeconds(
                Mathf.Max(0f, behavior.GetDuration() - staggerInterval));

            _playCoroutine = null;
            onAllComplete?.Invoke();
        }

        private List<Transform> BuildTargetList()
        {
            var list = new List<Transform>();

            if (targets != null && targets.Count > 0)
                list.AddRange(targets);
            else if (useChildren)
                for (int i = 0; i < transform.childCount; i++)
                    list.Add(transform.GetChild(i));

            if (randomOrder)
            {
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
            }
            else if (reverseOrder)
            {
                list.Reverse();
            }

            return list;
        }
    }
}
