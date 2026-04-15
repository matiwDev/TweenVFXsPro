using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffects
{
    /// <summary>
    /// The abstract base for all effects in the Creatush ecosystem.
    /// </summary>
    public abstract class VFXBehaviour : MonoBehaviour
    {
        [Header("Global Timing")]
        [SerializeField]
        [Tooltip("The animation curve applied to this behavior.")]
        protected Ease easeType = Ease.OutQuad;

        [SerializeField, Min(0.01f)]
        [Tooltip("How long this specific step takes to complete.")]
        protected float duration = 0.5f;

        /// <summary>
        /// Builds and returns a DOTween Sequence for the given target.
        /// </summary>
        /// <param name="index">Current item index (0 for single objects).</param>
        /// <param name="totalCount">Total items in the operation (1 for single objects).</param>
        /// <param name="target">The Transform being animated.</param>
        public abstract Sequence BuildSequence(int index, int totalCount, Transform target);

        [ContextMenu("Test Effect Locally")]
        public void TestPlay()
        {
            if (!Application.isPlaying) return;
            BuildSequence(0, 1, transform).Play();
        }
    }
}