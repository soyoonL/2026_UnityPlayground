using System;
using UnityEngine;

namespace Plaformer
{
    /// <summary>적의 현재 행동 상태</summary>
    public enum EnemyState
    {
        Idle,       // 대기
        Chase,      // 추적·접근
        Windup,     // 공격 준비 동작
        Attacking,  // 지속형 공격 중 (돌진 등)
        Recover,    // 공격 후 경직
        Hurt,       // 피격 경직
        Dead        // 사망
    }

    /// <summary>
    /// 모든 적이 공유하는 단일 클래스.
    /// 데이터는 EnemyData, 이동·공격은 EnemyBehaviour에 맡기고
    /// 개체별 런타임 상태(HP, 쿨타임, 상태 등)는 전부 여기서 관리한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Enemy : MonoBehaviour, IDamageable, IAttackable
    {
        /// <summary>적이 죽었을 때 발생. 골드 지급 등은 구독하는 쪽이 처리한다.</summary>
        public static event Action<Enemy> OnEnemyDied;

        [Header("데이터")]
        [SerializeField] private EnemyData data;

        [Header("레이어")]
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask groundLayer;

        [Header("지형 감지")]
        [SerializeField, Min(0f)] private float wallCheckDistance = 0.1f;
        [SerializeField, Min(0f)] private float ledgeCheckDistance = 0.5f;

        [Header("사망")]
        [Tooltip("사망 후 오브젝트를 비활성화하기까지의 시간 (사망 애니메이션 길이)")]
        [SerializeField, Min(0f)] private float disableDelay = 1f;

        [Header("런타임 상태 (확인용)")]
        [SerializeField] private int currentHP;
        [SerializeField] private EnemyState state;

        // 캐싱한 컴포넌트
        private Rigidbody2D rb;
        private Collider2D col;
        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private EnemyBehaviour behaviour;

        // 개체별 런타임 값
        private float stateTimer;
        private float nextAttackTime;
        private float hurtEndTime;
        private float deathTime;
        private int facing = 1;
        private Vector2 spawnPosition;
        private Vector2 actionOrigin;
        private float randomSeed;
        private bool hasHitThisAttack;

        // 애니메이터 파라미터 (문자열 대신 해시 사용)
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int WindupHash = Animator.StringToHash("Windup");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DieHash = Animator.StringToHash("Die");
        private bool hasSpeedParam, hasWindupParam, hasAttackParam, hasHurtParam, hasDieParam;

        // 플레이어 참조 캐시 (모든 적이 공유, 한 번만 찾는다)
        private static Transform s_player;
        private static float s_nextPlayerSearchTime;
        private const float PlayerSearchInterval = 1f;

        // Enter Play Mode에서 도메인 리로드를 꺼도 static 값이 남지 않도록 초기화
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            OnEnemyDied = null;
            s_player = null;
            s_nextPlayerSearchTime = 0f;
        }

        #region 프로퍼티

        public EnemyData Data => data;
        public Rigidbody2D Rigidbody => rb;
        public EnemyState State => state;
        public float StateTime => stateTimer;
        public int Facing => facing;
        public Vector2 Position => rb.position;
        public Vector2 SpawnPosition => spawnPosition;
        public Vector2 ActionOrigin => actionOrigin;
        public float RandomSeed => randomSeed;
        public bool HasHitThisAttack => hasHitThisAttack;
        public LayerMask PlayerLayer => playerLayer;

        // IDamageable
        public bool IsDead => state == EnemyState.Dead;

        // IAttackable
        public int AttackDamage => data.Damage;
        public bool CanAttack => !IsDead && state != EnemyState.Hurt && Time.time >= nextAttackTime;

        #endregion

        #region 생명주기

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();
        }

        // 나중에 적을 풀링해서 재사용해도 초기화되도록 OnEnable에서 설정
        private void OnEnable()
        {
            if (data == null)
            {
                Debug.LogError($"{name}: EnemyData가 비어 있습니다.", this);
                enabled = false;
                return;
            }

            behaviour = data.Behaviour;
            if (behaviour == null)
                Debug.LogWarning($"{name}: EnemyData에 Behaviour가 연결되지 않았습니다.", this);

            currentHP = data.MaxHP;
            spawnPosition = transform.position;
            randomSeed = UnityEngine.Random.value * 100f;
            nextAttackTime = 0f;
            hasHitThisAttack = false;
            rb.simulated = true;

            if (spriteRenderer != null && data.Sprite != null)
                spriteRenderer.sprite = data.Sprite;

            SetupAnimator();
            ChangeState(EnemyState.Idle);
            behaviour?.Initialize(this);
        }

        private void Update()
        {
            if (state == EnemyState.Dead)
            {
                if (Time.time >= deathTime + disableDelay)
                    gameObject.SetActive(false);
                return;
            }

            stateTimer += Time.deltaTime;

            if (state == EnemyState.Hurt)
            {
                if (Time.time >= hurtEndTime)
                    ChangeState(EnemyState.Idle);
                return;
            }

            behaviour?.Think(this);

            if (hasSpeedParam)
                animator.SetFloat(SpeedHash, Mathf.Abs(rb.linearVelocity.x));
        }

        private void FixedUpdate()
        {
            if (state == EnemyState.Dead || state == EnemyState.Hurt) return;
            behaviour?.Move(this);
        }

        #endregion

        #region IDamageable / IAttackable

        public void TakeDamage(int damage, Vector2 hitDirection)
        {
            if (IsDead || damage <= 0) return;

            currentHP = Mathf.Max(currentHP - damage, 0);
            if (currentHP == 0)
            {
                Die();
                return;
            }

            SetTrigger(HurtHash, hasHurtParam);

            // 경직 시간이 0이면 슈퍼아머: 하던 행동을 계속한다
            if (data.HurtStunTime <= 0f) return;

            hurtEndTime = Time.time + data.HurtStunTime;
            ChangeState(EnemyState.Hurt);

            if (data.KnockbackForce > 0f)
            {
                Vector2 dir = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : new Vector2(-facing, 0f);
                rb.linearVelocity = dir * data.KnockbackForce;
            }
        }

        public void Attack()
        {
            if (!CanAttack || behaviour == null) return;

            hasHitThisAttack = false;
            nextAttackTime = Time.time + behaviour.AttackCooldown;
            SetTrigger(AttackHash, hasAttackParam);
            behaviour.Attack(this);
        }

        private void Die()
        {
            ChangeState(EnemyState.Dead);
            deathTime = Time.time;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // 물리와 충돌을 한 번에 끈다
            SetTrigger(DieHash, hasDieParam);
            OnEnemyDied?.Invoke(this);
        }

        #endregion

        #region Behaviour에서 쓰는 기능

        public void ChangeState(EnemyState newState)
        {
            state = newState;
            stateTimer = 0f;
        }

        public void SetVelocity(Vector2 velocity) => rb.linearVelocity = velocity;

        public void SetHorizontalVelocity(float x) => rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);

        /// <summary>x 방향 부호에 맞춰 바라보는 방향을 바꾼다 (원본 스프라이트가 오른쪽을 본다고 가정)</summary>
        public void Face(float directionX)
        {
            if (Mathf.Abs(directionX) < 0.01f) return;
            facing = directionX > 0f ? 1 : -1;
            if (spriteRenderer != null) spriteRenderer.flipX = facing < 0;
        }

        /// <summary>돌진 시작 위치처럼 행동의 기준점을 기록</summary>
        public void MarkActionOrigin() => actionOrigin = rb.position;

        public void PlayWindup() => SetTrigger(WindupHash, hasWindupParam);

        /// <summary>범위 안에 플레이어가 있으면 위치를 돌려준다 (물리 쿼리 없이 거리만 비교)</summary>
        public bool TryGetPlayer(float range, out Vector2 playerPosition)
        {
            playerPosition = default;
            Transform player = FindPlayer();
            if (player == null || !player.gameObject.activeInHierarchy) return false;

            playerPosition = player.position;
            return (playerPosition - rb.position).sqrMagnitude <= range * range;
        }

        /// <summary>대상 콜라이더에 이 적의 공격력으로 데미지를 준다</summary>
        public bool TryDamage(Collider2D target)
        {
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead) return false;

            Vector2 dir = ((Vector2)target.transform.position - rb.position).normalized;
            damageable.TakeDamage(AttackDamage, dir);
            hasHitThisAttack = true;
            return true;
        }

        /// <summary>진행 방향에 벽이 있거나 발 앞이 낭떠러지면 true</summary>
        /// <param name="lookAhead">빠르게 움직일 때 미리 더 앞을 확인할 거리</param>
        public bool IsBlockedAhead(int direction, float lookAhead = 0f)
        {
            Bounds b = col.bounds;
            float frontX = direction > 0 ? b.max.x : b.min.x;

            // 벽: 몸 중앙 높이에서 앞으로
            Vector2 wallOrigin = new Vector2(frontX, b.center.y);
            if (Physics2D.Raycast(wallOrigin, new Vector2(direction, 0f), wallCheckDistance + lookAhead, groundLayer))
                return true;

            // 낭떠러지: 발 앞에서 아래로
            Vector2 ledgeOrigin = new Vector2(frontX + direction * (0.05f + lookAhead), b.min.y + 0.05f);
            return !Physics2D.Raycast(ledgeOrigin, Vector2.down, ledgeCheckDistance, groundLayer);
        }

        #endregion

        #region 내부 처리

        private static Transform FindPlayer()
        {
            if (s_player != null) return s_player;

            // 플레이어가 없을 때 매 프레임 검색하지 않도록 간격을 둔다
            if (Time.time < s_nextPlayerSearchTime) return null;
            s_nextPlayerSearchTime = Time.time + PlayerSearchInterval;

            GameObject go = GameObject.FindWithTag("Player");
            s_player = go != null ? go.transform : null;
            return s_player;
        }

        private void SetupAnimator()
        {
            hasSpeedParam = hasWindupParam = hasAttackParam = hasHurtParam = hasDieParam = false;
            if (animator == null) return;

            if (data.AnimatorController != null)
                animator.runtimeAnimatorController = data.AnimatorController;
            if (animator.runtimeAnimatorController == null) return;

            // 컨트롤러에 없는 파라미터를 호출하면 경고가 쌓이므로 미리 확인해 둔다
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.nameHash == SpeedHash) hasSpeedParam = true;
                else if (p.nameHash == WindupHash) hasWindupParam = true;
                else if (p.nameHash == AttackHash) hasAttackParam = true;
                else if (p.nameHash == HurtHash) hasHurtParam = true;
                else if (p.nameHash == DieHash) hasDieParam = true;
            }
        }

        private void SetTrigger(int hash, bool exists)
        {
            if (exists) animator.SetTrigger(hash);
        }

        private void OnCollisionEnter2D(Collision2D collision) => HandleContact(collision.collider);
        private void OnCollisionStay2D(Collision2D collision) => HandleContact(collision.collider);
        private void OnTriggerEnter2D(Collider2D other) => HandleContact(other);

        private void HandleContact(Collider2D other)
        {
            if (IsDead || behaviour == null) return;
            if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;
            behaviour.OnContact(this, other);
        }

        private void OnDrawGizmosSelected()
        {
            if (data != null && data.Behaviour != null)
                data.Behaviour.DrawGizmos(this);
        }

        #endregion
    }
}