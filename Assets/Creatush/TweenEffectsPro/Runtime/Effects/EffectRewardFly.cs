using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Spawns a burst of pooled items that pop in, float, then fly along a
    /// parabolic arc to a destination — the classic "coins fly to the wallet"
    /// reward animation.
    ///
    /// Unlike most effects this one manages its own pool of spawned
    /// GameObjects rather than animating a single target directly — ctx.target's
    /// position is used as the spawn source, so it still slots into an
    /// MasterSequenceController step like any other effect (Single mode, with the
    /// source object as the target).
    ///
    /// The old MonoBehaviour version drove spawning with a coroutine
    /// (WaitForSeconds between each item). Here every item's timing is known
    /// up front from serialized fields, so the whole burst is built as one
    /// declarative Sequence — each item's sequence is inserted at
    /// i * spawnStagger, with its spawn/activate step as its own leading
    /// callback — which plays, reverses, and reports GetDuration() exactly
    /// like every other effect, and finally fits into the master timeline
    /// of an MasterSequenceController.
    ///
    /// Implements IEffectLifecycle to release its pooled instances when the
    /// owning MasterSequenceController is destroyed — the same cleanup the old
    /// MonoBehaviour version did in OnDestroy().
    /// </summary>
    [System.Serializable]
    public class EffectRewardFly : EffectDefinition, IEffectLifecycle
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

        // ── Pool state — persists across plays, released via IEffectLifecycle ──

        private readonly List<GameObject> _pool = new List<GameObject>();
        private int _arrivedCount;
        private int _activeCount;
        private Canvas _canvas;
        private Camera _uiCamera;

        // ── EffectDefinition ─────────────────────────────────────────────────

        public override float GetDuration()
        {
            float lastIndex = Mathf.Max(0, itemCount - 1);
            float popIn = popInOnSpawn ? popInDuration : 0f;
            return lastIndex * spawnStagger + popIn + floatDuration + lastIndex * flyStagger + flyDuration;
        }

        public override Sequence BuildSequence(EffectContext ctx)
        {
            if (!Validate(ctx.target))
                return FinaliseSequence(DOTween.Sequence(), ctx.owner);

            EnsurePool(ctx);
            ResolveCanvas();

            _arrivedCount = 0;
            _activeCount = itemCount;

            Vector2 sourcePos = WorldToParentAnchored(ctx.target.position);
            Vector2 destPos = WorldToParentAnchored(destination.position);

            Sequence master = DOTween.Sequence();

            // The whole burst is built synchronously, up front, before any of
            // it plays — so none of this play's items have had their
            // SetActive(true) callback run yet when the next iteration asks
            // for a pooled item. Picking by activeSelf alone would therefore
            // hand back the same still-inactive slot (index 0) to every
            // iteration. reservedThisPlay tracks what's already been claimed
            // for THIS BuildSequence() call, on top of activeSelf still
            // correctly skipping any item genuinely mid-flight from an
            // earlier, still-overlapping play.
            var reservedThisPlay = new HashSet<GameObject>();

            for (int i = 0; i < itemCount; i++)
            {
                GameObject item = GetPooledItem(ctx, reservedThisPlay);
                if (item == null) { _activeCount--; continue; }
                reservedThisPlay.Add(item);

                Vector2 randOffset = Random.insideUnitCircle * spawnRadius * spawnRandomness;
                Vector2 spawnPos = sourcePos + randOffset;
                float itemArc = arcHeight + Random.Range(-arcVariance, arcVariance);

                Sequence itemSeq = BuildItemSequence(item, i, spawnPos, destPos, itemArc);
                master.Insert(i * spawnStagger, itemSeq);
            }

            return FinaliseSequence(master, ctx.owner);
        }

        // ── Item animation ────────────────────────────────────────────────────

        private Sequence BuildItemSequence(GameObject item, int itemIndex,
                                            Vector2 spawnPos, Vector2 destPos, float itemArc)
        {
            var rt = item.GetComponent<RectTransform>();
            Transform t = item.transform;

            Sequence seq = DOTween.Sequence();

            // ── Spawn — activate and place; the first thing this item's own
            // timeline does, so it stays hidden until its stagger turn arrives
            // even though the whole burst is built up front. ─────────────────
            seq.InsertCallback(0f, () =>
            {
                if (rt != null)
                {
                    rt.anchoredPosition = spawnPos;
                    rt.localScale = popInOnSpawn ? Vector3.zero : Vector3.one;
                    rt.localRotation = Quaternion.identity;
                }
                item.SetActive(true);
            });

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

            // ── Sprite sheet — cycles frames off the sequence's own elapsed
            // time for the item's full visible duration. Driven from OnUpdate
            // (not a joined Tween) specifically so it can never shift where the
            // next Append/AppendInterval lands — DOTween's Join/Insert extend the
            // sequence's known duration to cover whatever they add, which would
            // otherwise push the fly-stagger interval and fly tween to start
            // after this ran its full course instead of after floatTween alone.
            if (spriteFrames != null && spriteFrames.Length > 0)
            {
                var spriteImage = item.GetComponentInChildren<Image>();
                if (spriteImage != null)
                {
                    float frameDur = 1f / Mathf.Max(1, spriteFrameRate);
                    int frameCount = spriteFrames.Length;
                    seq.OnUpdate(() =>
                    {
                        int frame = Mathf.FloorToInt(seq.Elapsed() / frameDur) % frameCount;
                        spriteImage.sprite = spriteFrames[frame];
                    });
                }
            }

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
            return seq;
        }

        // ── Pool ──────────────────────────────────────────────────────────────

        private Transform PoolParent(EffectContext ctx) =>
            spawnParent != null ? (Transform)spawnParent : ctx.owner.transform;

        private void EnsurePool(EffectContext ctx)
        {
            if (_pool.Count > 0 || itemPrefab == null) return;
            Transform parent = PoolParent(ctx);
            for (int i = 0; i < itemCount; i++)
            {
                var go = Object.Instantiate(itemPrefab, parent);
                go.SetActive(false);
                go.name = $"{itemPrefab.name}_pool_{i}";
                _pool.Add(go);
            }
        }

        private GameObject GetPooledItem(EffectContext ctx, HashSet<GameObject> reservedThisPlay)
        {
            foreach (var go in _pool)
                if (go != null && !go.activeSelf && !reservedThisPlay.Contains(go)) return go;

            if (itemPrefab == null) return null;
            Transform parent = PoolParent(ctx);
            var extra = Object.Instantiate(itemPrefab, parent);
            extra.SetActive(false);
            extra.name = $"{itemPrefab.name}_pool_{_pool.Count}";
            _pool.Add(extra);
            return extra;
        }

        /// <summary>Releases pooled instances — called by the owning MasterSequenceController's OnDestroy.</summary>
        public void OnOwnerDestroyed(GameObject owner)
        {
            foreach (var go in _pool)
                if (go != null) Object.Destroy(go);
            _pool.Clear();
        }

        // ── Canvas helpers ────────────────────────────────────────────────────

        private void ResolveCanvas()
        {
            if (spawnParent == null) return;
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

        // ── Editor accessors (used by MasterSequenceControllerEditor's Scene View gizmos) ──

        public RectTransform Destination => destination;
        public float ArcHeight => arcHeight;
        public float ArcVariance => arcVariance;
        public float FloatHeight => floatHeight;
        public float SpawnRadius => spawnRadius;
        public RectTransform SpawnParent => spawnParent;

        // ── Validation ────────────────────────────────────────────────────────

        private bool Validate(Transform target)
        {
            if (itemPrefab == null)
            {
                Debug.LogWarning("[EffectRewardFly] No item prefab assigned.");
                return false;
            }
            if (spawnParent == null)
            {
                Debug.LogWarning("[EffectRewardFly] No spawn parent assigned.");
                return false;
            }
            if (destination == null)
            {
                Debug.LogWarning("[EffectRewardFly] No destination assigned.");
                return false;
            }
            if (target == null)
            {
                Debug.LogWarning("[EffectRewardFly] No source target assigned.");
                return false;
            }
            return true;
        }
    }
}
