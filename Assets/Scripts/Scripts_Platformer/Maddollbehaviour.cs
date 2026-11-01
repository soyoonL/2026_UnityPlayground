using UnityEngine;

namespace Plaformer
{

    /// <summary>
    /// 미치광이 인형: 플레이어를 감지하면 준비 동작 후 그 방향으로 돌진한다.
    /// 벽·낭떠러지를 만나거나, 최대 거리·시간에 도달하거나, 플레이어를 들이받으면 멈춘다.
    /// 쿨타임 동안은 천천히 걸어서 접근한다.
    /// 흐름: Idle/Chase → Windup → Attacking(돌진) → Recover → Idle
    /// </summary>
    [CreateAssetMenu(fileName = "MadDollBehaviour", menuName = "Game/Enemy/Behaviour/Mad Doll")]
    public class MadDollBehaviour : EnemyBehaviour
    {
        [Header("돌진 조건")]
        [Tooltip("플레이어와의 높이 차가 이 이하일 때만 돌진 (다른 층이면 무시)")]
        [SerializeField, Min(0f)] private float verticalTolerance = 1.5f;

        [Header("돌진")]
        [SerializeField, Min(0f)] private float windupTime = 0.6f;
        [SerializeField, Min(0f)] private float dashSpeed = 10f;
        [SerializeField, Min(0f)] private float maxDashDistance = 6f;
        [Tooltip("어딘가에 걸려 계속 돌진하는 상황 방지용")]
        [SerializeField, Min(0f)] private float maxDashTime = 1.2f;
        [SerializeField, Min(0f)] private float recoverTime = 1f;

        public override void Think(Enemy enemy)
        {
            switch (enemy.State)
            {
                case EnemyState.Idle:
                case EnemyState.Chase:
                    if (!enemy.TryGetPlayer(detectRange, out Vector2 playerPos))
                    {
                        if (enemy.State != EnemyState.Idle) enemy.ChangeState(EnemyState.Idle);
                        return;
                    }

                    enemy.Face(playerPos.x - enemy.Position.x);
                    bool sameLevel = Mathf.Abs(playerPos.y - enemy.Position.y) <= verticalTolerance;

                    if (sameLevel && enemy.CanAttack)
                    {
                        enemy.ChangeState(EnemyState.Windup);
                        enemy.PlayWindup();
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
                    if (enemy.StateTime >= recoverTime)
                        enemy.ChangeState(EnemyState.Idle);
                    break;

                    // Attacking(돌진)의 종료 판정은 물리와 맞추기 위해 Move에서 처리
            }
        }

        public override void Move(Enemy enemy)
        {
            switch (enemy.State)
            {
                case EnemyState.Chase:
                    bool canWalk = !enemy.IsBlockedAhead(enemy.Facing);
                    enemy.SetHorizontalVelocity(canWalk ? enemy.Facing * enemy.Data.MoveSpeed : 0f);
                    break;

                case EnemyState.Attacking:
                    float traveled = Mathf.Abs(enemy.Position.x - enemy.ActionOrigin.x);
                    // 빠르게 움직이므로 다음 물리 프레임 이동 거리만큼 미리 확인
                    bool blocked = enemy.IsBlockedAhead(enemy.Facing, dashSpeed * Time.fixedDeltaTime);

                    if (blocked || traveled >= maxDashDistance || enemy.StateTime >= maxDashTime)
                    {
                        StopDash(enemy);
                    }
                    else
                    {
                        enemy.SetHorizontalVelocity(enemy.Facing * dashSpeed);
                    }
                    break;

                default:
                    enemy.SetHorizontalVelocity(0f);
                    break;
            }
        }

        public override void Attack(Enemy enemy)
        {
            enemy.MarkActionOrigin();
            enemy.ChangeState(EnemyState.Attacking);
        }

        public override void OnContact(Enemy enemy, Collider2D other)
        {
            // 돌진 중에만, 한 번의 돌진에 한 번만 데미지
            if (enemy.State != EnemyState.Attacking || enemy.HasHitThisAttack) return;

            if (enemy.TryDamage(other))
                StopDash(enemy);
        }

        private static void StopDash(Enemy enemy)
        {
            enemy.SetHorizontalVelocity(0f);
            enemy.ChangeState(EnemyState.Recover);
        }

        public override void DrawGizmos(Enemy enemy)
        {
            base.DrawGizmos(enemy);
            Vector3 pos = enemy.transform.position;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(pos, pos + new Vector3(maxDashDistance * enemy.Facing, 0f, 0f));
        }
    }
}