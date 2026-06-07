using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// A named library of pre-configured VFXBehaviour assets.
    /// Avoids duplicating configured components across prefabs —
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
            public VFXBehaviour effect;
            [TextArea(1, 3)]
            public string description;
        }

        [SerializeField] private List<CatalogueEntry> entries = new List<CatalogueEntry>();

        // ── Runtime lookup ────────────────────────────────────────────────────

        // Built lazily on first Get() call, then cached.
        private Dictionary<string, VFXBehaviour> _lookup;

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, VFXBehaviour>(entries.Count);
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
        /// Returns the VFXBehaviour registered under the given key, or null.
        /// </summary>
        public VFXBehaviour Get(string key)
        {
            if (_lookup == null) BuildLookup();
            _lookup.TryGetValue(key, out var effect);
            return effect;
        }

        /// <summary>
        /// Plays the effect registered under key on the given target.
        /// Returns the Sequence so callers can chain callbacks, or null if not found.
        /// </summary>
        public DG.Tweening.Sequence Play(string key, Transform target)
        {
            var effect = Get(key);
            if (effect == null)
            {
                Debug.LogWarning(
                    $"[EffectCatalogue] Key '{key}' not found in {name}.", this);
                return null;
            }
            return effect.BuildSequence(0, 1, target).Play();
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
