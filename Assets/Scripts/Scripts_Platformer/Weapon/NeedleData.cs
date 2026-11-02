using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 바늘 무기의 수치 데이터. 런타임에 값을 바꾸지 않는 읽기 전용 에셋.
    /// </summary>
    [CreateAssetMenu(fileName = "NeedleData", menuName = "Game/Weapon/Needle Data")]
    public class NeedleData : ScriptableObject
    {
        [Header("차징")]
        [Tooltip("이 시간 동안 누르고 있으면 최대 힘")]
        [SerializeField, Min(0.05f)] private float maxChargeTime = 0.8f;
        [Tooltip("차징 중 이동 속도 배율 (0.3 = 30%)")]
        [SerializeField, Range(0f, 1f)] private float chargeMoveMultiplier = 0.3f;

        [Header("던지기")]
        [SerializeField, Min(0f)] private float minThrowSpeed = 5f;
        [Tooltip("평지 최대 사거리 = 속도² / (9.81 × 중력 배율). 실 길이보다 짧게 유지할 것")]
        [SerializeField, Min(0f)] private float maxThrowSpeed = 13f;

        [Header("바늘 물리 (무게감)")]
        [SerializeField, Min(0f)] private float gravityScale = 2f;
        [SerializeField, Min(0.01f)] private float mass = 3f;
        [SerializeField, Min(0f)] private float linearDamping = 0.1f;
        [Tooltip("튕기는 정도 (0 = 전혀 안 튕김)")]
        [SerializeField, Range(0f, 1f)] private float bounciness = 0.05f;
        [Tooltip("바닥에서 미끄러지지 않는 정도")]
        [SerializeField, Min(0f)] private float friction = 0.8f;
        [Tooltip("땅에 처음 부딪힐 때 남기는 속도 비율 (작을수록 '쿵' 하고 멈춤)")]
        [SerializeField, Range(0f, 1f)] private float impactSpeedKeep = 0.35f;

        [Header("실")]
        [SerializeField, Min(1f)] private float maxRopeLength = 10f;
        [Tooltip("실이 느슨할 때 가장 많이 처지는 정도")]
        [SerializeField, Min(0f)] private float maxRopeSag = 1.2f;

        [Header("회수")]
        [SerializeField, Min(0f)] private float pullSpeed = 14f;
        [Tooltip("끌려오는 속도에 도달하는 빠르기 (작을수록 묵직하게 끌려옴)")]
        [SerializeField, Min(0f)] private float pullAcceleration = 60f;
        [Tooltip("바늘귀가 손에서 이 거리 안으로 들어오면 회수 완료")]
        [SerializeField, Min(0.05f)] private float catchDistance = 0.6f;

        [Header("공격")]
        [SerializeField, Min(0)] private int damage = 2;
        [Tooltip("같은 적에게 연속 데미지가 들어가는 간격 (초)")]
        [SerializeField, Min(0.02f)] private float hitInterval = 0.25f;

        public float MaxChargeTime => maxChargeTime;
        public float ChargeMoveMultiplier => chargeMoveMultiplier;
        public float GravityScale => gravityScale;
        public float Mass => mass;
        public float LinearDamping => linearDamping;
        public float Bounciness => bounciness;
        public float Friction => friction;
        public float ImpactSpeedKeep => impactSpeedKeep;
        public float MaxRopeLength => maxRopeLength;
        public float MaxRopeSag => maxRopeSag;
        public float PullSpeed => pullSpeed;
        public float PullAcceleration => pullAcceleration;
        public float CatchDistance => catchDistance;
        public int Damage => damage;
        public float HitInterval => hitInterval;

        /// <summary>차징 비율(0~1)에 따른 던지는 속도</summary>
        public float GetThrowSpeed(float charge01) => Mathf.Lerp(minThrowSpeed, maxThrowSpeed, charge01);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (minThrowSpeed > maxThrowSpeed) minThrowSpeed = maxThrowSpeed;

            // 최대 힘으로 던져도 실 길이 안에 떨어지는지 확인
            float g = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
            float flatRange = g > 0f ? maxThrowSpeed * maxThrowSpeed / g : float.PositiveInfinity;
            if (flatRange > maxRopeLength)
                Debug.LogWarning($"{name}: 평지 최대 사거리(약 {flatRange:F1})가 실 길이({maxRopeLength})보다 깁니다. 최대 속도를 낮추거나 실을 늘려주세요.", this);
        }
#endif
    }
}