using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float maxLifetime = 5f; // 어떤 이유로든 못 맞히면 이 시간 후 반환
    public int damage;

    [Header("발사체 애니메이션")]
    [SerializeField] private Animator projectileAnimator;

    [Header("타격 이펙트 설정")]
    [SerializeField] private GameObject hitEffectPrefab;

    private IObjectPool<Projectile> managedPool;
    private Transform target;

    // 반환 경로가 두 개(코루틴, 트리거)라서 이중 Release 방지용.
    // SetTarget에서만 false로 풀리므로, 풀에서 꺼낸 직후~SetTarget 전에는 충돌도 무시됨.
    private bool isReleased;

    private void Awake()
    {
        if (projectileAnimator == null)
            projectileAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (projectileAnimator != null)
            projectileAnimator.Play(0, 0, 0f); // 현재(기본) 상태를 처음부터 재생
    }

    public void SetPool(IObjectPool<Projectile> pool)
    {
        managedPool = pool;
    }

    public void SetTarget(Transform enemyTarget, int newDamage)
    {
        target = enemyTarget;
        damage = newDamage;
        isReleased = false;

        StopAllCoroutines();
        StartCoroutine(MoveToTarget());
    }

    private IEnumerator MoveToTarget()
    {
        float elapsed = 0f;

        while (IsTargetValid() && elapsed < maxLifetime)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ReleaseToPool();
    }

    // 적이 Destroy됐거나(null) 풀로 반납돼 비활성화된 경우 모두 무효로 처리
    private bool IsTargetValid()
    {
        return target != null && target.gameObject.activeInHierarchy;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isReleased) return;
        if (!other.TryGetComponent(out Enemy enemy)) return;

        enemy.TakeDamage(damage);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(SFXType.Hit, true);

        HitEffectPoolManager.Instance.Spawn(hitEffectPrefab, transform.position);
        ReleaseToPool();
    }

    private void ReleaseToPool()
    {
        if (isReleased) return;
        isReleased = true;

        if (managedPool != null)
        {
            managedPool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}