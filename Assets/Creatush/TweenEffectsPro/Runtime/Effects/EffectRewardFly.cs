using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Reward Fly")]
    public class EffectRewardFly : MonoBehaviour
    {
        // ── Item setup ────────────────────────────────────────────────────────

        [Header("Item Setup")]
        [SerializeField] private GameObject itemPrefab;
        [SerializeField, Min(1)] private int itemCount = 8;
        [SerializeField] private RectTransform spawnParent;

        // ── Sprite sheet (optional) ───────────────────────────────────────────

        [Header("Sprite Sheet (optional)")]
        [SerializeField, Tooltip("Frames to cycle while the item is visible. Leave empty to use the prefab as-is.")]
        private Sprite[] spriteFrames;

        [SerializeField, Min(1), Tooltip("Frames per second.")]
        private int spriteFrameRate = 12;

        // ── Spawn ─────────────────────────────────────────────────────────────

        [Header("Spawn")]
        [SerializeField, Tooltip("Scatter radius in canvas units.")]
        private float spawnRadius = 80f;

        [SerializeField, Range(0f, 1f),
         Tooltip("0 = tight cluster at source, 1 = full scatter to radius.")]
        private float spawnRandomness = 0.8f;

        [SerializeField, Min(0f), Tooltip("Seconds between successive item spawns.")]
        private float spawnStagger = 0.06f;

        [SerializeField, Tooltip("Scale items from 0 on spawn.")]
        private bool popInOnSpawn = true;

        [SerializeField, Min(0.05f)]
        private float popInDuration = 0.18f;

        // ── Float phase ───────────────────────────────────────────────────────

        [Header("Float Phase")]
        [SerializeField, Min(0f), Tooltip("How long items float before flying (seconds).")]
        private float floatDuration = 0.6f;

        [SerializeField, Tooltip("Oscillation amplitude in canvas units (how far up and down).")]
        private float floatHeight = 30f;

        [SerializeField, Min(0.1f),
         Tooltip("Oscillation speed in cycles per second.\n1 = one full up-down per second.")]
        private float floatSpeed = 1.5f;

        // ── Fly phase ─────────────────────────────────────────────────────────

        [Header("Fly Phase")]
        [SerializeField, Tooltip("Where items fly to. Required.")]
        private RectTransform destination;

        [SerializeField, Min(0.1f), Tooltip("Fly duration in seconds.")]
        private float flyDuration = 0.5f;

        [SerializeField, Tooltip("Peak arc height in canvas units. 0 = straight line.")]
        private float arcHeight = 100f;

        [SerializeField, Tooltip("Random variation on arc height per item.")]
        private float arcVariance = 30f;

        [SerializeField]
        private Ease flyEase = Ease.InQuad;

        [SerializeField, Range(0f, 1f), Tooltip("Scale on arrival. 0 = shrink to nothing.")]
        private float flyArrivalScale = 0f;

        [SerializeField, Min(0f), Tooltip("Stagger between items starting their fly.")]
        private float flyStagger = 0.08f;

        // ── Events ────────────────────────────────────────────────────────────

        [Header("Events")]
        [SerializeField] private UnityEvent onAllArrived;
        [SerializeField] private UnityEvent onItemArrived;

        // ── Private state ─────────────────────────────────────────────────────

        private readonly List<GameObject> _pool = new List<GameObject>();
        private Coroutine _playCoroutine;
        private int _arrivedCount;
        private int _activeCount;
        private Canvas _canvas;
        private Camera _uiCamera;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake() => BuildPool();
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
            Transform parent = spawnParent != null ? (Transform)spawnParent : transform;
            var extra = Instantiate(itemPrefab, parent);
            extra.SetActive(false);
            extra.name = $"{itemPrefab.name}_pool_{_pool.Count}";
            _pool.Add(extra);
            return extra;
        }

        // ── Canvas helpers ────────────────────────────────────────────────────

        private void ResolveCanvas()
        {
            _canvas = spawnParent.GetComponentInParent<Canvas>();
            _uiCamera = _canvas != null &&
                        _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera : null;
        }

        private Vector2 WorldToParentAnchored(Vector3 worldPos)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(_uiCamera, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                spawnParent, screen, _uiCamera, out Vector2 local);
            return local;
        }

        // ── Play routine ──────────────────────────────────────────────────────

        private IEnumerator PlayRoutine()
        {
            _arrivedCount = 0;
            _activeCount = itemCount;

            ResolveCanvas();

            Vector2 sourcePos = WorldToParentAnchored(transform.position);
            Vector2 destPos = WorldToParentAnchored(destination.position);

            for (int i = 0; i < itemCount; i++)
            {
                GameObject item = GetPooledItem();
                if (item == null) { _activeCount--; continue; }

                Vector2 randOffset = Random.insideUnitCircle * spawnRadius * spawnRandomness;
                Vector2 spawnPos = sourcePos + randOffset;

                var rt = item.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = spawnPos;
                    rt.localScale = popInOnSpawn ? Vector3.zero : Vector3.one;
                    rt.localRotation = Quaternion.identity;
                }

                item.SetActive(true);

                float itemArc = arcHeight + Random.Range(-arcVariance, arcVariance);
                AnimateItem(item, i, spawnPos, destPos, itemArc);

                if (spawnStagger > 0f)
                    yield return new WaitForSeconds(spawnStagger);
            }

            _playCoroutine = null;
        }

        // ── Item animation ────────────────────────────────────────────────────

        private void AnimateItem(GameObject item, int itemIndex,
                                  Vector2 spawnPos, Vector2 destPos, float itemArc)
        {
            var rt = item.GetComponent<RectTransform>();
            Transform t = item.transform;

            float totalDur = floatDuration + flyStagger * itemIndex + flyDuration;

            Sequence seq = DOTween.Sequence();

            // ── Pop in ────────────────────────────────────────────────────────
            if (popInOnSpawn)
                seq.Append(t.DOScale(Vector3.one, popInDuration).SetEase(Ease.OutBack));

            // ── Float — sine-wave oscillation ─────────────────────────────────
            float floatVal = 0f;
            float floatStartY = spawnPos.y;

            Tween floatTween = DOTween.To(
                getter: () => floatVal,
                setter: v =>
                {
                    floatVal = v;
                    float yOffset = Mathf.Sin(v * floatSpeed * Mathf.PI * 2f) * floatHeight;
                    if (rt != null)
                        rt.anchoredPosition = new Vector2(spawnPos.x, floatStartY + yOffset);
                    else
                        t.localPosition = new Vector3(spawnPos.x,
                            floatStartY + yOffset, t.localPosition.z);
                },
                endValue: 1f,
                duration: floatDuration
            ).SetEase(Ease.Linear);

            seq.Append(floatTween);

            // ── Sprite sheet — joins float, runs full visible duration ─────────
            if (spriteFrames != null && spriteFrames.Length > 0)
                seq.Join(BuildSpriteSheetTween(item, totalDur));

            // ── Fly stagger ───────────────────────────────────────────────────
            if (flyStagger > 0f)
                seq.AppendInterval(itemIndex * flyStagger);

            // ── Fly — parabolic arc ───────────────────────────────────────────
            Vector2 floatPos = new Vector2(spawnPos.x, floatStartY);
            float flyVal = 0f;

            Tween flyTween = DOTween.To(
                getter: () => flyVal,
                setter: v =>
                {
                    flyVal = v;
                    Vector2 straight = Vector2.Lerp(floatPos, destPos, v);
                    float parabola = 4f * v * (1f - v) * itemArc;
                    Vector2 pos = straight + Vector2.up * parabola;

                    if (rt != null) rt.anchoredPosition = pos;
                    else t.localPosition = new Vector3(pos.x, pos.y, 0f);
                },
                endValue: 1f,
                duration: flyDuration
            ).SetEase(Ease.Linear);

            seq.Append(flyTween);
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

        // ── Sprite sheet ──────────────────────────────────────────────────────

        private Tween BuildSpriteSheetTween(GameObject item, float totalDuration)
        {
            var image = item.GetComponentInChildren<Image>();
            if (image == null) return DOTween.Sequence();

            int frameCount = spriteFrames.Length;
            float frameDur = 1f / Mathf.Max(1, spriteFrameRate);
            int totalFrames = Mathf.Max(1, Mathf.RoundToInt(totalDuration / frameDur));
            int frameIndex = 0;

            return DOTween.To(
                getter: () => frameIndex,
                setter: v =>
                {
                    frameIndex = v;
                    image.sprite = spriteFrames[v % frameCount];
                },
                endValue: totalFrames,
                duration: totalDuration
            ).SetEase(Ease.Linear);
        }

        // ── Editor accessors ──────────────────────────────────────────────────

        public RectTransform Destination => destination;
        public float ArcHeight => arcHeight;
        public float ArcVariance => arcVariance;
        public float FloatHeight => floatHeight;
        public float SpawnRadius => spawnRadius;
        public RectTransform SpawnParent => spawnParent;

        // ── Validation ────────────────────────────────────────────────────────

        private bool Validate()
        {
            if (itemPrefab == null)
            {
                Debug.LogWarning("[EffectRewardFly] No item prefab assigned.", this);
                return false;
            }
            if (spawnParent == null)
            {
                Debug.LogWarning("[EffectRewardFly] No spawn parent assigned.", this);
                return false;
            }
            if (destination == null)
            {
                Debug.LogWarning("[EffectRewardFly] No destination assigned.", this);
                return false;
            }
            return true;
        }
    }
}
