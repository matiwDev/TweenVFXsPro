using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffects
{
    public abstract class VFXBehaviour : MonoBehaviour
    {
        [Header("Global Timing")]
        public Ease easeType = Ease.OutQuad;
        [Min(0.01f)] public float duration = 0.5f;

        protected virtual Sequence InitializeSequence()
        {
            Sequence s = DOTween.Sequence();

            // Pro Feature: Automatically sync with Global Brain
            if (TweenSettingsSO.Instance != null)
            {
                s.timeScale = TweenSettingsSO.Instance.globalTimeScale;
            }

            return s;
        }

        public abstract Sequence BuildSequence(int index, int totalCount, Transform target);

        [ContextMenu("Test Effect Locally")]
        public void TestPlay()
        {
            if (!Application.isPlaying) return;
            BuildSequence(0, 1, transform).Play();
        }
    }
}