using UnityEngine;

namespace Plaformer
{
  
    /// <summary>
    /// 천 조각 벌레: 스폰 위치 주변을 8자로 날아다니며,
    /// 플레이어가 감지 범위 안에 있으면 일정 간격으로 시침핀을 발사한다.
    /// 흐름: Idle → Windup(조준) → (발사) → Recover → Idle
    /// </summary>
    [CreateAssetMenu(fileName = "ClothBugBehaviour", menuName = "Game/Enemy/Behaviour/Cloth Bug")]
    public class ClothBugBehaviour : EnemyBehaviour
    {
        [Header("비행")]
        [Tooltip("스폰 위치 기준 비행 반경 (가로, 세로)")]
        [SerializeField] private Vector2 hoverRadius = new Vector2(2f, 0.6f);
        [Tooltip("1초에 몇 바퀴 도는지")]
        [SerializeField, Min(0f)] private float hoverFrequency = 0.25f;
        [Tooltip("목표 지점을 따라가는 민첩함")]
        [SerializeField, Min(0f)] private float followSharpness = 4f;

        [Header("시침핀 발사")]
        [SerializeField] private Projectile pinPrefab;
        [SerializeField, Min(0f)] private float aimTime = 0.3f;
        [SerializeField, Min(0f)] private float projectileSpeed = 8f;
        [SerializeField] private Vector2 fireOffset = new Vector2(0.3f, 0f);
        [SerializeField, Min(0f)] private float recoverTime = 0.2f;

        public override void Initialize(Enemy enemy)
        {
            enemy.Rigidbody.gravityScale = 0f; // 비행형
        }

        public override void Think(Enemy enemy)
        {
            switch (enemy.State)
            {
                case EnemyState.Idle:
                case EnemyState.Chase:
                    if (enemy.TryGetPlayer(detectRange, out Vector2 playerPos))
                    {
                        enemy.Face(playerPos.x - enemy.Position.x);
                        if (enemy.CanAttack)
                        {
                            enemy.ChangeState(EnemyState.Windup);
                            enemy.PlayWindup();
                        }
                    }
                    else
                    {
                        enemy.Face(enemy.Rigidbody.linearVelocity.x); // 날아가는 방향을 바라봄
                    }
                    break;

                case EnemyState.Windup:
                    if (enemy.StateTime >= aimTime)
                    {
                        if (enemy.CanAttack) enemy.Attack();
                        else enemy.ChangeState(EnemyState.Idle);
                    }
                    break;

                case EnemyState.Recover:
                    if (enemy.StateTime >= recoverTime)
                        enemy.ChangeState(EnemyState.Idle);
                    break;
            }
        }

        public override void Move(Enemy enemy)
        {
            // 개체마다 RandomSeed가 달라서 여러 마리가 똑같이 움직이지 않는다
            float t = Time.time * hoverFrequency * Mathf.PI * 2f + enemy.RandomSeed;
            Vector2 target = enemy.SpawnPosition + new Vector2(Mathf.Cos(t) * hoverRadius.x, Mathf.Sin(t * 2f) * hoverRadius.y);

            Vector2 velocity = (target - enemy.Position) * followSharpness;
            enemy.SetVelocity(Vector2.ClampMagnitude(velocity, enemy.Data.MoveSpeed));
        }

        public override void Attack(Enemy enemy)
        {
            if (pinPrefab != null)
            {
                Vector2 origin = enemy.Position + new Vector2(fireOffset.x * enemy.Facing, fireOffset.y);

                // 조준하는 동안 플레이어가 범위를 벗어났어도 마지막으로 그 방향을 향해 쏜다
                Vector2 dir = enemy.TryGetPlayer(float.PositiveInfinity, out Vector2 playerPos)
                    ? (playerPos - origin).normalized
                    : new Vector2(enemy.Facing, 0f);

                ProjectilePool.Spawn(pinPrefab, origin, dir, projectileSpeed, enemy.AttackDamage);
            }

            enemy.ChangeState(EnemyState.Recover);
        }

        public override void DrawGizmos(Enemy enemy)
        {
            base.DrawGizmos(enemy);
            Gizmos.color = Color.cyan;
            Vector3 center = Application.isPlaying ? (Vector3)enemy.SpawnPosition : enemy.transform.position;
            Gizmos.DrawWireCube(center, new Vector3(hoverRadius.x * 2f, hoverRadius.y * 2f, 0f));
        }
    }
}
