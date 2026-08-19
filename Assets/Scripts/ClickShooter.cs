using UnityEngine;
using UnityEngine.Pool;

public class ClickShooter : MonoBehaviour
{
    [SerializeField] Projectile projectilePrefab;  // 발사체 프리팹
    [SerializeField] private Transform spawnPoint;   // 소환되는 위치
    [SerializeField] private Transform targetEnemy; // 적의 위치가 고정되어있는 상태이므로 인스펙터에서 끌어오는 형식으로 ㄱ

    private IObjectPool<Projectile> projectilePool; // 선언

    private void Awake()
    {
        projectilePool = new ObjectPool<Projectile>(
            createFunc: CreatProjectile,
            actionOnGet: OnGetProjectile,
            actionOnRelease: OnReleaseProjectile,
            actionOnDestroy: OnDestroyProjectile,
            collectionCheck: true,
            defaultCapacity: 10,
            maxSize: 50
            );
    }

    private Projectile CreatProjectile()
    {
        Projectile bullet = Instantiate(projectilePrefab);
        bullet.SetPool(projectilePool);
        return bullet;
    }

    private void OnGetProjectile(Projectile bullet)
    {
        bullet.gameObject.SetActive(true);
    }

    private void OnReleaseProjectile(Projectile bullet)
    {
        bullet.gameObject.SetActive(false);
    }

    private void OnDestroyProjectile(Projectile bullet)
    {
        Destroy(bullet.gameObject);
    }

    public void Shoot()
    {
        if(targetEnemy == null) return;

        Projectile bullet = projectilePool.Get();

        bullet.transform.position = spawnPoint.position;

        int currentDamage = GameManager.Instance.CurrentDamage;
        bullet.SetTarget(targetEnemy,currentDamage);
    }

}
