using UnityEngine;

namespace Creatush.TweenEffects // Keeping it the same!
{
    [CreateAssetMenu(fileName = "TweenSettings", menuName = "Creatush/Tween Effects/Global Settings")]
    public class TweenSettingsSO : ScriptableObject
    {
        [Header("Global Timing")]
        [Range(0f, 2f)] public float globalTimeScale = 1f;
        
        [Header("Safety")]
        public bool autoSanitizeSequences = true;

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
    }
}