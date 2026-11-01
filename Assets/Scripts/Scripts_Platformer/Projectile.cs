using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Plaformer
{
  
    /// <summary>
    /// 직선으로 날아가는 투사체 (시침핀 등).
    /// targetLayer에 닿으면 데미지를 주고, obstacleLayer에 닿거나 수명이 끝나면 풀로 돌아간다.
    /// 원본 스프라이트는 오른쪽을 향하도록 만들 것 (진행 방향으로 회전함).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("충돌")]
        [Tooltip("데미지를 줄 대상 (Player)")]
        [SerializeField] private LayerMask targetLayer;
        [Tooltip("닿으면 사라질 지형 (Ground)")]
        [SerializeField] private LayerMask obstacleLayer;

        [Header("수명")]
        [SerializeField, Min(0.1f)] private float lifeTime = 3f;

        private Rigidbody2D rb;
        private IObjectPool<Projectile> pool;
        private int damage;
        private float despawnTime;
        private bool isActive;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 빠른 투사체가 벽을 뚫지 않도록
            GetComponent<Collider2D>().isTrigger = true;
        }

        internal void SetPool(IObjectPool<Projectile> owner) => pool = owner;

        public void Launch(Vector2 position, Vector2 direction, float speed, int damageAmount)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            rb.position = position;
            rb.rotation = angle;
            rb.linearVelocity = direction * speed;

            damage = damageAmount;
            despawnTime = Time.time + lifeTime;
            isActive = true;
        }

        private void Update()
        {
            if (isActive && Time.time >= despawnTime)
                Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            int layerBit = 1 << other.gameObject.layer;

            if ((targetLayer.value & layerBit) != 0)
            {
                IDamageable damageable = other.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                    damageable.TakeDamage(damage, rb.linearVelocity.normalized);
                Despawn();
            }
            else if ((obstacleLayer.value & layerBit) != 0)
            {
                Despawn();
            }
        }

        private void Despawn()
        {
            if (!isActive) return; // 같은 프레임에 두 번 반납되는 것 방지
            isActive = false;
            rb.linearVelocity = Vector2.zero;

            if (pool != null) pool.Release(this);
            else Destroy(gameObject);
        }
    }

    /// <summary>
    /// 투사체 프리팹별 오브젝트 풀. Instantiate/Destroy 반복을 막는다.
    /// 씬이 바뀌어도 풀이 깨지지 않도록 DontDestroyOnLoad 오브젝트 아래에 보관한다.
    /// </summary>
    public static class ProjectilePool
    {
        private static readonly Dictionary<Projectile, ObjectPool<Projectile>> pools = new Dictionary<Projectile, ObjectPool<Projectile>>();
        private static Transform root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            pools.Clear();
            root = null;
        }

        public static void Spawn(Projectile prefab, Vector2 position, Vector2 direction, float speed, int damage)
        {
            if (!pools.TryGetValue(prefab, out ObjectPool<Projectile> pool))
            {
                pool = CreatePool(prefab);
                pools.Add(prefab, pool);
            }

            pool.Get().Launch(position, direction, speed, damage);
        }

        private static ObjectPool<Projectile> CreatePool(Projectile prefab)
        {
            if (root == null)
            {
                root = new GameObject("[ProjectilePool]").transform;
                Object.DontDestroyOnLoad(root.gameObject);
            }

            ObjectPool<Projectile> pool = null;
            pool = new ObjectPool<Projectile>(
                createFunc: () =>
                {
                    Projectile p = Object.Instantiate(prefab, root);
                    p.SetPool(pool);
                    return p;
                },
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => { if (p != null) Object.Destroy(p.gameObject); },
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50);

            return pool;
        }
    }
}