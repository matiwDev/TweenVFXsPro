using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffects
{
    [AddComponentMenu("Creatush/Tween Effects/Single FX Controller")]
    public class SingleEffectController : MonoBehaviour
    {
        [SerializeField] private VFXBehaviour effect;
        [SerializeField] private bool playOnEnable = false;
        [SerializeField] private bool playOnStart = false;

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        private void Start()
        {
            if (playOnStart) Play();
        }

        [ContextMenu("Play Effect")]
        public void Play()
        {
            if (effect == null) return;
            // Standard single-target call (Index 0, Total 1)
            effect.BuildSequence(0, 1, transform).Play();
        }
    }
}