using System.Collections;
using UnityEngine;

public class Projectile : PoolableObject
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float maxLifetime = 5f; // 어떤 이유로든 못 맞히면 이 시간 후 반환
    public int damage;

    [Header("발사체 애니메이션")]
    [SerializeField] private Animator projectileAnimator;

    [Header("타격 이펙트 설정")]
    [SerializeField] private HitEffect hitEffectPrefab; // GameObject → HitEffect로 변경

    private Transform target;

    // 이중 반납 방지(isReleased)와 풀 주소(managedPool)는 PoolableObject가 처리

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

    public void SetTarget(Transform enemyTarget, int newDamage)
    {
        target = enemyTarget;
        damage = newDamage;

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

        ReturnToPool();
    }

    // 적이 Destroy됐거나(null) 풀로 반납돼 비활성화된 경우 모두 무효로 처리
    private bool IsTargetValid()
    {
        return target != null && target.gameObject.activeInHierarchy;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsReleased) return;
        if (!other.TryGetComponent(out Enemy enemy)) return;

        enemy.TakeDamage(damage);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(SFXType.Hit, true);

        PoolManager.Instance.Spawn(hitEffectPrefab, transform.position);
        ReturnToPool();
    }
}