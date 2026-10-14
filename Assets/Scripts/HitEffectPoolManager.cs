using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 히트 이펙트 프리팹별로 풀을 하나씩만 만들어 모든 발사체가 공유합니다.
/// 씬에 따로 배치하지 않아도 처음 사용할 때 자동으로 생성됩니다.
/// 씬이 바뀌면 함께 파괴되고, 다음 사용 시 다시 생성됩니다.
/// </summary>
public class HitEffectPoolManager : MonoBehaviour
{
    private static HitEffectPoolManager instance;

    public static HitEffectPoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("HitEffectPoolManager").AddComponent<HitEffectPoolManager>();
            }
            return instance;
        }
    }

    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxSize = 50;

    private readonly Dictionary<GameObject, IObjectPool<HitEffect>> pools = new Dictionary<GameObject, IObjectPool<HitEffect>>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void Spawn(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;

        if (!pools.TryGetValue(prefab, out IObjectPool<HitEffect> pool))
        {
            pool = CreatePool(prefab);
            pools.Add(prefab, pool);
        }

        HitEffect effect = pool.Get();
        // 위치를 먼저 잡고 활성화해야 이전 위치에서 한 프레임 재생되는 문제가 없음
        effect.transform.position = position;
        effect.gameObject.SetActive(true);
    }

    private IObjectPool<HitEffect> CreatePool(GameObject prefab)
    {
        Transform container = new GameObject($"{prefab.name}_Pool").transform;
        container.SetParent(transform);

        IObjectPool<HitEffect> pool = null;
        pool = new ObjectPool<HitEffect>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(prefab, container);
                obj.SetActive(false);

                if (!obj.TryGetComponent(out HitEffect effect))
                {
                    effect = obj.AddComponent<HitEffect>();
                }

                // 풀 주소는 생성 시점에 한 번만 넘김 (Get 이후 매번 넘길 필요 없음)
                effect.Init(pool);
                return effect;
            },
            actionOnGet: null, // 활성화는 Spawn에서 위치 설정 후에 직접 함
            actionOnRelease: effect => effect.gameObject.SetActive(false),
            actionOnDestroy: effect =>
            {
                if (effect != null) Destroy(effect.gameObject);
            },
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        return pool;
    }
}