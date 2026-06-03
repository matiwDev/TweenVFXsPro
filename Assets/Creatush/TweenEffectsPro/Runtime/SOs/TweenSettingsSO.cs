using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Global settings asset for TweenEffects Pro.
    /// Create one via: Assets > Create > Creatush > TweenEffects Pro > Global Settings
    /// Place it in a Resources folder so it loads automatically at runtime.
    ///
    /// Settings are applied once on load via TweenSettingsLoader (a RuntimeInitializeOnLoad
    /// bootstrapper — no scene object required).
    /// </summary>
    [CreateAssetMenu(
        fileName = "TweenSettings",
        menuName = "Creatush/TweenEffects Pro/Global Settings")]
    public class TweenSettingsSO : ScriptableObject
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        private static TweenSettingsSO _instance;
        public static TweenSettingsSO Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<TweenSettingsSO>("TweenSettings");
                return _instance;
            }
        }

        // ── Global Timing ─────────────────────────────────────────────────────

        [Header("Global Timing")]
        [Range(0f, 2f),
         Tooltip("Multiplies the playback speed of every tween in the project.\n" +
                 "1 = normal speed. Applied to DOTween.timeScale at startup.\n" +
                 "Useful for slow-motion menus or fast-forward debug modes.")]
        public float globalTimeScale = 1f;

        [Tooltip("If enabled, effects respect Time.timeScale (pauses with the game).\n" +
                 "If disabled, effects run in unscaled time — useful for pause-menu animations.")]
        public bool useUnscaledTime = false;

        // ── DOTween Capacity ──────────────────────────────────────────────────

        [Header("DOTween Capacity")]
        [Tooltip("Initial pool size for Tweeners. Increase if you see DOTween expanding its pool at runtime.\n" +
                 "Each slot costs a small fixed memory allocation — set this to your expected peak.")]
        public int tweenersCapacity = 200;

        [Tooltip("Initial pool size for Sequences.")]
        public int sequencesCapacity = 50;

        // ── Debug ─────────────────────────────────────────────────────────────

        [Header("Debug")]
        [Tooltip("Log warnings when effects are played on inactive GameObjects or with missing references.")]
        public bool logWarnings = true;

        [Tooltip("Enable DOTween's safe mode — catches and logs tween errors instead of throwing.\n" +
                 "Recommended on during development, optional in release builds.")]
        public bool safeMode = true;

        // ── Applied at runtime ────────────────────────────────────────────────

        /// <summary>
        /// Pushes all settings into DOTween.
        /// Called automatically by TweenSettingsLoader at startup.
        /// Safe to call manually after changing settings at runtime.
        /// </summary>
        public void Apply()
        {
            DOTween.timeScale = globalTimeScale;

            // Re-initialise DOTween capacity. If DOTween is already initialised
            // this call updates the pool sizes gracefully.
            DOTween.SetTweensCapacity(tweenersCapacity, sequencesCapacity);

            if (safeMode)
                DOTween.useSafeMode = true;

#if UNITY_EDITOR
            if (logWarnings)
                Debug.Log("[TweenEffects Pro] Global settings applied. " +
                          $"TimeScale={globalTimeScale}, " +
                          $"Capacity={tweenersCapacity}/{sequencesCapacity}, " +
                          $"SafeMode={safeMode}, UnscaledTime={useUnscaledTime}");
#endif
        }
    }
}
