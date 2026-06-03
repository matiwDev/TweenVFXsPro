using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [AddComponentMenu("Creatush/TweenEffects Pro/Single FX Controller")]
    public class SingleEffectController : MonoBehaviour
    {
        [SerializeField] private VFXBehaviour effect;

        [Tooltip("The transform the effect is applied to. Defaults to this GameObject if left empty.")]
        [SerializeField] private Transform target;

        [SerializeField] private bool playOnEnable = false;
        [SerializeField] private bool playOnStart  = false;

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
            Transform t = target != null ? target : transform;
            effect.BuildSequence(0, 1, t).Play();
        }

        public void Stop()
        {
            if (effect == null) return;
            Transform t = target != null ? target : transform;
            t.DOKill();
        }
    }
}
