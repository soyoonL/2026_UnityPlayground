using UnityEngine;

namespace Platformer
{
  
    /// <summary>
    /// 실뭉치 슬라임: 플레이어를 감지하면 다가가서 근접 공격한다.
    /// 흐름: Idle → Chase → Windup → (타격) → Recover → Idle
    /// </summary>
    [CreateAssetMenu(fileName = "SlimeBehaviour", menuName = "Game/Enemy/Behaviour/Slime")]
    public class SlimeBehaviour : EnemyBehaviour
    {
        [Header("근접 공격")]
        [Tooltip("플레이어와의 가로 거리가 이 안이면 공격을 시작")]
        [SerializeField, Min(0f)] private float attackRange = 0.8f;
        [SerializeField, Min(0f)] private float windupTime = 0.3f;
        [SerializeField, Min(0f)] private float recoverTime = 0.4f;

        [Header("타격 판정")]
        [SerializeField] private Vector2 hitOffset = new Vector2(0.5f, 0f);
        [SerializeField, Min(0f)] private float hitRadius = 0.5f;

        [Header("추적")]
        [Tooltip("발견 후 이 거리 밖으로 벗어나면 추적 포기. 0이면 끝까지 추적")]
        [SerializeField, Min(0f)] private float loseRange = 0f;

        public override void Think(Enemy enemy)
        {
            switch (enemy.State)
            {
                case EnemyState.Idle:
                case EnemyState.Chase:
                    // 발견 전에는 감지 범위, 발견 후에는 포기 거리(0이면 무한)로 판정
                    float range = !enemy.IsAggro ? detectRange
                                : (loseRange > 0f ? loseRange : float.PositiveInfinity);

                    if (!enemy.TryGetPlayer(range, out Vector2 playerPos))
                    {
                        enemy.SetAggro(false);
                        if (enemy.State != EnemyState.Idle) enemy.ChangeState(EnemyState.Idle);
                        return;
                    }
                    enemy.SetAggro(true);

                    float dx = playerPos.x - enemy.Position.x;
                    enemy.Face(dx);

                    if (Mathf.Abs(dx) <= attackRange)
                    {
                        if (enemy.CanAttack)
                        {
                            enemy.ChangeState(EnemyState.Windup);
                            enemy.PlayWindup();
                        }
                        else if (enemy.State != EnemyState.Idle)
                        {
                            enemy.ChangeState(EnemyState.Idle); // 쿨타임 동안 제자리 대기
                        }
                    }
                    else if (enemy.State != EnemyState.Chase)
                    {
                        enemy.ChangeState(EnemyState.Chase);
                    }
                    break;

                case EnemyState.Windup:
                    if (enemy.StateTime >= windupTime)
                    {
                        if (enemy.CanAttack) enemy.Attack();
                        else enemy.ChangeState(EnemyState.Idle);
                    }
                    break;

                case EnemyState.Recover:
                    // 최소 경직 시간이 지나고, 공격 애니메이션도 끝나야 복귀
                    if (enemy.StateTime >= recoverTime && !enemy.IsAttackAnimationPlaying())
                        enemy.ChangeState(EnemyState.Idle);
                    break;
            }
        }

        public override void Move(Enemy enemy)
        {
            bool canWalk = enemy.State == EnemyState.Chase && !enemy.IsBlockedAhead(enemy.Facing);
            enemy.SetHorizontalVelocity(canWalk ? enemy.Facing * enemy.Data.MoveSpeed : 0f);
        }

        public override void Attack(Enemy enemy)
        {
            Vector2 center = enemy.Position + new Vector2(hitOffset.x * enemy.Facing, hitOffset.y);

            // 단일 결과를 반환하는 오버로드라 GC 할당이 없다
            Collider2D hit = Physics2D.OverlapCircle(center, hitRadius, enemy.PlayerLayer);
            if (hit != null) enemy.TryDamage(hit);

            enemy.ChangeState(EnemyState.Recover);
        }

        public override void DrawGizmos(Enemy enemy)
        {
            base.DrawGizmos(enemy);
            Vector2 pos = enemy.transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos + new Vector2(hitOffset.x * enemy.Facing, hitOffset.y), hitRadius);
        }
    }
}