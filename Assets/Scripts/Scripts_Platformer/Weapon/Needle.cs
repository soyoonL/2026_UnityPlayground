using System.Collections.Generic;
using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 던져지는 바늘 본체. 비행·착지·끌려옴 같은 물리와 데미지 판정을 담당한다.
    /// 언제 던지고 당길지는 NeedleWeapon이 정하고, 여기서는 명령만 수행한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(DistanceJoint2D))]
    public class Needle : MonoBehaviour
    {
        [Header("구성")]
        [Tooltip("실이 묶이는 지점 (바늘귀). 바늘 뒤쪽 끝에 둔 자식 오브젝트")]
        [SerializeField] private Transform ropeAnchor;
        [Tooltip("데미지 판정 콜라이더. 비우면 바늘 본체 콜라이더를 사용")]
        [SerializeField] private Collider2D hitBox;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("레이어")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private LayerMask groundLayer;

        [Header("회전")]
        [Tooltip("이 속도보다 느려지면 바닥에 눕는다")]
        [SerializeField, Min(0f)] private float restSpeed = 1f;
        [SerializeField, Min(0f)] private float rotateSharpness = 15f;

        private Rigidbody2D rb;
        private DistanceJoint2D joint;
        private PhysicsMaterial2D physicsMaterial;
        private NeedleData data;
        private Transform hand;

        private bool isActive;
        private bool isPulling;
        private bool hasLanded;

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
            joint = GetComponent<DistanceJoint2D>();
            if (hitBox == null) hitBox = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true; // 회전은 직접 계산 (진행 방향을 바라보게)
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // 실: 손(월드 좌표)과의 거리가 최대 길이를 넘지 않게만 제한
            joint.connectedBody = null;
            joint.autoConfigureConnectedAnchor = false;
            joint.autoConfigureDistance = false;
            joint.maxDistanceOnly = true;
            joint.enableCollision = false;
            if (ropeAnchor != null)
                joint.anchor = transform.InverseTransformPoint(ropeAnchor.position);
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
            joint.distance = data.MaxRopeLength;

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

            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            rb.position = position;
            rb.rotation = angle;
            rb.linearVelocity = velocity;
            rb.angularVelocity = 0f;

            joint.connectedAnchor = hand.position;
            joint.enabled = true;

            isActive = true;
            isPulling = false;
            hasLanded = false;
            nextHitTime.Clear();
            UpdateFlip(angle);
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
            joint.connectedAnchor = handPos; // 플레이어가 움직이면 실의 시작점도 따라감

            if (isPulling)
            {
                Vector2 target = (handPos - RopeAnchorPosition).normalized * data.PullSpeed;
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, data.PullAcceleration * Time.fixedDeltaTime);
            }

            UpdateRotation(handPos);
            DealDamage();
        }

        private void UpdateRotation(Vector2 handPos)
        {
            Vector2 fromHand = rb.position - handPos;
            Vector2 velocity = rb.linearVelocity;
            bool ropeTaut = fromHand.sqrMagnitude >= (data.MaxRopeLength - 0.05f) * (data.MaxRopeLength - 0.05f);

            float target;
            if (isPulling || ropeTaut)
                target = Mathf.Atan2(fromHand.y, fromHand.x) * Mathf.Rad2Deg;   // 실에 끌려갈 때: 바늘귀가 손 쪽
            else if (velocity.sqrMagnitude > restSpeed * restSpeed)
                target = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;   // 날아갈 때: 진행 방향
            else
                target = fromHand.x >= 0f ? 0f : 180f;                          // 멈추면 바닥에 눕기

            float angle = Mathf.LerpAngle(rb.rotation, target, 1f - Mathf.Exp(-rotateSharpness * Time.fixedDeltaTime));
            rb.rotation = angle;
            UpdateFlip(angle);
        }

        // 왼쪽을 향할 때 스프라이트가 뒤집혀 보이지 않도록 상하 반전
        private void UpdateFlip(float angle)
        {
            if (spriteRenderer != null)
                spriteRenderer.flipY = Mathf.Abs(Mathf.DeltaAngle(0f, angle)) > 90f;
        }

        private void DealDamage()
        {
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
    }
}
