using UnityEngine;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Optional hook for EffectDefinitions that hold state which outlives a
    /// single BuildSequence() call (pooled instances, cached components, etc).
    /// MasterSequenceController calls OnOwnerDestroyed on every step's resolved effect
    /// when its own OnDestroy runs, mirroring what an OnDestroy() on a
    /// MonoBehaviour-based effect used to do for itself.
    /// </summary>
    public interface IEffectLifecycle
    {
        void OnOwnerDestroyed(GameObject owner);
    }
}
