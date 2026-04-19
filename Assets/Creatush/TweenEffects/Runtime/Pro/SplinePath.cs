using System.Collections.Generic;
using UnityEngine;

namespace Creatush.TweenEffects
{
    public class SplinePath : MonoBehaviour
    {
        public enum SpeedMode { ConstantAcrossPath, EqualPerSegment }

        [HideInInspector] public List<SplineKnot> knots = new List<SplineKnot>();
        public Color pathColor = new Color(0f, 1f, 1f, 0.8f);
        public float pointSize = 0.2f;

        [System.Serializable]
        public struct SplineKnot
        {
            public Vector3 point;
            public Vector3 tangentOut;
            public bool isExpanded;
            public SplineKnot(Vector3 pos) { point = pos; tangentOut = new Vector3(100, 50, 0); isExpanded = false; }
        }

        private float[] _distanceTable;
        private float _totalLength;
        private const int Precision = 200;

        public void BakePath()
        {
            _distanceTable = new float[Precision + 1];
            _totalLength = 0;
            Vector3 lastP = GetRawPoint(0);
            _distanceTable[0] = 0;

            for (int i = 1; i <= Precision; i++)
            {
                float t = i / (float)Precision;
                Vector3 currentP = GetRawPoint(t);
                _totalLength += Vector3.Distance(lastP, currentP);
                _distanceTable[i] = _totalLength;
                lastP = currentP;
            }
        }

        public Vector3 GetPointOnPath(float t, SpeedMode mode)
        {
            if (knots.Count < 2) return Vector3.zero;

            if (mode == SpeedMode.ConstantAcrossPath)
            {
                if (_distanceTable == null || _distanceTable.Length == 0) BakePath();
                float targetDistance = Mathf.Clamp01(t) * _totalLength;
                int i = 0;
                for (; i < Precision; i++) { if (_distanceTable[i + 1] >= targetDistance) break; }

                float dStart = _distanceTable[i];
                float dEnd = _distanceTable[i + 1];
                float segmentPercent = (targetDistance - dStart) / (dEnd - dStart);
                return GetRawPoint(Mathf.Lerp(i / (float)Precision, (i + 1) / (float)Precision, segmentPercent));
            }

            // EqualPerSegment is just the raw Bezier T
            return GetRawPoint(t);
        }

        private Vector3 GetRawPoint(float t)
        {
            float segmentT = t * (knots.Count - 1);
            int index = Mathf.Min(Mathf.FloorToInt(segmentT), knots.Count - 2);
            float localT = segmentT - index;
            return CalculateBezier(knots[index].point, knots[index].point + knots[index].tangentOut, knots[index + 1].point, knots[index + 1].point, localT);
        }

        private Vector3 CalculateBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            return (u * u * u * p0) + (3 * u * u * t * p1) + (3 * u * t * t * p2) + (t * t * t * p3);
        }
    }
}