using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Reward fly effect: spawns items from a pool at a source position,
    /// floats them briefly with idle rotation, then flies them to a destination.
    ///
    /// Pooling is built-in — items are pre-instantiated at startup and reused
    /// each play with no runtime allocation.
    ///
    /// Designed to be triggered directly (call Play()) or wired to a UnityEvent.
    /// Does not inherit VFXBehaviour because it manages its own pool lifecycle
    /// and is not composable inside SequenceEffectsController.
    /// </summary>
    [AddComponentMenu("Creatush/TweenEffects Pro/Effect Reward Fly")]
    public class EffectRewardFly : MonoBehaviour
    {
        // ── Item setup ────────────────────────────────────────────────────────

        [Header("Item Setup")]
        [SerializeField, Tooltip("Prefab to spawn. Can be any UI or world-space GameObject.")]
        private GameObject itemPrefab;

        [SerializeField, Min(1), Tooltip("How many items to spawn per play.")]
        private int itemCount = 8;

        [SerializeField, Tooltip("Parent transform for spawned items. " +
                                  "Defaults to this GameObject's parent if left empty.")]
        private Transform spawnParent;

        // ── Spawn offset ──────────────────────────────────────────────────────

        [Header("Spawn Offset")]
        [SerializeField, Tooltip("Items spawn at the source position plus a random offset " +
                                  "within this radius (in local units).")]
        private float spawnRadius = 60f;

        [SerializeField, Range(0f, 1f),
         Tooltip("0 = all items spawn at exactly the source position.\n" +
                 "1 = items spread to the full spawnRadius.\n" +
                 "Values in between give a tighter or looser cluster.")]
        private float spawnRandomness = 0.8f;

        [SerializeField, Tooltip("Stagger between each item's spawn in seconds. " +
                                  "0 = all spawn simultaneously.")]
        private float spawnStagger = 0.05f;

        // ── Float phase ───────────────────────────────────────────────────────

        [Header("Float Phase")]
        [SerializeField, Min(0f), Tooltip("How long items float before flying. Seconds.")]
        private float floatDuration = 0.6f;

        [SerializeField, Tooltip("How far items float up from their spawn position.")]
        private float floatAmplitude = 30f;

        [SerializeField, Tooltip("Ease applied to the float-up movement.")]
        private Ease floatEase = Ease.OutQuad;

        [SerializeField, Tooltip("Items rotate while floating. Degrees per second.")]
        private float rotationSpeed = 120f;

        [SerializeField, Range(0f, 1f),
         Tooltip("Randomises each item's rotation direction and speed.\n" +
                 "0 = all items rotate identically.\n" +
                 "1 = fully random direction and speed per item.")]
        private float rotationRandomness = 0.5f;

        [SerializeField, Tooltip("Scale items from 0 on spawn for a pop-in feel.")]
        private bool popInOnSpawn = true;

        [SerializeField, Tooltip("Duration of the pop-in scale animation.")]
        private float popInDuration = 0.2f;

        // ── Fly phase ─────────────────────────────────────────────────────────

        [Header("Fly Phase")]
        [SerializeField, Tooltip("Where items fly to. Required.")]
        private Transform destination;

        [SerializeField, Min(0.1f), Tooltip("How long the fly-to animation takes. Seconds.")]
        private float flyDuration = 0.5f;

        [SerializeField, Tooltip("Ease applied to the fly movement.")]
        private Ease flyEase = Ease.InBack;

        [SerializeField, Tooltip("Items scale to this value as they arrive at the destination. " +
                                  "0 = they shrink to nothing on arrival (clean disappear). " +
                                  "1 = arrive at full size.")]
        private float flyArrivalScale = 0f;

        [SerializeField, Tooltip("Stagger between each item starting its fly in seconds. " +
                                  "Creates a streaming / collecting feel rather than a simultaneous burst.")]
        private float flyStagger = 0.08f;

        // ── Events ────────────────────────────────────────────────────────────

        [Header("Events")]
        [SerializeField, Tooltip("Fired when every item has arrived at the destination.")]
        private UnityEvent onAllArrived;

        [SerializeField, Tooltip("Fired each time a single item arrives.")]
        private UnityEvent onItemArrived;

        // ── Pool ──────────────────────────────────────────────────────────────

        private readonly List<GameObject> _pool = new List<GameObject>();
        private Coroutine                 _playCoroutine;
        private int                       _arrivedCount;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()  => BuildPool();
        private void OnDestroy() => ClearPool();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Spawn and animate all items.</summary>
        public void Play()
        {
            if (itemPrefab == null)
            {
                Debug.LogWarning("[EffectRewardFly] No item prefab assigned.", this);
                return;
            }
            if (destination == null)
            {
                Debug.LogWarning("[EffectRewardFly] No destination assigned.", this);
                return;
            }

            if (_playCoroutine != null) StopCoroutine(_playCoroutine);
            _playCoroutine = StartCoroutine(PlayRoutine());
        }

        /// <summary>Kill all running tweens and return items to the pool immediately.</summary>
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

        // ── Pool management ───────────────────────────────────────────────────

        private void BuildPool()
        {
            if (itemPrefab == null) return;

            ClearPool();

            Transform parent = spawnParent != null ? spawnParent : transform.parent;

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
            foreach (var go in _pool)
                if (go != null) Destroy(go);
            _pool.Clear();
        }

        private GameObject GetPooledItem()
        {
            foreach (var go in _pool)
                if (go != null && !go.activeSelf) return go;

            // Pool exhausted — grow it by one
            if (itemPrefab != null)
            {
                Transform parent = spawnParent != null ? spawnParent : transform.parent;
                var go = Instantiate(itemPrefab, parent);
                go.SetActive(false);
                go.name = $"{itemPrefab.name}_pool_{_pool.Count}";
                _pool.Add(go);
                return go;
            }
            return null;
        }

        // ── Play routine ──────────────────────────────────────────────────────

        private IEnumerator PlayRoutine()
        {
            _arrivedCount = 0;
            Vector3 sourcePos = transform.position;

            for (int i = 0; i < itemCount; i++)
            {
                GameObject item = GetPooledItem();
                if (item == null) continue;

                // Spawn position: source + random offset within spawnRadius
                Vector2 randCircle = Random.insideUnitCircle * spawnRadius * spawnRandomness;
                Vector3 spawnPos   = sourcePos + new Vector3(randCircle.x, randCircle.y, 0f);

                item.transform.position   = spawnPos;
                item.transform.localScale = popInOnSpawn ? Vector3.zero : Vector3.one;
                item.transform.rotation   = Quaternion.identity;
                item.SetActive(true);

                AnimateItem(item, i);

                if (spawnStagger > 0f)
                    yield return new WaitForSeconds(spawnStagger);
            }

            _playCoroutine = null;
        }

        private void AnimateItem(GameObject item, int itemIndex)
        {
            Transform t        = item.transform;
            Vector3   spawnPos = t.position;
            Vector3   floatPos = spawnPos + new Vector3(0f, floatAmplitude, 0f);

            // Per-item rotation variation
            float rotDir   = Random.value > 0.5f ? 1f : -1f;
            float rotSpeed = rotationSpeed * (1f + (Random.value - 0.5f) * 2f * rotationRandomness);
            float totalRot = rotDir * rotSpeed * (floatDuration + flyDuration);

            Sequence seq = DOTween.Sequence();

            // ── Pop in ────────────────────────────────────────────────────────
            if (popInOnSpawn)
                seq.Append(t.DOScale(Vector3.one, popInDuration).SetEase(Ease.OutBack));

            // ── Float up ──────────────────────────────────────────────────────
            seq.Append(t.DOMove(floatPos, floatDuration).SetEase(floatEase));

            // ── Rotation during float + fly ───────────────────────────────────
            seq.Join(t.DORotate(new Vector3(0f, 0f, totalRot), floatDuration + flyDuration,
                RotateMode.FastBeyond360).SetEase(Ease.Linear));

            // ── Fly delay: stagger each item's departure ──────────────────────
            float flyDelay = itemIndex * flyStagger;
            seq.AppendInterval(flyDelay);

            // ── Fly to destination ────────────────────────────────────────────
            seq.Append(t.DOMove(destination.position, flyDuration).SetEase(flyEase));
            seq.Join(t.DOScale(Vector3.one * flyArrivalScale, flyDuration).SetEase(flyEase));

            // ── Arrival ───────────────────────────────────────────────────────
            seq.OnComplete(() =>
            {
                item.SetActive(false);
                onItemArrived?.Invoke();

                _arrivedCount++;
                if (_arrivedCount >= itemCount)
                    onAllArrived?.Invoke();
            });

            seq.SetLink(item, LinkBehaviour.KillOnDestroy);
            seq.Play();
        }
    }
}
