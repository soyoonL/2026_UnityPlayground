using System;
using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 게임 전체 상태를 관리한다. 지금은 골드만 담당.
    /// Enemy는 GameManager를 모르고 사망 이벤트만 보내며, 여기서 그 이벤트를 받아 처리한다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("재화")]
        [SerializeField, Min(0)] private int gold;

        public int Gold => gold;

        /// <summary>골드가 바뀔 때 발생 (UI가 구독). 인자는 바뀐 뒤의 총 골드</summary>
        public event Action<int> OnGoldChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Enemy.OnEnemyDied += HandleEnemyDied;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Enemy.OnEnemyDied -= HandleEnemyDied;
            Instance = null;
        }

        private void HandleEnemyDied(Enemy enemy) => AddGold(enemy.Data.GoldReward);

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            gold += amount;
            OnGoldChanged?.Invoke(gold);
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0 || gold < amount) return false;
            gold -= amount;
            OnGoldChanged?.Invoke(gold);
            return true;
        }
    }

}