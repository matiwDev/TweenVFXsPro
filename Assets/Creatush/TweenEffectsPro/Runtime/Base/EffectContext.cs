using UnityEngine;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Everything an EffectDefinition needs to build its Sequence for one play.
    /// Kept as a plain struct (not a MonoBehaviour reference) so effects stay
    /// pure data — no scene dependency until BuildSequence() actually runs.
    ///
    /// TMP groundwork: textRenderer/characterIndex are unused by every effect
    /// today, but they exist so a future per-character text effect can plug
    /// into the same MasterSequenceController/EffectStep pipeline (a "target" that is
    /// a character on a TMP_Text rather than a Transform) without another
    /// interface rewrite. Effects that only care about a Transform ignore them.
    /// </summary>
    public readonly struct EffectContext
    {
        /// <summary>The transform this play of the effect acts on.</summary>
        public readonly Transform target;

        /// <summary>
        /// The GameObject of the MasterSequenceController driving this effect. Used for
        /// SetLink/kill scoping and as a fallback parent for effects that spawn
        /// their own objects (e.g. EffectRewardFly's item pool).
        /// </summary>
        public readonly GameObject owner;

        /// <summary>Position of this target within a stagger/multi-target group. 0 for a single target.</summary>
        public readonly int index;

        /// <summary>Size of the stagger/multi-target group this target belongs to. 1 for a single target.</summary>
        public readonly int totalCount;

        /// <summary>Reserved for a future per-character text effect: the text renderer being animated, if any.</summary>
        public readonly Object textRenderer;

        /// <summary>Reserved for a future per-character text effect: the character index within textRenderer, if any. -1 when not applicable.</summary>
        public readonly int characterIndex;

        public EffectContext(Transform target, GameObject owner, int index = 0, int totalCount = 1,
            Object textRenderer = null, int characterIndex = -1)
        {
            this.target = target;
            this.owner = owner;
            this.index = index;
            this.totalCount = totalCount;
            this.textRenderer = textRenderer;
            this.characterIndex = characterIndex;
        }

        public EffectContext WithTarget(Transform newTarget) =>
            new EffectContext(newTarget, owner, index, totalCount, textRenderer, characterIndex);
    }
}
