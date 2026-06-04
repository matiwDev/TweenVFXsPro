using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Reward fly effect. Items spawn from a pool near a source position,
    /// float briefly with rotation, then fly to a destination — optionally
    /// following a SplinePath instead of a straight line.
    ///
    /// Works in both world-space and UI (Canvas) contexts.
    /// Pooling is built-in — no runtime allocation after Awake.
    /// </summary>
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Reward Fly")]
    public class EffectRewardFly : MonoBehaviour
    {
        // ── Item setup ────────────────────────────────────────────────────────

        [Header("Item Setup")]
        [SerializeField, Tooltip("Prefab to spawn. UI Image, world sprite — anything works.")]
        private GameObject itemPrefab;

        [SerializeField, Min(1)]
        private int itemCount = 8;

        [SerializeField, Tooltip("Parent for spawned items. " +
            "For UI: must be a Canvas or RectTransform. " +
            "Defaults to this GameObject's own transform if left empty.")]
        private RectTransform spawnParent;

        // ── Spawn ─────────────────────────────────────────────────────────────

        [Header("Spawn")]
        [SerializeField, Tooltip("Random spawn radius in local/canvas units.")]
        private float spawnRadius = 80f;

        [SerializeField, Range(0f, 1f),
         Tooltip("0 = all items at source centre, 1 = fully spread to spawnRadius.")]
        private float spawnRandomness = 0.8f;

        [SerializeField, Min(0f), Tooltip("Seconds between successive item spawns.")]
        private float spawnStagger = 0.06f;

        [SerializeField, Tooltip("Pop items in from scale 0 on spawn.")]
        private bool popInOnSpawn = true;

        [SerializeField, Min(0.05f)]
        private float popInDuration = 0.18f;

        // ── Float phase ───────────────────────────────────────────────────────

        [Header("Float Phase")]
        [SerializeField, Min(0f), Tooltip("How long items float before flying.")]
        private float floatDuration = 0.55f;

        [SerializeField, Tooltip("How far items float upward from spawn.")]
        private float floatAmplitude = 40f;

        [SerializeField]
        private Ease floatEase = Ease.OutQuad;

        [SerializeField, Tooltip("Z rotation speed in degrees per second while floating.")]
        private float rotationSpeed = 90f;

        [SerializeField, Range(0f, 1f),
         Tooltip("Per-item variation on rotation direction and speed.")]
        private float rotationRandomness = 0.5f;

        // ── Fly phase ─────────────────────────────────────────────────────────

        [Header("Fly Phase")]
        [SerializeField, Tooltip("Where items fly to. Required.")]
        private RectTransform destination;

        [SerializeField, Min(0.1f)]
        private float flyDuration = 0.45f;

        [SerializeField]
        private Ease flyEase = Ease.InQuad;

        [SerializeField, Range(0f, 1f),
         Tooltip("Scale items reach on arrival. 0 = shrink to nothing, 1 = full size.")]
        private float flyArrivalScale = 0f;

        [SerializeField, Min(0f),
         Tooltip("Stagger between each item starting its fly. Creates a streaming feel.")]
        private float flyStagger = 0.07f;

        // ── Spline fly (optional) ─────────────────────────────────────────────

        [Header("Spline Fly (optional)")]
        [SerializeField, Tooltip("When assigned, items follow this spline to the destination " +
            "instead of a straight line. The spline's start maps to the item's float position " +
            "and the end maps to the destination.")]
        private SplinePath flySpline;

        [SerializeField,
         Tooltip("ConstantAcrossPath: equal screen distance per time.\n" +
                 "EqualPerSegment: raw Bezier T.")]
        private SplinePathSO.SpeedMode splineSpeedMode = SplinePathSO.SpeedMode.ConstantAcrossPath;

        // ── Events ────────────────────────────────────────────────────────────

        [Header("Events")]
        [SerializeField] private UnityEvent onAllArrived;
        [SerializeField] private UnityEvent onItemArrived;

        // ── Pool ──────────────────────────────────────────────────────────────

        private readonly List<GameObject> _pool = new List<GameObject>();
        private Coroutine _playCoroutine;
        private int       _arrivedCount;
        private int       _activeCount;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()     => BuildPool();
        private void OnDestroy() => ClearPool();

        // ── Public API ────────────────────────────────────────────────────────

        public void Play()
        {
            if (!Validate()) return;
            if (_playCoroutine != null) StopCoroutine(_playCoroutine);
            _playCoroutine = StartCoroutine(PlayRoutine());
        }

        public void Stop()
        {
            if (_playCoroutine != null) { StopCoroutine(_playCoroutine); _playCoroutine = null; }
            foreach (var item in _pool)
            {
                if (item == null) continue;
                item.transform.DOKill();
                item.SetActive(false);
            }
        }

        // ── Pool ──────────────────────────────────────────────────────────────

        private void BuildPool()
        {
            if (itemPrefab == null) return;
            ClearPool();
            Transform parent = spawnParent != null ? (Transform)spawnParent : transform;
            for (int i = 0; i < itemCount; i++)
            {
                var go = Instantiate(itemPrefab, parent);
                go.SetActive(false);
                go.name = $"{itemPrefab.name}_pool_{i}";
                _pool.Add(go);
            }
        }

        private void ClearPool()
        {
            foreach (var go in _pool) if (go != null) Destroy(go);
            _pool.Clear();
        }

        private GameObject GetPooledItem()
        {
            foreach (var go in _pool)
                if (go != null && !go.activeSelf) return go;

            // Grow pool on demand
            Transform parent = spawnParent != null ? (Transform)spawnParent : transform;
            var extra = Instantiate(itemPrefab, parent);
            extra.SetActive(false);
            extra.name = $"{itemPrefab.name}_pool_{_pool.Count}";
            _pool.Add(extra);
            return extra;
        }

        // ── Play routine ──────────────────────────────────────────────────────

        private IEnumerator PlayRoutine()
        {
            _arrivedCount = 0;
            _activeCount  = itemCount;

            // Convert source world position into spawnParent local canvas space.
            // This is the only correct approach when source and spawnParent
            // have different parent RectTransforms.
            Canvas canvas = spawnParent.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;

            Vector2 sourceAnchored;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                spawnParent, screenPoint, uiCamera, out sourceAnchored);

            if (flySpline != null) flySpline.BakePath();

            for (int i = 0; i < itemCount; i++)
            {
                GameObject item = GetPooledItem();
                if (item == null) { _activeCount--; continue; }

                // Randomised spawn position in canvas/local space
                Vector2 randOffset = Random.insideUnitCircle * spawnRadius * spawnRandomness;
                Vector2 spawnPos   = sourceAnchored + randOffset;

                var rt = item.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = spawnPos;
                    rt.localScale       = popInOnSpawn ? Vector3.zero : Vector3.one;
                    rt.localRotation    = Quaternion.identity;
                }
                else
                {
                    item.transform.localPosition = new Vector3(spawnPos.x, spawnPos.y, 0f);
                    item.transform.localScale    = popInOnSpawn ? Vector3.zero : Vector3.one;
                    item.transform.localRotation = Quaternion.identity;
                }

                item.SetActive(true);
                AnimateItem(item, i, spawnPos);

                if (spawnStagger > 0f)
                    yield return new WaitForSeconds(spawnStagger);
            }

            _playCoroutine = null;
        }

        private void AnimateItem(GameObject item, int itemIndex, Vector2 spawnPos)
        {
            var       rt       = item.GetComponent<RectTransform>();
            Transform t        = item.transform;
            Vector2   floatPos = spawnPos + Vector2.up * floatAmplitude;

            // Convert destination world position into spawnParent local space
            Canvas canvas = spawnParent.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            Vector2 destInParent;
            Vector2 destScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, destination.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                spawnParent, destScreen, uiCamera, out destInParent);

            float rotDir   = Random.value > 0.5f ? 1f : -1f;
            float rotSpeed = rotationSpeed * Mathf.Lerp(1f,
                Random.Range(0.5f, 1.5f), rotationRandomness);
            float totalRot = rotDir * rotSpeed * (floatDuration + flyDuration);

            Sequence seq = DOTween.Sequence();

            // ── Pop in ────────────────────────────────────────────────────────
            if (popInOnSpawn)
                seq.Append(t.DOScale(Vector3.one, popInDuration).SetEase(Ease.OutBack));
            else
                seq.AppendInterval(0f); // ensure sequence has at least one step

            // ── Float up ──────────────────────────────────────────────────────
            if (rt != null)
                seq.Append(rt.DOAnchorPos(floatPos, floatDuration).SetEase(floatEase));
            else
                seq.Append(t.DOLocalMove(new Vector3(floatPos.x, floatPos.y, 0f), floatDuration)
                    .SetEase(floatEase));

            // Rotation spans float + fly — starts after pop-in completes
            seq.Join(t.DOLocalRotate(new Vector3(0f, 0f, totalRot),
                floatDuration + flyDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear));

            // ── Fly delay per item ────────────────────────────────────────────
            if (flyStagger > 0f)
                seq.AppendInterval(itemIndex * flyStagger);

            // ── Fly phase ─────────────────────────────────────────────────────
            if (flySpline != null)
                AppendSplineFly(seq, t, rt, floatPos, destInParent);
            else
                AppendDirectFly(seq, t, rt, destInParent);

            // Scale down on fly
            seq.Join(t.DOScale(Vector3.one * flyArrivalScale, flyDuration).SetEase(flyEase));

            // ── Arrival ───────────────────────────────────────────────────────
            seq.OnComplete(() =>
            {
                item.SetActive(false);
                onItemArrived?.Invoke();
                _arrivedCount++;
                if (_arrivedCount >= _activeCount)
                    onAllArrived?.Invoke();
            });

            seq.SetLink(item, LinkBehaviour.KillOnDestroy);
            seq.Play();
        }

        // ── Fly variants ──────────────────────────────────────────────────────

        private void AppendDirectFly(Sequence seq, Transform t, RectTransform rt,
                                       Vector2 destInParent)
        {
            if (rt != null)
                seq.Append(rt.DOAnchorPos(destInParent, flyDuration).SetEase(flyEase));
            else
                seq.Append(t.DOLocalMove(destination.localPosition, flyDuration).SetEase(flyEase));
        }

        private void AppendSplineFly(Sequence seq, Transform t, RectTransform rt,
                                      Vector2 startPos, Vector2 destInParent)
        {
            float   val     = 0f;
            Vector2 destPos = destInParent;
            Vector2 splineOrigin = (Vector2)(Vector3)flySpline.GetPointOnPath(0f, splineSpeedMode);

            Tween splineTween = DOTween.To(
                getter: () => val,
                setter: v =>
                {
                    val = v;
                    Vector2 straight    = Vector2.Lerp(startPos, destPos, v);
                    Vector3 splinePt    = flySpline.GetPointOnPath(v, splineSpeedMode);
                    Vector2 splineOffset = new Vector2(splinePt.x, splinePt.y) - splineOrigin;
                    Vector2 finalPos    = straight + splineOffset * 0.5f;

                    if (rt != null) rt.anchoredPosition = finalPos;
                    else            t.localPosition     = new Vector3(finalPos.x, finalPos.y, 0f);
                },
                endValue: 1f,
                duration: flyDuration
            ).SetEase(flyEase);

            seq.Append(splineTween);
        }

        // ── Validation ────────────────────────────────────────────────────────

        private bool Validate()
        {
            if (itemPrefab == null)
            {
                Debug.LogWarning("[EffectRewardFly] No item prefab assigned.", this);
                return false;
            }
            if (destination == null)
            {
                Debug.LogWarning("[EffectRewardFly] No destination assigned.", this);
                return false;
            }
            if (spawnParent == null)
            {
                Debug.LogWarning("[EffectRewardFly] No spawn parent assigned. " +
                    "Assign the Canvas or a RectTransform parent.", this);
                return false;
            }
            return true;
        }
    }
}
