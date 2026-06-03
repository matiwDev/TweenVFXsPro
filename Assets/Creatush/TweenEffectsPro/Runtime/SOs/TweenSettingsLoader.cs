using UnityEngine;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Bootstrapper that loads and applies TweenSettingsSO before any scene loads.
    /// No GameObject or scene setup required — RuntimeInitializeOnLoad handles it.
    ///
    /// Place your TweenSettings asset in a Resources folder for auto-discovery.
    /// If no asset is found, DOTween runs with its own defaults.
    /// </summary>
    internal static class TweenSettingsLoader
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            TweenSettingsSO settings = TweenSettingsSO.Instance;

            if (settings == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    "[TweenEffects Pro] No TweenSettings asset found in Resources. " +
                    "Create one via Assets > Create > Creatush > TweenEffects Pro > Global Settings " +
                    "and place it in a Resources folder.");
#endif
                return;
            }

            settings.Apply();
        }
    }
}
