using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ClickShooter : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;   // 소환되는 위치
    [SerializeField] private Transform targetEnemy; // 적의 위치가 고정되어있는 상태이므로 인스펙터에서 끌어오는 형식으로 ㄱ
    [SerializeField] private Transform poolContainer;

    private Dictionary<Projectile,IObjectPool<Projectile>> poolDictionary = new Dictionary<Projectile, IObjectPool<Projectile>>();

    /// <summary> 발사체를 묶어둘 부모 Transform 가져오기 </summary>
    private Transform GetContainer()
    {
        if(poolContainer == null)
        {
            GameObject containerObj = new GameObject("ProjectilePool");
            poolContainer = containerObj.transform;
        }
        return poolContainer;
    }

    /// <summary> 프리팹에 맞는 풀을 가져오거나 없으면 새로 생성 </summary>
    private IObjectPool<Projectile> GetOrCreatPool(Projectile prefab)
    {
        if(!poolDictionary.TryGetValue(prefab, out var pool))
        {
            pool = new ObjectPool<Projectile>(
                createFunc: () =>
                {
                    Projectile bullet = Instantiate(prefab,GetContainer());
                    return bullet;
                },
                actionOnGet: bullet => bullet.gameObject.SetActive(true),
                actionOnRelease: bullet => bullet.gameObject.SetActive(false),
                actionOnDestroy: bullet => Destroy(bullet.gameObject),
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 50
                );
                poolDictionary.Add(prefab, pool);
        }
        return pool;
    }

    /// <summary> 캐릭터 클릭 시 해당 캐릭터의 발사체를 발사 </summary>
    public void Shoot()
    {
        if(targetEnemy == null) return; // 적이 없을 경우 반환

        CharacterData currentCharacter = GameManager.Instance.currentCharacter;
        if (currentCharacter == null || currentCharacter.projectilePrefab == null) return;

        Projectile targetPrefab = currentCharacter.projectilePrefab;
        IObjectPool<Projectile> currentPool = GetOrCreatPool(targetPrefab);

        Projectile bullet = currentPool.Get();
        bullet.SetPool(currentPool);
        bullet.transform.position = spawnPoint.position; // 발사체가 생성되는 위치

        int currentDamage = GameManager.Instance.CurrentDamage; // 발사체의 데미지
        bullet.SetTarget(targetEnemy,currentDamage); 
    }

}
