using System.Collections.Generic;
using UnityEngine;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Pure data + math asset. No scene dependency.
    /// Create via: Assets > Create > Creatush > TweenEffects Pro > Spline Path
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewSplinePath",
        menuName = "Creatush/TweenEffects Pro/Spline Path")]
    public class SplinePathSO : ScriptableObject
    {
        // ── Enums ─────────────────────────────────────────────────────────────

        public enum SpeedMode
        {
            /// <summary>Arc-length remap: equal screen distance per unit time.</summary>
            ConstantAcrossPath,
            /// <summary>Raw Bézier T: faster in middles, slower near knots.</summary>
            EqualPerSegment
        }

        public enum LoopMode { Once, Loop, PingPong }

        // ── Knot ──────────────────────────────────────────────────────────────

        [System.Serializable]
        public struct SplineKnot
        {
            public Vector3 point;
            public Vector3 tangentOut;
            public Vector3 tangentIn;
            [Range(-180f, 180f)] public float roll;
            [HideInInspector] public bool isExpanded;

            public SplineKnot(Vector3 pos)
            {
                point = pos;
                tangentOut = new Vector3(80, 0, 0);
                tangentIn = new Vector3(-80, 0, 0);
                roll = 0f;
                isExpanded = false;
            }
        }

        // ── Serialized settings ───────────────────────────────────────────────

        [HideInInspector] public List<SplineKnot> knots = new List<SplineKnot>();
        [HideInInspector] public bool closedLoop = false;
        [HideInInspector] public int arcPrecision = 200;
        [HideInInspector] public Color pathColor = new Color(0f, 0.9f, 0.9f, 0.85f);
        [HideInInspector] public Color tangentColor = new Color(1f, 0.8f, 0.2f, 0.7f);
        [HideInInspector] public float pointSize = 12f;

        // ── Optional playback defaults ────────────────────────────────────────

        [HideInInspector] public bool overridePlayback = false;
        [HideInInspector] public SpeedMode defaultSpeedMode = SpeedMode.ConstantAcrossPath;
        [HideInInspector] public LoopMode defaultLoopMode = LoopMode.Once;
        [HideInInspector] public int defaultLoopCount = 1;
        [HideInInspector] public float defaultStartOffset = 0f;

        // ── Baked arc-length table ────────────────────────────────────────────

        private float[] _distanceTable;
        private float _totalLength;
        private bool _isDirty = true;

        public void BakePath()
        {
            int knotCount = ActiveKnotCount();
            if (knotCount < 2)
            {
                _distanceTable = new float[] { 0f };
                _totalLength = 0f;
                _isDirty = false;
                return;
            }

            _distanceTable = new float[arcPrecision + 1];
            _totalLength = 0f;
            Vector3 lastPoint = GetRawPoint(0f);
            _distanceTable[0] = 0f;

            for (int i = 1; i <= arcPrecision; i++)
            {
                float t = i / (float)arcPrecision;
                Vector3 currentPoint = GetRawPoint(t);
                _totalLength += Vector3.Distance(lastPoint, currentPoint);
                _distanceTable[i] = _totalLength;
                lastPoint = currentPoint;
            }

            _isDirty = false;
        }

        public void MarkDirty() => _isDirty = true;

        // ── Sampling API ──────────────────────────────────────────────────────

        public Vector3 GetPointOnPath(float t, SpeedMode mode = SpeedMode.ConstantAcrossPath)
        {
            int knotCount = ActiveKnotCount();
            if (knotCount < 2) return knots.Count > 0 ? knots[0].point : Vector3.zero;
            if (_isDirty) BakePath();

            if (mode == SpeedMode.ConstantAcrossPath)
            {
                if (_totalLength <= 0f) return GetRawPoint(0f);

                float targetDist = Mathf.Clamp01(t) * _totalLength;
                int lo = 0, hi = arcPrecision;
                while (lo < hi - 1)
                {
                    int mid = (lo + hi) / 2;
                    if (_distanceTable[mid] < targetDist) lo = mid;
                    else hi = mid;
                }

                float dStart = _distanceTable[lo];
                float dEnd = _distanceTable[hi];
                float span = dEnd - dStart;
                float frac = span > 0f ? (targetDist - dStart) / span : 0f;
                float rawT = Mathf.Lerp(lo / (float)arcPrecision, hi / (float)arcPrecision, frac);
                return GetRawPoint(rawT);
            }

            return GetRawPoint(Mathf.Clamp01(t));
        }

        public Vector3 GetTangentOnPath(float t, SpeedMode mode = SpeedMode.ConstantAcrossPath)
        {
            const float eps = 0.001f;
            Vector3 a = GetPointOnPath(Mathf.Clamp01(t - eps), mode);
            Vector3 b = GetPointOnPath(Mathf.Clamp01(t + eps), mode);
            Vector3 dir = b - a;
            return dir.sqrMagnitude > 0f ? dir.normalized : Vector3.forward;
        }

        public float GetRollAtT(float t)
        {
            int knotCount = ActiveKnotCount();
            if (knotCount < 2) return 0f;
            float segmentT = Mathf.Clamp01(t) * (knotCount - 1);
            int index = Mathf.Min(Mathf.FloorToInt(segmentT), knotCount - 2);
            float localT = segmentT - index;
            return Mathf.LerpAngle(knots[index].roll, knots[index + 1].roll, localT);
        }

        public float TotalLength
        {
            get { if (_isDirty) BakePath(); return _totalLength; }
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private int ActiveKnotCount() =>
            closedLoop && knots.Count >= 2 ? knots.Count + 1 : knots.Count;

        private Vector3 GetRawPoint(float t)
        {
            int knotCount = ActiveKnotCount();
            if (knotCount < 2) return knots.Count > 0 ? knots[0].point : Vector3.zero;

            float segmentT = Mathf.Clamp01(t) * (knotCount - 1);
            int index = Mathf.Min(Mathf.FloorToInt(segmentT), knotCount - 2);
            float localT = segmentT - index;
            int i0 = index % knots.Count;
            int i1 = (index + 1) % knots.Count;

            return CubicBezier(
                knots[i0].point,
                knots[i0].point + knots[i0].tangentOut,
                knots[i1].point + knots[i1].tangentIn,
                knots[i1].point,
                localT);
        }

        private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            return (uu * u * p0) + (3f * uu * t * p1) + (3f * u * tt * p2) + (tt * t * p3);
        }
    }
}
