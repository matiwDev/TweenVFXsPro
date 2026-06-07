using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    // ── Shared data types ─────────────────────────────────────────────────────

    [System.Serializable]
    public class OutAnimSettings
    {
        [Tooltip("Call Hide() instead of SetActive(false) to play this animation before deactivating.")]
        public bool enabled = false;

        [Tooltip("Play the sequence reversed as the exit animation.")]
        public bool reverse = false;

        [Min(0.1f),
         Tooltip("Playback speed multiplier. 1 = same speed, 1.5 = 50% faster.")]
        public float speed = 1f;

        [Tooltip("Fired when the out animation finishes. " +
                 "Wire deactivation of parent objects or scene transitions here.")]
        public UnityEvent OnOutComplete;
    }

    [System.Serializable]
    public class ControllerAutoPlay
    {
        [Tooltip("Play once when Start() is called — first activation only.")]
        public bool onStart = false;

        [Tooltip("Play every time this GameObject is enabled.")]
        public bool onEnable = false;

        [Tooltip("Repeat the sequence on a fixed interval while this object is active.")]
        public bool loop = false;

        [Tooltip("-1 = infinite.")]
        public int loopCount = -1;

        [Min(0f),
         Tooltip("Seconds between end of one run and start of the next.")]
        public float loopInterval = 1f;
    }

    // ── Base controller ───────────────────────────────────────────────────────

    /// <summary>
    /// Shared lifecycle, auto-play, out-animation, and loop logic for all
    /// effect controllers. Subclasses implement PlayInternal() and GetDuration().
    /// </summary>
    public abstract class EffectControllerBase : MonoBehaviour
    {
        [SerializeField] public ControllerAutoPlay autoPlay = new ControllerAutoPlay();
        [SerializeField] public OutAnimSettings outAnim = new OutAnimSettings();

        private Coroutine _loopCoroutine;

        // ── Unity messages ────────────────────────────────────────────────────

        protected virtual void Start()
        {
            if (autoPlay.onStart && !autoPlay.onEnable) Play();
        }

        protected virtual void OnEnable()
        {
            if (autoPlay.onEnable) Play();
            if (autoPlay.loop && _loopCoroutine == null)
                _loopCoroutine = StartCoroutine(LoopRoutine());
        }

        protected virtual void OnDisable() => StopLoop();

        protected virtual void OnDestroy() => Stop();

        // ── Public API ────────────────────────────────────────────────────────

        public void Play() => PlayInternal(reverse: false, speed: 1f);
        public void PlayReverse(float speed = 1f) => PlayInternal(reverse: true, speed);

        public void Hide()
        {
            if (!outAnim.enabled) { gameObject.SetActive(false); return; }
            StartCoroutine(PlayOutThenDeactivate());
        }

        public virtual void Stop()
        {
            StopLoop();
            DOTween.Kill(this);
        }

        // ── Abstract contract ─────────────────────────────────────────────────

        /// <summary>Build and play all effects. Reverse and speed are applied when playing out.</summary>
        protected abstract void PlayInternal(bool reverse, float speed);

        /// <summary>
        /// Returns the wall-clock duration of one full play.
        /// Used by the loop coroutine — must not allocate.
        /// </summary>
        public abstract float GetDuration();

        // ── Shared helpers ────────────────────────────────────────────────────

        protected void StopLoop()
        {
            if (_loopCoroutine == null) return;
            StopCoroutine(_loopCoroutine);
            _loopCoroutine = null;
        }

        private IEnumerator LoopRoutine()
        {
            int count = 0;
            while (autoPlay.loopCount < 0 || count < autoPlay.loopCount)
            {
                Play();
                yield return new WaitForSeconds(GetDuration() + autoPlay.loopInterval);
                count++;
            }
            _loopCoroutine = null;
        }

        private IEnumerator PlayOutThenDeactivate()
        {
            float dur = GetDuration() / Mathf.Max(0.01f, outAnim.speed);
            if (outAnim.reverse) PlayReverse(outAnim.speed);
            else Play();
            yield return new WaitForSeconds(dur);
            outAnim.OnOutComplete?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
