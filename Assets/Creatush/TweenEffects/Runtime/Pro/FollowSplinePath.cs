using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffects
{
    [AddComponentMenu("Creatush/Tween Effects/Behaviors/Follow Spline Path")]
    public class FollowSplinePath : VFXBehaviour
    {
        public SplinePath targetPath;
        public SplinePath.SpeedMode speedMode = SplinePath.SpeedMode.ConstantAcrossPath;
        public AnimationCurve scaleOverPath = AnimationCurve.Linear(0, 1, 1, 1);

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            Sequence s = InitializeSequence();
            if (targetPath == null) return s;

            targetPath.BakePath();
            RectTransform rect = target as RectTransform;
            float val = 0;

            Tween t = DOTween.To(() => val, x =>
            {
                val = x;
                float easedT = 0;

                if (speedMode == SplinePath.SpeedMode.ConstantAcrossPath)
                {
                    // Path-wide Ease
                    easedT = DOVirtual.EasedValue(0, 1, val, easeType);
                }
                else
                {
                    // Per-segment Ease
                    float totalSegments = targetPath.knots.Count - 1;
                    float segmentT = val * totalSegments;
                    int segmentIndex = Mathf.FloorToInt(segmentT);
                    float localT = segmentT - segmentIndex;

                    float easedLocalT = DOVirtual.EasedValue(0, 1, localT, easeType);
                    easedT = (segmentIndex + easedLocalT) / totalSegments;
                }

                Vector3 pos = targetPath.GetPointOnPath(Mathf.Clamp01(easedT), speedMode);

                if (rect != null) rect.anchoredPosition = pos;
                else target.localPosition = pos;

                target.localScale = Vector3.one * scaleOverPath.Evaluate(val);
            }, 1f, duration).SetEase(Ease.Linear);

            s.Append(t);
            return s;
        }
    }
}