using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Bounce")]
    public class EffectBounce : VFXBehaviour
    {
        [Header("Bounce Settings")]
        [SerializeField, Tooltip("Height of the bounce in local units.")]
        private float bounceHeight = 40f;

        [SerializeField, Range(0f, 1f),
         Tooltip("Squash and stretch strength.\n" +
                 "0 = no deformation, 1 = full cartoon squash/stretch.")]
        private float squashStrength = 0.3f;

        [SerializeField, Range(0f, 2f),
         Tooltip("How springy the landing recovery is.\n" +
                 "0 = snaps back instantly, 2 = very bouncy overshoot.")]
        private float bounciness = 1f;

        // ── VFXBehaviour ──────────────────────────────────────────────────────

        public override float GetDuration() { return duration; }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            Vector3 originPos   = target.localPosition;
            Vector3 originScale = target.localScale;

            // ── Timing breakdown ──────────────────────────────────────────────
            // 0.0 – riseTime  : rise to apex
            // riseTime        : apex (brief scale normalise)
            // riseTime – dur  : fall back to ground (impact at t = duration)
            // dur – dur+recov : landing recovery spring
            float riseTime    = duration * 0.45f;
            float fallTime    = duration * 0.55f;
            float impactTime  = riseTime + fallTime; // = duration
            float recovTime   = duration * 0.2f;

            // ── Scale values ──────────────────────────────────────────────────
            // Rise stretch: tall + thin
            float riseStretchY = 1f + squashStrength;
            float riseSquashXZ = 1f - squashStrength * 0.5f;
            Vector3 riseScale  = new Vector3(
                originScale.x * riseSquashXZ,
                originScale.y * riseStretchY,
                originScale.z * riseSquashXZ);

            // Landing squash: wide + flat
            float landSquashY  = 1f - squashStrength * 0.8f;
            float landStretchX = 1f + squashStrength * 0.4f;
            Vector3 landScale  = new Vector3(
                originScale.x * landStretchX,
                originScale.y * landSquashY,
                originScale.z * landStretchX);

            Sequence seq = DOTween.Sequence();

            // ── Position ──────────────────────────────────────────────────────
            // Rise
            seq.Insert(0f,
                ApplyEase(target.DOLocalMoveY(originPos.y + bounceHeight, riseTime)));
            // Fall — InQuad for gravity feel
            seq.Insert(riseTime,
                target.DOLocalMoveY(originPos.y, fallTime).SetEase(Ease.InQuad));

            // ── Scale ─────────────────────────────────────────────────────────
            // Stretch on the way up (first 40% of rise)
            seq.Insert(0f,
                target.DOScale(riseScale, riseTime * 0.4f).SetEase(Ease.OutQuad));
            // Return to normal at apex
            seq.Insert(riseTime * 0.4f,
                target.DOScale(originScale, riseTime * 0.6f).SetEase(Ease.InOutQuad));
            // Pre-squash during fall (last 20% of fall, anticipates landing)
            seq.Insert(riseTime + fallTime * 0.8f,
                target.DOScale(landScale, fallTime * 0.2f).SetEase(Ease.InQuad));
            // Recovery spring from land scale back to origin
            seq.Insert(impactTime,
                target.DOScale(originScale, recovTime)
                    .SetEase(Ease.OutBack, 1f + bounciness));

            // ── Cleanup ───────────────────────────────────────────────────────
            seq.OnComplete(() =>
            {
                target.localPosition = originPos;
                target.localScale    = originScale;
            });

            return FinaliseSequence(seq);
        }
    }
}
