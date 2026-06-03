using UnityEngine;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Scene-side component. Holds a reference to a SplinePathSO asset and
    /// forwards all sampling calls to it. Multiple GameObjects can share one asset.
    /// If no asset is assigned a local inline instance is created automatically.
    /// </summary>
    [AddComponentMenu("Creatush/TweenEffects Pro/Spline Path")]
    [ExecuteAlways]
    public class SplinePath : MonoBehaviour
    {
        [Tooltip("Assign a SplinePathSO asset, or leave empty to use an embedded local path.")]
        [SerializeField] private SplinePathSO pathAsset;

        [SerializeField, HideInInspector]
        private SplinePathSO _localPath;

        public SplinePathSO Asset
        {
            get
            {
                if (pathAsset != null) return pathAsset;
                if (_localPath == null)
                {
                    _localPath      = ScriptableObject.CreateInstance<SplinePathSO>();
                    _localPath.name = "LocalPath";
                }
                return _localPath;
            }
        }

        // Passthrough convenience properties used by the editor
        public Color pathColor  { get => Asset.pathColor;  set => Asset.pathColor  = value; }
        public float pointSize  { get => Asset.pointSize;  set => Asset.pointSize  = value; }

        public System.Collections.Generic.List<SplinePathSO.SplineKnot> knots => Asset.knots;

        public void    BakePath()    => Asset.BakePath();
        public float   TotalLength   => Asset.TotalLength;
        public float   GetRollAtT(float t) => Asset.GetRollAtT(t);

        public Vector3 GetPointOnPath(float t,
            SplinePathSO.SpeedMode mode = SplinePathSO.SpeedMode.ConstantAcrossPath)
            => Asset.GetPointOnPath(t, mode);

        public Vector3 GetTangentOnPath(float t,
            SplinePathSO.SpeedMode mode = SplinePathSO.SpeedMode.ConstantAcrossPath)
            => Asset.GetTangentOnPath(t, mode);

        // World-space helpers — used only by Scene View / Handles drawing code
        public Vector3 GetWorldPointOnPath(float t,
            SplinePathSO.SpeedMode mode = SplinePathSO.SpeedMode.ConstantAcrossPath)
        {
            Matrix4x4 m = transform.parent != null
                ? transform.parent.localToWorldMatrix : Matrix4x4.identity;
            return m.MultiplyPoint3x4(Asset.GetPointOnPath(t, mode));
        }

        public Vector3 GetWorldTangentOnPath(float t,
            SplinePathSO.SpeedMode mode = SplinePathSO.SpeedMode.ConstantAcrossPath)
        {
            Matrix4x4 m = transform.parent != null
                ? transform.parent.localToWorldMatrix : Matrix4x4.identity;
            return m.MultiplyVector(Asset.GetTangentOnPath(t, mode));
        }
    }
}
