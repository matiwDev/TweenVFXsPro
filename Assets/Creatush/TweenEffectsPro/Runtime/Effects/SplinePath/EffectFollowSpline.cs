using UnityEngine;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Moves a target along a cubic Bézier spline defined by a SplinePath component.
    /// Supports constant-speed arc-length remapping, orient-to-path, scale-over-path,
    /// and PingPong / Loop playback — all without the Animator.
    ///
    /// Loop note: this effect owns its loop behaviour via splineLoopMode rather than
    /// the base loop fields, because PingPong maps to DOTween Yoyo which the base
    /// ApplyLoop does not support. The base loop fields are intentionally unused here.
    /// </summary>
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Follow Spline")]
    public class EffectFollowSpline : VFXBehaviour
    {
        // ── Path ──────────────────────────────────────────────────────────────

        [Header("Path")]
        [SerializeField] private SplinePath targetPath;

        [SerializeField,
         Tooltip("ConstantAcrossPath: arc-length remap — equal screen distance per unit time.\n" +
                 "EqualPerSegment: raw Bézier T — faster in middles, slower near knots.")]
        private SplinePathSO.SpeedMode speedMode = SplinePathSO.SpeedMode.ConstantAcrossPath;

        // ── Playback ──────────────────────────────────────────────────────────

        [Header("Playback")]
        [SerializeField] private SplinePathSO.LoopMode splineLoopMode = SplinePathSO.LoopMode.Once;

        [SerializeField, Tooltip("How many times to loop. -1 = infinite. Only used when Loop Mode is not Once.")]
        private int splineLoopCount = -1;

        [SerializeField, Range(0f, 1f),
         Tooltip("Normalised start point along the path (0 = beginning, 1 = end).")]
        private float startOffset = 0f;

        [SerializeField,
         Tooltip("If enabled the target's position is restored to the start of the path " +
                 "when the sequence ends. Disable to keep it at the end point.")]
        private bool resetOnComplete = false;

        // ── Orientation ───────────────────────────────────────────────────────

        [Header("Orientation")]
        [SerializeField] private bool orientToPath = false;

        [SerializeField,
         Tooltip("2D mode: rotates on the Z axis to face the path tangent.\n" +
                 "Use for UI and top-down sprites.\n" +
                 "3D mode: uses LookRotation — suits world-space objects.")]
        private bool orient2D = true;

        [SerializeField,
         Tooltip("Additional rotation added on top of path orientation (Euler degrees).")]
        private Vector3 rotationOffset = Vector3.zero;

        [SerializeField, Range(0f, 1f),
         Tooltip("Smoothing applied to orientation changes. 0 = instant snap.")]
        private float orientSmoothing = 0f;

        // ── Scale over path ───────────────────────────────────────────────────

        [Header("Scale Over Path")]
        [CurvePreset,
         SerializeField,
         Tooltip("Drives localScale uniformly along the path.\n" +
                 "X = normalised path position (0–1), Y = scale multiplier.")]
        private AnimationCurve scaleOverPath = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        // ── Events ────────────────────────────────────────────────────────────

        [Header("Events")]
        [SerializeField] private UnityEngine.Events.UnityEvent onPathComplete;

        // ── Internal per-play state ───────────────────────────────────────────

        private float      _val;
        private Quaternion _lastOrientation;
        private Vector3    _startPos;

        // ── VFXBehaviour ──────────────────────────────────────────────────────

        public override float GetDuration() { return duration; }

        public override Sequence BuildSequence(int index, int totalCount, Transform target)
        {
            Sequence seq = DOTween.Sequence();

            if (targetPath == null)
            {
                Debug.LogWarning("[EffectFollowSpline] No SplinePath assigned.", this);
                return FinaliseSequence(seq);
            }

            targetPath.BakePath();

            SplinePathSO asset = targetPath.Asset;
            if (asset.overridePlayback)
            {
                speedMode       = asset.defaultSpeedMode;
                splineLoopMode  = asset.defaultLoopMode;
                splineLoopCount = asset.defaultLoopCount;
                startOffset     = asset.defaultStartOffset;
            }

            RectTransform rect = target.GetComponent<RectTransform>();

            // Cache start position for optional reset on complete
            _startPos        = rect != null
                ? (Vector3)rect.anchoredPosition
                : target.localPosition;

            // Reset per-sequence state
            _val             = startOffset;
            _lastOrientation = target.rotation;

            // SetEase(Linear) — easing is applied inside RemapT to avoid compounding
            Tween tween = DOTween.To(
                ()  => _val,
                x   => ApplyPathSample(x, target, rect),
                1f,
                duration
            ).SetEase(Ease.Linear);

            if (splineLoopMode != SplinePathSO.LoopMode.Once)
            {
                LoopType lt = splineLoopMode == SplinePathSO.LoopMode.PingPong
                    ? LoopType.Yoyo
                    : LoopType.Restart;
                tween.SetLoops(splineLoopCount, lt);
            }

            tween.OnComplete(() =>
            {
                if (resetOnComplete)
                {
                    if (rect != null) rect.anchoredPosition = _startPos;
                    else              target.localPosition  = _startPos;
                }
                onPathComplete?.Invoke();
            });

            seq.Append(tween);

            // Use KillOnDestroy rather than KillOnDisable — prevents the sequence
            // being killed unexpectedly when the source GO is disabled mid-animation.
            seq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            return seq;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ApplyPathSample(float rawVal, Transform target, RectTransform rect)
        {
            _val = rawVal;

            float   easedT = RemapT(rawVal);
            Vector3 pos    = targetPath.GetPointOnPath(Mathf.Clamp01(easedT), speedMode);

            if (rect != null) rect.anchoredPosition = pos;
            else              target.localPosition   = pos;

            // Scale
            float s = scaleOverPath.Evaluate(rawVal);
            target.localScale = new Vector3(
                target.localScale.x * s / Mathf.Max(0.001f, target.localScale.x) * target.localScale.x,
                target.localScale.y * s / Mathf.Max(0.001f, target.localScale.y) * target.localScale.y,
                target.localScale.z * s / Mathf.Max(0.001f, target.localScale.z) * target.localScale.z);
            // Simplified: uniform scale multiplier relative to unit scale
            target.localScale = Vector3.one * s;

            // Orientation
            if (orientToPath)
            {
                Vector3 tangent = targetPath.GetTangentOnPath(Mathf.Clamp01(easedT), speedMode);
                if (tangent.sqrMagnitude > 0.001f)
                {
                    float      roll    = targetPath.GetRollAtT(Mathf.Clamp01(easedT));
                    Quaternion pathRot;

                    if (orient2D)
                    {
                        // 2D: rotate on Z axis — angle of the tangent in local XY plane
                        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                        pathRot = Quaternion.Euler(rotationOffset.x,
                                                   rotationOffset.y,
                                                   angle + rotationOffset.z + roll);
                    }
                    else
                    {
                        // 3D: LookRotation — use world-space tangent
                        Vector3 worldTangent = targetPath.GetWorldTangentOnPath(
                            Mathf.Clamp01(easedT), speedMode);
                        if (worldTangent.sqrMagnitude > 0.001f)
                            pathRot = Quaternion.LookRotation(worldTangent)
                                    * Quaternion.Euler(rotationOffset)
                                    * Quaternion.Euler(0f, 0f, roll);
                        else
                            pathRot = _lastOrientation;
                    }

                    target.rotation = orientSmoothing > 0f
                        ? Quaternion.Slerp(_lastOrientation, pathRot, 1f - orientSmoothing)
                        : pathRot;

                    _lastOrientation = target.rotation;
                }
            }
        }

        private float RemapT(float rawVal)
        {
            if (speedMode == SplinePathSO.SpeedMode.ConstantAcrossPath)
            {
                return easeMode == EaseMode.Curve
                    ? easeCurve.Evaluate(rawVal)
                    : DOVirtual.EasedValue(0f, 1f, rawVal, ease);
            }

            if (rawVal >= 1f) return 1f;

            int   segCount   = Mathf.Max(1, targetPath.Asset.knots.Count - 1);
            float segmentT   = rawVal * segCount;
            int   segIdx     = Mathf.Clamp(Mathf.FloorToInt(segmentT), 0, segCount - 1);
            float localT     = Mathf.Clamp01(segmentT - segIdx);
            float easedLocal = easeMode == EaseMode.Curve
                ? easeCurve.Evaluate(localT)
                : DOVirtual.EasedValue(0f, 1f, localT, ease);

            return Mathf.Clamp01((segIdx + easedLocal) / segCount);
        }

        public SplinePath             TargetPath    => targetPath;
        public SplinePathSO.SpeedMode SpeedMode     => speedMode;
        public AnimationCurve         ScaleOverPath => scaleOverPath;
    }
}
