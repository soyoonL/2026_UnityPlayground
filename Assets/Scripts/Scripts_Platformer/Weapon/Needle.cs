using System.Collections.Generic;
using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 던져지는 바늘 본체.
    /// 물리 몸통(회전하지 않는 작은 원)과 그림(Visual, 진행 방향으로 회전)을 분리해서,
    /// 회전이 땅을 밀어 바늘이 튕겨 오르는 문제를 막는다.
    /// 언제 던지고 당길지는 NeedleWeapon이 정하고, 여기서는 명령만 수행한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Needle : MonoBehaviour
    {
        [Header("구성")]
        [Tooltip("바늘 그림, 바늘귀, 데미지 판정을 담은 자식 오브젝트 (이것만 회전함)")]
        [SerializeField] private Transform visual;
        [Tooltip("실이 묶이는 지점 (Visual의 자식, 바늘 뒤쪽 끝)")]
        [SerializeField] private Transform ropeAnchor;
        [Tooltip("데미지 판정 콜라이더 (Visual에 붙인 Is Trigger 콜라이더)")]
        [SerializeField] private Collider2D hitBox;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("레이어")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private LayerMask groundLayer;

        [Header("회전 (그림만)")]
        [Tooltip("이 속도보다 느려지면 바닥에 눕는다")]
        [SerializeField, Min(0f)] private float restSpeed = 1f;
        [SerializeField, Min(0f)] private float rotateSharpness = 15f;

        private Rigidbody2D rb;
        private PhysicsMaterial2D physicsMaterial;
        private NeedleData data;
        private Transform hand;

        private bool isActive;
        private bool isPulling;
        private bool hasLanded;
        private float targetAngle;
        private float currentAngle;

        // 데미지 판정용 (미리 할당해서 재사용)
        private ContactFilter2D enemyFilter;
        private readonly List<Collider2D> overlapResults = new List<Collider2D>(8);
        private readonly Dictionary<Collider2D, IDamageable> damageableCache = new Dictionary<Collider2D, IDamageable>();
        private readonly Dictionary<IDamageable, float> nextHitTime = new Dictionary<IDamageable, float>();

        public bool IsActive => isActive;
        public bool IsPulling => isPulling;
        public Vector2 RopeAnchorPosition => ropeAnchor != null ? (Vector2)ropeAnchor.position : (Vector2)transform.position;

        #region 초기화

        private void Awake() => CacheComponents();

        // 다른 오브젝트의 Awake보다 먼저 불려도 동작하도록 지연 초기화
        private void CacheComponents()
        {
            if (rb != null) return;

            rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (visual == null && spriteRenderer != null) visual = spriteRenderer.transform;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true; // 몸통은 절대 회전하지 않음 (그림만 회전)
            rb.rotation = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // 실 길이는 코드로 제한하므로 남아 있는 Joint는 꺼 둔다
            foreach (Joint2D j in GetComponents<Joint2D>()) j.enabled = false;

            if (visual == transform)
                Debug.LogWarning($"{name}: 바늘 그림(SpriteRenderer)을 자식 오브젝트 Visual로 옮겨주세요. 루트에 있으면 회전 문제가 다시 생깁니다.", this);
            if (hitBox != null && !hitBox.isTrigger)
                Debug.LogWarning($"{name}: Hit Box 콜라이더의 Is Trigger를 켜주세요. 꺼져 있으면 바닥에 걸려 튕깁니다.", this);
        }

        public void Initialize(NeedleData needleData, Transform handPoint)
        {
            CacheComponents();
            data = needleData;
            hand = handPoint;

            rb.gravityScale = data.GravityScale;
            rb.mass = data.Mass;
            rb.linearDamping = data.LinearDamping;

            physicsMaterial = new PhysicsMaterial2D("NeedleHeavy")
            {
                bounciness = data.Bounciness,
                friction = data.Friction
            };
            rb.sharedMaterial = physicsMaterial;

            enemyFilter = new ContactFilter2D();
            enemyFilter.SetLayerMask(enemyLayer);
            enemyFilter.useTriggers = false;

            Hide();
        }

        private void OnDestroy()
        {
            if (physicsMaterial != null) Destroy(physicsMaterial);
        }

        #endregion

        #region NeedleWeapon이 부르는 명령

        public void Launch(Vector2 position, Vector2 velocity)
        {
            gameObject.SetActive(true);

            transform.SetPositionAndRotation(position, Quaternion.identity);
            rb.position = position;
            rb.rotation = 0f;
            rb.linearVelocity = velocity;
            rb.angularVelocity = 0f;

            currentAngle = targetAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            ApplyVisualRotation();

            isActive = true;
            isPulling = false;
            hasLanded = false;
            nextHitTime.Clear();
        }

        public void SetPulling(bool pulling)
        {
            isPulling = pulling;
            if (!pulling) hasLanded = false; // 당기기를 멈추면 다시 떨어질 때 '쿵' 처리
        }

        public void Hide()
        {
            isActive = false;
            isPulling = false;
            gameObject.SetActive(false);
        }

        #endregion

        #region 물리 처리

        private void FixedUpdate()
        {
            if (!isActive || data == null) return;

            Vector2 handPos = hand.position;

            if (isPulling)
            {
                Vector2 target = (handPos - rb.position).normalized * data.PullSpeed;
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, data.PullAcceleration * Time.fixedDeltaTime);
            }

            ApplyRopeLimit(handPos);
            targetAngle = ComputeTargetAngle(handPos);
            DealDamage();
        }

        /// <summary>
        /// 실 길이 제한. 손에서 실 길이보다 멀어지면 바깥으로 나가는 속도를 없애고
        /// 넘어선 만큼 안쪽으로 당긴다. 플레이어가 멀어지면 바늘이 이 힘으로 끌려온다.
        /// </summary>
        private void ApplyRopeLimit(Vector2 handPos)
        {
            Vector2 offset = rb.position - handPos;
            float max = data.MaxRopeLength;
            float sqrDist = offset.sqrMagnitude;
            if (sqrDist <= max * max) return;

            float dist = Mathf.Sqrt(sqrDist);
            Vector2 outward = offset / dist;
            Vector2 v = rb.linearVelocity;

            float outwardSpeed = Vector2.Dot(v, outward);
            if (outwardSpeed > 0f) v -= outward * outwardSpeed;

            // 넘어선 거리를 몇 스텝에 걸쳐 부드럽게 되돌림 (한 번에 되돌리면 튐)
            v -= outward * ((dist - max) / Time.fixedDeltaTime * 0.3f);
            rb.linearVelocity = v;
        }

        // 그림이 바라볼 각도 계산 (물리에는 영향 없음)
        private float ComputeTargetAngle(Vector2 handPos)
        {
            Vector2 fromHand = rb.position - handPos;
            Vector2 velocity = rb.linearVelocity;
            float taut = data.MaxRopeLength - 0.05f;

            if (isPulling || fromHand.sqrMagnitude >= taut * taut)
                return Mathf.Atan2(fromHand.y, fromHand.x) * Mathf.Rad2Deg;   // 실에 끌려갈 때: 바늘귀가 손 쪽

            if (velocity.sqrMagnitude > restSpeed * restSpeed)
                return Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;   // 날아갈 때: 진행 방향

            // 멈추면 지금 각도에서 가까운 쪽으로 눕기 (플레이어가 지나가도 뒤집히지 않음)
            return Mathf.Abs(Mathf.DeltaAngle(currentAngle, 0f)) <= 90f ? 0f : 180f;
        }

        private void DealDamage()
        {
            if (hitBox == null) return;

            int count = hitBox.Overlap(enemyFilter, overlapResults);
            if (count == 0) return;

            float now = Time.time;
            for (int i = 0; i < count; i++)
            {
                Collider2D col = overlapResults[i];

                // GetComponentInParent는 처음 한 번만 (없으면 null도 캐싱)
                if (!damageableCache.TryGetValue(col, out IDamageable target))
                {
                    target = col.GetComponentInParent<IDamageable>();
                    damageableCache[col] = target;
                }

                if (target == null || target.IsDead) continue;
                if (nextHitTime.TryGetValue(target, out float next) && now < next) continue;
                nextHitTime[target] = now + data.HitInterval;

                // 움직이는 중이면 진행 방향, 멈춰 있으면 바늘에서 적 쪽으로 밀어냄
                Vector2 dir = rb.linearVelocity.sqrMagnitude > 1f
                    ? rb.linearVelocity.normalized
                    : ((Vector2)col.bounds.center - rb.position).normalized;

                target.TakeDamage(data.Damage, dir);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!isActive || isPulling || hasLanded) return;
            if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0) return;

            // 무거운 바늘: 처음 부딪힐 때 속도를 크게 잃어서 튕기지 않고 털썩 떨어짐
            hasLanded = true;
            rb.linearVelocity *= data.ImpactSpeedKeep;
        }

        #endregion

        #region 그림 회전

        // 화면 프레임마다 부드럽게 회전 (보간된 위치와 맞춰 자연스럽게 보이도록)
        private void Update()
        {
            if (!isActive) return;
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, 1f - Mathf.Exp(-rotateSharpness * Time.deltaTime));
            ApplyVisualRotation();
        }

        private void ApplyVisualRotation()
        {
            if (visual != null && visual != transform)
                visual.rotation = Quaternion.Euler(0f, 0f, currentAngle);

            // 왼쪽을 향할 때 그림이 뒤집혀 보이지 않도록 상하 반전
            if (spriteRenderer != null)
                spriteRenderer.flipY = Mathf.Abs(Mathf.DeltaAngle(0f, currentAngle)) > 90f;
        }

        #endregion
    }
}