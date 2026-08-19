using UnityEngine;
using UnityEngine.Pool;

public class ClickShooter : MonoBehaviour
{
    [SerializeField] Projectile projectilePrefab;  // 발사체 프리팹
    [SerializeField] private Transform spawnPoint;   // 소환되는 위치
    [SerializeField] private Transform targetEnemy; // 적의 위치가 고정되어있는 상태이므로 인스펙터에서 끌어오는 형식으로 ㄱ

    private IObjectPool<Projectile> projectilePool; // 발사체를 관라힐 객체 풀 변수 선언

    private void Awake()
    {
        // 유니티 내장 객체 풀 초기화
        projectilePool = new ObjectPool<Projectile>(
            createFunc: CreatProjectile,
            actionOnGet: OnGetProjectile,
            actionOnRelease: OnReleaseProjectile,
            actionOnDestroy: OnDestroyProjectile,
            collectionCheck: true, // 중복 반환 검사 (실수로 코드 작성을 잘못해서 같은 오브젝트를 두 번 반환하는 버그 발생 시 콘솔 창에 에러를 띄워 알려줌)
            defaultCapacity: 10, // 기본 생성 수량 (pool안의 내부 배열의 크기를 미리 할당, 미리 할당 안해두면 메모리를 재할당하는 과정에서 렉 발생
            maxSize: 50 // 내부 배열에 보관할 최대 수량
            );
    }

    /// <summary> 풀 내부 배열에 재사용 가능한 발사체가 부족할 때 호출되어 새 발사체를 생성, pool이 비어있는 경우 Get() 실행 시 CreateProjectile() 먼저 호출 </summary>
    private Projectile CreatProjectile()
    {
        Projectile bullet = Instantiate(projectilePrefab);
        bullet.SetPool(projectilePool);
        return bullet;
    }

    /// <summary> Get() 호출 시 실행되는 함수로, 발사체 오브젝트를 활성화  </summary>
    private void OnGetProjectile(Projectile bullet)
    {
        bullet.gameObject.SetActive(true);
    }

    /// <summary> Release() 호출 시 실행되는 함수로, 발사체 오브젝트를 비활성화, 처음 비활성화된 오브젝트는 pool 내부 배열 안에 저장됨 </summary>
    private void OnReleaseProjectile(Projectile bullet)
    {
        bullet.gameObject.SetActive(false);
    }

    /// <summary> maxSize 초과 같이 오브젝트 완전 파괴가 필요할 때 실행 </summary>
    private void OnDestroyProjectile(Projectile bullet)
    {
        Destroy(bullet.gameObject);
    }

    /// <summary> 캐릭터 클릭 시 발사체를 발사 </summary>
    public void Shoot()
    {
        if(targetEnemy == null) return; // 적이 없을 경우 반환

        Projectile bullet = projectilePool.Get(); 

        bullet.transform.position = spawnPoint.position; // 발사체가 생성되는 위치

        int currentDamage = GameManager.Instance.CurrentDamage; // 발사체의 데미지
        bullet.SetTarget(targetEnemy,currentDamage); 
    }

}
