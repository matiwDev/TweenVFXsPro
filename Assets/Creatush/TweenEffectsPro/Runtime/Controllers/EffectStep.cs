using UnityEngine;
using System.Collections.Generic;

namespace Creatush.TweenEffectsPro
{
    public enum EffectTargetMode { Single, Multiple, Children }

    /// <summary>
    /// One entry in an MasterSequenceController's timeline. Replaces the old split
    /// between SequenceEffectsController/ParallelEffectsController/
    /// StaggerEffectsController/SingleEffectController — sequential vs. joined
    /// timing and single vs. staggered-across-targets are just properties of a
    /// step now, not separate component types. There's no separate
    /// "join previous" flag: a negative Delay overlaps with whatever came
    /// before, which is all "joining" ever needed to mean.
    /// </summary>
    [System.Serializable]
    public class EffectStep
    {
        [Tooltip("Seconds from the end of the timeline so far (i.e. after every earlier step has finished).\n" +
                 "0 = starts right after.\nPositive = adds a gap.\nNegative = overlaps/joins earlier steps — " +
                 "e.g. -1 starts 1s before they would otherwise finish.")]
        public float delay = 0f;

        [Tooltip("Optional shared preset. When assigned, its effect is used instead of the inline one below — lets the same configured effect be reused across controllers/prefabs.")]
        public EffectPresetSO preset;

        [SerializeReference]
        public EffectDefinition effect;

        [Tooltip("Single: one target (Target Override, or this GameObject if empty).\nMultiple: an explicit target list, staggered.\nChildren: every direct child of Target Override (or this GameObject), staggered.")]
        public EffectTargetMode targetMode = EffectTargetMode.Single;

        [Tooltip("Single mode: the target. Children mode: the parent whose children are used. Ignored by Multiple mode." +
                 "Effects that need a specific component (e.g. ColorTint needs a Graphic, FillAmount needs an Image) " +
                 "look for it on this GameObject or its children, so this doesn't need to be the exact GameObject the " +
                 "component lives on — just something above it in the hierarchy.")]
        public Transform targetOverride;

        [Tooltip("Explicit target list — Multiple mode only.")]
        public List<Transform> targets = new List<Transform>();

        [Min(0f), Tooltip("Seconds between each target's start — Multiple/Children modes only.")]
        public float staggerInterval = 0.08f;

        public bool reverseOrder = false;
        public bool randomOrder = false;

        /// <summary>The effect actually used when this step plays: the preset's effect if assigned, otherwise the inline one.</summary>
        public EffectDefinition ResolvedEffect => preset != null ? preset.effect : effect;

        /// <summary>Resolves this step's target list against the controller's own transform (used as the Single/Children fallback).</summary>
        public List<Transform> ResolveTargets(Transform ownerTransform)
        {
            var list = new List<Transform>();

            switch (targetMode)
            {
                case EffectTargetMode.Single:
                    list.Add(targetOverride != null ? targetOverride : ownerTransform);
                    break;

                case EffectTargetMode.Multiple:
                    if (targets != null)
                        foreach (var t in targets)
                            if (t != null) list.Add(t);
                    break;

                case EffectTargetMode.Children:
                    Transform parent = targetOverride != null ? targetOverride : ownerTransform;
                    if (parent != null)
                        for (int i = 0; i < parent.childCount; i++)
                            list.Add(parent.GetChild(i));
                    break;
            }

            if (randomOrder)
            {
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
            }
            else if (reverseOrder)
            {
                list.Reverse();
            }

            return list;
        }

        /// <summary>Wall-clock length of this step alone: one play's duration, plus any stagger spread across its resolved targets.</summary>
        public float GetStepDuration(Transform ownerTransform)
        {
            var def = ResolvedEffect;
            if (def == null) return 0f;
            int count = Mathf.Max(1, ResolveTargets(ownerTransform).Count);
            return def.GetDuration() + staggerInterval * Mathf.Max(0, count - 1);
        }
    }
}
