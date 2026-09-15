using UnityEngine;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Wraps a single configured EffectDefinition as a shareable asset, so the
    /// same effect can be reused across multiple MasterSequenceController steps and
    /// prefabs instead of re-authoring it inline every time. Assign one to an
    /// EffectStep's Preset field to have that step use it in place of its
    /// inline effect.
    ///
    /// Create via: Assets > Create > Creatush > TweenEffects Pro > Effect Preset
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewEffectPreset",
        menuName = "Creatush/TweenEffects Pro/Effect Preset")]
    public class EffectPresetSO : ScriptableObject
    {
        [SerializeReference] public EffectDefinition effect;
    }
}
