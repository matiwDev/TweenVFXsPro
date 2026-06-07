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
         Tooltip("Squash and stretch strength.\n0 = no deformation, 1 = full cartoon squash/stretch.")]
        private float squashStrength = 0.3f;

        [SerializeField, Range(0f, 2f),
         Tooltip("How springy the landing recovery is.\n0 = snaps back instantly, 2 = very bouncy overshoot.")]
        private float bounciness = 1f;

        public override float GetDuration() { return duration; }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            if (target == null) return null;

            Vector3 originPos = target.localPosition;
            Vector3 originScale = target.localScale;

            // ── Scale values ──────────────────────────────────────────────────
            float stretchFactor = 1f + squashStrength;
            float squashFactor = 1f - squashStrength * 0.5f;

            Vector3 stretchScale = new Vector3(
                originScale.x * squashFactor,
                originScale.y * stretchFactor,
                originScale.z * squashFactor);

            Vector3 landScale = new Vector3(
                originScale.x * (1f + squashStrength * 0.4f),
                originScale.y * (1f - squashStrength * 0.8f),
                originScale.z * (1f + squashStrength * 0.4f));

            // ── Pivot simulation via position offset ───────────────────────────
            // When we stretch the Y scale, the object grows around its centre.
            // To simulate "pivot=bottom" (bottom stays planted), we shift the
            // object up by half the extra height so the bottom doesn't move.
            // extraHeight = originScale.y * (stretchFactor - 1) * 0.5
            float extraHeight = originScale.y * squashStrength * 0.5f;

            // Stretch-up planted position (bottom stays on floor)
            Vector3 stretchUpPos = originPos + Vector3.up * extraHeight;

            // At apex, object is at bounceHeight + stretchUpPos.y
            // When we unstretch at apex, centre moves back down by extraHeight
            Vector3 apexPos = new Vector3(originPos.x,
                                         originPos.y + bounceHeight,
                                         originPos.z);
            Vector3 apexStretchPos = apexPos + Vector3.up * extraHeight;

            // Landing squash: bottom stays on floor, object squashes down
            // squashY = originScale.y * (1 - squashStrength*0.8)
            // object centre drops by half the lost height
            float lostHeight = originScale.y * squashStrength * 0.8f * 0.5f;
            Vector3 landSquashPos = originPos - Vector3.up * lostHeight;

            // ── Timing ────────────────────────────────────────────────────────
            float tStretchUp = duration * 0.08f;
            float tRise = duration * 0.37f;
            float tUnstretch = duration * 0.08f;
            float tFall = duration * 0.37f;
            float tLandSquash = duration * 0.10f;
            float tRecover = duration * 0.18f;

            Sequence seq = DOTween.Sequence();

            // ── 1. Stretch up — bottom stays planted ──────────────────────────
            // Scale and position change together so bottom is fixed
            seq.Append(target.DOScale(stretchScale, tStretchUp).SetEase(Ease.OutQuad));
            seq.Join(target.DOLocalMove(stretchUpPos, tStretchUp).SetEase(Ease.OutQuad));

            // ── 2. Rise to apex — stretched, bottom lifts off ─────────────────
            seq.Append(ApplyEase(target.DOLocalMove(apexStretchPos, tRise)));

            // ── 3. Unstretch at apex — top stays fixed ────────────────────────
            // Scale shrinks back, position drops by extraHeight to keep top fixed
            seq.Append(target.DOScale(originScale, tUnstretch).SetEase(Ease.InOutQuad));
            seq.Join(target.DOLocalMove(apexPos, tUnstretch).SetEase(Ease.InOutQuad));

            // ── 4. Fall to ground ─────────────────────────────────────────────
            seq.Append(target.DOLocalMove(originPos, tFall).SetEase(Ease.InQuad));

            // ── 5. Landing squash — bottom stays planted ──────────────────────
            seq.Append(target.DOScale(landScale, tLandSquash).SetEase(Ease.OutQuad));
            seq.Join(target.DOLocalMove(landSquashPos, tLandSquash).SetEase(Ease.OutQuad));

            // ── 6. Recovery spring ────────────────────────────────────────────
            seq.Append(target.DOScale(originScale, tRecover)
                .SetEase(Ease.OutBack, 1f + bounciness));
            seq.Join(target.DOLocalMove(originPos, tRecover)
                .SetEase(Ease.OutBack, 1f + bounciness));

            seq.OnComplete(() =>
            {
                target.localPosition = originPos;
                target.localScale = originScale;
            });

            return FinaliseSequence(seq);
        }
    }
}
