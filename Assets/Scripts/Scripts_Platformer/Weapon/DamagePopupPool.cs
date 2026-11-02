using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Platformer
{
    /// <summary>
    /// 데미지 숫자 프리팹별 오브젝트 풀. Instantiate/Destroy 반복을 막는다.
    /// </summary>
    public static class DamagePopupPool
    {
        private static readonly Dictionary<DamagePopup, ObjectPool<DamagePopup>> pools = new Dictionary<DamagePopup, ObjectPool<DamagePopup>>();
        private static Transform root;

        // Enter Play Mode에서 도메인 리로드를 꺼도 이전 값이 남지 않도록 초기화
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            pools.Clear();
            root = null;
        }

        public static void Spawn(DamagePopup prefab, Vector2 position, int amount)
        {
            if (!pools.TryGetValue(prefab, out ObjectPool<DamagePopup> pool))
            {
                pool = CreatePool(prefab);
                pools.Add(prefab, pool);
            }

            pool.Get().Show(position, amount);
        }

        private static ObjectPool<DamagePopup> CreatePool(DamagePopup prefab)
        {
            if (root == null)
            {
                root = new GameObject("[DamagePopupPool]").transform;
                Object.DontDestroyOnLoad(root.gameObject);
            }

            ObjectPool<DamagePopup> pool = null;
            pool = new ObjectPool<DamagePopup>(
                createFunc: () =>
                {
                    DamagePopup p = Object.Instantiate(prefab, root);
                    p.SetPool(pool);
                    return p;
                },
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => { if (p != null) Object.Destroy(p.gameObject); },
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 40);

            return pool;
        }
    }
}
