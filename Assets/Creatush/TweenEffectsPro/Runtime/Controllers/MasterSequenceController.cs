using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    // ── Shared data types ─────────────────────────────────────────────────────

    [System.Serializable]
    public class TriggerSettings
    {
        [Tooltip("Optional identifier external systems can use to Play() this controller by name — " +
                 "e.g. an Animation Event, a dialogue system, or a shared trigger bus. " +
                 "Call PlayByTrigger(name) with a matching name to fire it. Leave blank if unused.")]
        public string playTrigger;

        [Tooltip("Optional identifier external systems can use to Stop() this controller by name. " +
                 "Particularly useful for ending a looping sequence (Auto Play > Loop), since a " +
                 "looping controller won't stop on its own. Call KillByTrigger(name) to fire it.")]
        public string killTrigger;
    }

    [System.Serializable]
    public class ControllerAutoPlay
    {
        [Tooltip("Play once when Start() is called — first activation only.")]
        public bool onStart = false;

        [Tooltip("Play every time this GameObject is enabled.")]
        public bool onEnable = false;

        [Tooltip("Loop this controller's sequence — baked into the sequence itself, so it " +
                 "applies no matter how playback was triggered (Auto Play, a script call, or a Trigger).")]
        public bool loop = false;

        [Tooltip("-1 = infinite.")]
        public int loopCount = -1;

        [Min(0f),
         Tooltip("Seconds between the end of one run and the start of the next.")]
        public float loopInterval = 1f;

        [Tooltip("Restart: replay from the beginning each time." + "Yoyo: alternate forward/backward each loop — ping-pongs the whole sequence." +
                 "Incremental: each loop continues from where the previous one ended, stacking (e.g. a repeated move keeps moving further each time) rather than resetting.")]
        public LoopType loopType = LoopType.Restart;
    }

    // ── Controller ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The single controller for TweenEffects Pro. Holds an ordered list of
    /// EffectSteps and composes them into one DOTween Sequence — sequential,
    /// joined ("parallel"), and staggered-across-targets are all just
    /// properties of a step rather than separate controller types.
    ///
    /// Replaces SequenceEffectsController / ParallelEffectsController /
    /// StaggerEffectsController / SingleEffectController from the pre-2.0
    /// component split (migrated away from via MigrationTool; both the old
    /// components and the migration tool have since been removed from the
    /// package now that migration is complete).
    /// </summary>
    [AddComponentMenu("Creatush/TweenEffects Pro/Master Sequence Controller")]
    public class MasterSequenceController : MonoBehaviour
    {
        [SerializeField] public ControllerAutoPlay autoPlay = new ControllerAutoPlay();
        [SerializeField] public TriggerSettings triggers = new TriggerSettings();

        [SerializeField, Tooltip("Fired once this controller's sequence finishes playing — after " +
                 "every loop iteration if looping. Never fires when Loop Count is -1 (infinite).")]
        public UnityEvent onSequenceComplete;

        [SerializeField] private List<EffectStep> steps = new List<EffectStep>();

        public IReadOnlyList<EffectStep> Steps => steps;

        /// <summary>Appends a step at runtime or from editor tooling (e.g. the migration tool).</summary>
        public void AddStep(EffectStep step) => steps.Add(step);

        // ── Unity messages ────────────────────────────────────────────────────

        protected virtual void Start()
        {
            if (autoPlay.onStart && !autoPlay.onEnable) Play();
        }

        protected virtual void OnEnable()
        {
            if (autoPlay.onEnable) Play();
        }

        protected virtual void OnDestroy()
        {
            Stop();
            if (steps == null) return;
            foreach (var step in steps)
            {
                if (step?.ResolvedEffect is IEffectLifecycle lifecycle)
                    lifecycle.OnOwnerDestroyed(gameObject);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Play() => PlayInternal(reverse: false, speed: 1f);
        public void PlayReverse(float speed = 1f) => PlayInternal(reverse: true, speed);

        /// <summary>Plays this controller if triggerName matches Triggers.playTrigger. Wire this to an Animation Event, a dialogue system, or any string-based trigger bus.</summary>
        public void PlayByTrigger(string triggerName)
        {
            if (!string.IsNullOrEmpty(triggers.playTrigger) && triggerName == triggers.playTrigger)
                Play();
        }

        /// <summary>Stops this controller (including an active loop) if triggerName matches Triggers.killTrigger.</summary>
        public void KillByTrigger(string triggerName)
        {
            if (!string.IsNullOrEmpty(triggers.killTrigger) && triggerName == triggers.killTrigger)
                Stop();
        }

        public virtual void Stop()
        {
            DOTween.Kill(this);

            if (steps == null) return;
            foreach (var step in steps)
            {
                if (step == null) continue;
                foreach (var t in step.ResolveTargets(transform))
                    if (t != null) t.DOKill();
            }
        }

        /// <summary>
        /// Wall-clock duration of one full Play(), mirroring exactly how
        /// PlayInternal composes steps: each step inserts at
        /// Max(0, cursor + delay), where cursor tracks the furthest point
        /// any step has reached so far — the same "running length" DOTween
        /// itself tracks on the live Sequence via Duration(false).
        /// </summary>
        public virtual float GetDuration()
        {
            if (steps == null) return 0f;

            float cursor = 0f;

            foreach (var step in steps)
            {
                var def = step?.ResolvedEffect;
                if (def == null) continue;

                float insertAt = Mathf.Max(0f, cursor + step.delay);
                cursor = Mathf.Max(cursor, insertAt + step.GetStepDuration(transform));
            }

            return cursor;
        }

        // ── Build & play ─────────────────────────────────────────────────────

        private void PlayInternal(bool reverse, float speed)
        {
            if (steps == null || steps.Count == 0) return;

            Sequence master = DOTween.Sequence();
            master.SetId(this);

            var ordered = steps;
            if (reverse)
            {
                ordered = new List<EffectStep>(steps);
                ordered.Reverse();
            }

            foreach (var step in ordered)
            {
                var def = step?.ResolvedEffect;
                if (def == null) continue;

                Sequence stepSeq = BuildStepSequence(step, def);
                if (stepSeq == null) continue;

                if (reverse)
                {
                    stepSeq.Goto(stepSeq.Duration(false), andPlay: false);
                    stepSeq.timeScale = -speed;
                }

                // master.Duration(false) is DOTween's own live "furthest point
                // reached so far" — using it directly (rather than a hand-rolled
                // cursor) means a step's placement always accounts for the true
                // end of everything inserted before it, however it overlapped.
                float insertAt = Mathf.Max(0f, master.Duration(false) + step.delay);
                master.Insert(insertAt, stepSeq);
            }

            // Loop is baked directly into the sequence itself (mirroring how
            // each effect used to bake its own loop) so it applies no matter
            // how playback was triggered — Auto Play, a script call, or a
            // Trigger — rather than only when driven by a separate coroutine.
            if (autoPlay.loop)
            {
                if (autoPlay.loopInterval > 0f) master.AppendInterval(autoPlay.loopInterval);
                master.SetLoops(autoPlay.loopCount, autoPlay.loopType);
            }

            if (onSequenceComplete != null)
                master.OnComplete(() => onSequenceComplete.Invoke());

            // Explicit link on the master itself — the child step sequences
            // are already individually KillOnDisable-linked, but a looping
            // master needs its own guarantee that disabling this object
            // actually stops the repeat, not just whatever step is mid-play.
            master.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            master.timeScale = reverse ? speed : 1f;
            master.Play();
        }

        /// <summary>
        /// Builds one step's own Sequence: a single BuildSequence() call for a
        /// step resolving to one target, or a composed sub-sequence inserting
        /// each resolved target's play at i*staggerInterval otherwise.
        /// </summary>
        private Sequence BuildStepSequence(EffectStep step, EffectDefinition def)
        {
            var targets = step.ResolveTargets(transform);
            if (targets.Count == 0) return null;

            Sequence result;

            if (targets.Count == 1)
            {
                var ctx = new EffectContext(targets[0], gameObject, 0, 1);
                result = def.BuildSequence(ctx);
            }
            else
            {
                Sequence burst = DOTween.Sequence();
                bool any = false;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] == null) continue;
                    var ctx = new EffectContext(targets[i], gameObject, i, targets.Count);
                    Sequence itemSeq = def.BuildSequence(ctx);
                    if (itemSeq == null) continue;
                    burst.Insert(i * step.staggerInterval, itemSeq);
                    any = true;
                }
                result = any ? burst : null;
            }

            return result;
        }
    }
}
