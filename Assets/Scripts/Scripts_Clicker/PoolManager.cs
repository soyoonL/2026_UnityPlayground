using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// PoolableObject를 상속한 모든 프리팹을 하나의 매니저로 풀링합니다.
/// 씬에 따로 배치하지 않아도 처음 사용할 때 자동으로 생성됩니다.
/// 씬이 바뀌면 함께 파괴되고, 다음 사용 시 다시 생성됩니다.
/// </summary>
public class PoolManager : MonoBehaviour
{
    private static PoolManager instance;

    public static PoolManager Instance
    {
        get
        {
            if (instance == null)
                instance = new GameObject("PoolManager").AddComponent<PoolManager>();
            return instance;
        }
    }

    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxSize = 50;

    // 키: 프리팹 / 값: 그 프리팹 전용 풀
    // 풀 타입이 IObjectPool<Projectile>, IObjectPool<HitEffect>처럼 제각각이라 object로 저장
    private readonly Dictionary<PoolableObject, object> pools = new Dictionary<PoolableObject, object>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    /// <summary> 프리팹을 풀에서 꺼내 position에 활성화하고 반환 </summary>
    public T Spawn<T>(T prefab, Vector3 position) where T : PoolableObject
    {
        if (prefab == null) return null;

        IObjectPool<T> pool = GetOrCreatePool(prefab);

        T obj = pool.Get();
        // 위치를 먼저 잡고 활성화해야 이전 위치에서 한 프레임 보이는 문제가 없음
        obj.transform.position = position;
        obj.MarkSpawned();
        obj.gameObject.SetActive(true);
        return obj;
    }

    /// <summary> 프리팹에 맞는 풀을 가져오거나 없으면 새로 생성 </summary>
    private IObjectPool<T> GetOrCreatePool<T>(T prefab) where T : PoolableObject
    {
        if (pools.TryGetValue(prefab, out object existing))
            return (IObjectPool<T>)existing; // object → 원래 타입으로 형변환

        Transform container = new GameObject($"{prefab.name}_Pool").transform;
        container.SetParent(transform);

        IObjectPool<T> pool = null;
        pool = new ObjectPool<T>(
            createFunc: () =>
            {
                T obj = Instantiate(prefab, container);
                obj.gameObject.SetActive(false);

                // "반납할 땐 이 풀에 Release해라"를 생성 시 한 번만 알려줌
                obj.SetReleaseAction(() => pool.Release(obj));
                return obj;
            },
            actionOnGet: null, // 활성화는 Spawn에서 위치 설정 후에 직접 함
            actionOnRelease: obj => obj.gameObject.SetActive(false),
            actionOnDestroy: obj =>
            {
                if (obj != null) Destroy(obj.gameObject);
            },
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        pools.Add(prefab, pool);
        return pool;
    }
}