using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 적의 이동·공격 방식을 정의하는 추상 클래스 (전략 패턴).
    ///
    /// 주의: 이 에셋은 같은 종류의 적 전부가 공유한다.
    /// 쿨타임, 현재 상태 같은 개체별 값을 이 클래스의 필드에 저장하면
    /// 모든 적이 그 값을 공유하는 버그가 생긴다.
    /// 여기에는 "설정값"만 두고, 상태는 전부 Enemy를 통해 읽고 쓴다.
    /// </summary>
    public abstract class EnemyBehaviour : ScriptableObject
    {
        [Header("감지")]
        [SerializeField, Min(0f)] protected float detectRange = 5f;

        [Header("공격")]
        [SerializeField, Min(0f)] protected float attackCooldown = 1.5f;

        public float DetectRange => detectRange;
        public float AttackCooldown => attackCooldown;

        /// <summary>적이 활성화될 때 한 번 호출</summary>
        public virtual void Initialize(Enemy enemy) { }

        /// <summary>Update에서 호출. 감지, 상태 전환 등 판단을 담당</summary>
        public abstract void Think(Enemy enemy);

        /// <summary>FixedUpdate에서 호출. 물리 이동을 담당</summary>
        public abstract void Move(Enemy enemy);

        /// <summary>실제 공격 실행. Enemy.Attack()을 통해서만 호출된다</summary>
        public abstract void Attack(Enemy enemy);

        /// <summary>플레이어와 부딪혔을 때 호출 (접촉 공격이 필요한 적만 재정의)</summary>
        public virtual void OnContact(Enemy enemy, Collider2D other) { }

        /// <summary>씬 뷰에서 적을 선택했을 때 범위 표시</summary>
        public virtual void DrawGizmos(Enemy enemy)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(enemy.transform.position, detectRange);
        }
    }
}