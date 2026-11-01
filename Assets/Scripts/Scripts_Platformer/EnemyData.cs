using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 적 한 종류의 기본 데이터. 런타임에 값을 바꾸지 않는 읽기 전용 에셋.
    /// (현재 HP 같은 개체별 값은 Enemy가 따로 가진다)
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy/Enemy Data", order = 0)]
    public class EnemyData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string enemyName = "Enemy"; // Object.name과 겹치지 않도록 enemyName 사용

        [Header("스탯")]
        [SerializeField, Min(1)] private int maxHP = 10;
        [SerializeField, Min(0)] private int damage = 1;
        [SerializeField, Min(0f)] private float moveSpeed = 2f;

        [Header("피격")]
        [Tooltip("넉백 세기. 0이면 넉백 없음")]
        [SerializeField, Min(0f)] private float knockbackForce = 4f;
        [Tooltip("피격 경직 시간. 0이면 경직 없음 (슈퍼아머)")]
        [SerializeField, Min(0f)] private float hurtStunTime = 0.2f;

        [Header("보상")]
        [SerializeField, Min(0)] private int goldReward = 5;

        [Header("비주얼")]
        [Tooltip("애니메이터가 없거나 재생 전 표시할 기본 스프라이트")]
        [SerializeField] private Sprite sprite;
        [SerializeField] private RuntimeAnimatorController animatorController;

        [Header("행동")]
        [SerializeField] private EnemyBehaviour behaviour;

        public string EnemyName => enemyName;
        public int MaxHP => maxHP;
        public int Damage => damage;
        public float MoveSpeed => moveSpeed;
        public float KnockbackForce => knockbackForce;
        public float HurtStunTime => hurtStunTime;
        public int GoldReward => goldReward;
        public Sprite Sprite => sprite;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public EnemyBehaviour Behaviour => behaviour;
    }

}