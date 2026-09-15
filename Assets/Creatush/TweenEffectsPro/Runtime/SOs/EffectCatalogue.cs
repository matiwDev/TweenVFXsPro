using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// A named library of pre-configured EffectPresetSO assets.
    /// Avoids duplicating configured effects across prefabs —
    /// define "CoinPop", "DamageFlash", "MenuSlideIn" once and reference by name.
    ///
    /// Create via: Assets > Create > Creatush > TweenEffects Pro > Effect Catalogue
    /// </summary>
    [CreateAssetMenu(
        fileName = "EffectCatalogue",
        menuName = "Creatush/TweenEffects Pro/Effect Catalogue")]
    public class EffectCatalogue : ScriptableObject
    {
        [System.Serializable]
        public class CatalogueEntry
        {
            [Tooltip("Unique name used to retrieve this effect at runtime.")]
            public string key;
            public EffectPresetSO effect;
            [TextArea(1, 3)]
            public string description;
        }

        [SerializeField] private List<CatalogueEntry> entries = new List<CatalogueEntry>();

        // ── Runtime lookup ────────────────────────────────────────────────────

        // Built lazily on first Get() call, then cached.
        private Dictionary<string, EffectPresetSO> _lookup;

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, EffectPresetSO>(entries.Count);
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.key) || e.effect == null) continue;
                if (!_lookup.ContainsKey(e.key))
                    _lookup[e.key] = e.effect;
                else
                    Debug.LogWarning(
                        $"[EffectCatalogue] Duplicate key '{e.key}' in {name}. " +
                        "First entry wins.", this);
            }
        }

        /// <summary>
        /// Returns the EffectPresetSO registered under the given key, or null.
        /// </summary>
        public EffectPresetSO Get(string key)
        {
            if (_lookup == null) BuildLookup();
            _lookup.TryGetValue(key, out var preset);
            return preset;
        }

        /// <summary>
        /// Plays the effect registered under key on the given target.
        /// Returns the Sequence so callers can chain callbacks, or null if not found.
        /// owner defaults to target's own GameObject when left null — pass the
        /// GameObject that should own the sequence's lifecycle if that differs
        /// (e.g. a controller orchestrating effects on other objects).
        /// </summary>
        public Sequence Play(string key, Transform target, GameObject owner = null)
        {
            var preset = Get(key);
            if (preset == null || preset.effect == null)
            {
                Debug.LogWarning(
                    $"[EffectCatalogue] Key '{key}' not found in {name}.", this);
                return null;
            }

            var ctx = new EffectContext(target, owner != null ? owner : (target != null ? target.gameObject : null));
            var seq = preset.effect.BuildSequence(ctx);
            return seq?.Play();
        }

        /// <summary>Returns all registered keys — useful for editor dropdowns.</summary>
        public IReadOnlyList<CatalogueEntry> Entries => entries;

        /// <summary>
        /// Invalidates the lookup cache. Call after modifying entries at runtime.
        /// </summary>
        public void Invalidate() => _lookup = null;

#if UNITY_EDITOR
        // Re-cache whenever the SO is saved in the editor
        private void OnValidate() => _lookup = null;
#endif
    }
}
