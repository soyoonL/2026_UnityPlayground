using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Platformer
{
    /// <summary>바늘 무기의 상태</summary>
    public enum NeedleState
    {
        Ready,      // 손에 있음
        Charging,   // 조준 중 (왼쪽 버튼 누르는 중)
        Thrown,     // 던져진 상태 (날아가거나 바닥에 놓임)
        Retracting  // 실을 당기는 중 (오른쪽 버튼 누르는 중)
    }

    /// <summary>
    /// 플레이어의 바늘 무기. 입력, 차징, 상태 전환을 담당한다.
    /// 물리는 Needle, 실 그리기는 NeedleRope, 궤적 표시는 ThrowTrajectory에 맡긴다.
    /// 흐름: Ready > Charging(왼쪽 누름) > Thrown(뗌) >< Retracting(오른쪽 누름) > /Retracting>/Retracting> Ready(손에 도착)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public class NeedleWeapon : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private NeedleData data;

        [Header("구성")]
        [SerializeField] private Needle needle;
        [Tooltip("바늘을 던지고 실이 이어지는 손 위치 (좌우 반전되는 오브젝트의 자식)")]
        [SerializeField] private Transform handPoint;
        [SerializeField] private NeedleRope rope;
        [SerializeField] private ThrowTrajectory trajectory;

        [Header("입력")]
        [Tooltip("던지기: Player/Attack (마우스 왼쪽)")]
        [SerializeField] private InputActionReference throwAction;
        [Tooltip("당기기: Player/Pull (마우스 오른쪽)")]
        [SerializeField] private InputActionReference pullAction;

        [Header("궤적 예측")]
        [SerializeField] private LayerMask groundLayer;

        [Header("조준")]
        [Tooltip("마우스가 몸 중심에서 좌우로 이 거리 이상 벗어나야 방향을 바꿈")]
        [SerializeField, Min(0f)] private float facingDeadZone = 0.3f;
        [Tooltip("마우스가 몸 중심에 이보다 가까우면 이전 조준 방향 유지")]
        [SerializeField, Min(0f)] private float minAimDistance = 0.5f;

        private Vector2 lastAimDir = Vector2.right;

        [Header("런타임 상태 (확인용)")]
        [SerializeField] private NeedleState state;
        [SerializeField, Range(0f, 1f)] private float charge;

        private PlayerController player;
        private Camera cam;
        private InputAction throwInput;
        private InputAction pullInput;
        private float chargeStartTime;

        // 애니메이터 파라미터 (없으면 자동으로 건너뜀)
        private Animator animator;
        private static readonly int AimingHash = Animator.StringToHash("Aiming");
        private static readonly int ThrowHash = Animator.StringToHash("Throw");
        private static readonly int PullingHash = Animator.StringToHash("Pulling");
        private static readonly int HasNeedleHash = Animator.StringToHash("HasNeedle");
        private bool hasAimingParam, hasThrowParam, hasPullingParam, hasNeedleParam;

        public NeedleState State => state;
        public float Charge01 => charge;

        /// <summary>사운드, 이펙트 연결용</summary>
        public event Action ChargeStarted;
        public event Action Thrown;
        public event Action Caught;

        #region 생명주기

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            animator = GetComponentInChildren<Animator>();
            cam = Camera.main;
            throwInput = throwAction != null ? throwAction.action : null;
            pullInput = pullAction != null ? pullAction.action : null;

            if (data == null || needle == null || handPoint == null)
            {
                Debug.LogError($"{name}: NeedleWeapon의 Data, Needle, Hand Point를 모두 연결해주세요.", this);
                enabled = false;
                return;
            }

            // 프리팹 에셋을 직접 넣었다면 씬에 생성
            if (!needle.gameObject.scene.IsValid())
                needle = Instantiate(needle);

            // 플레이어의 좌우 반전(스케일 -1)과 이동 영향을 받지 않도록 월드로 분리
            needle.transform.SetParent(null, true);
            if (rope != null) rope.transform.SetParent(null, true);
            if (trajectory != null)
            {
                trajectory.transform.SetParent(null, true);
                trajectory.Hide();
            }

            needle.Initialize(data, handPoint);
            if (rope != null) rope.Bind(handPoint, needle, data);

            CacheAnimatorParams();
            state = NeedleState.Ready;
            SetBool(HasNeedleHash, hasNeedleParam, true);
        }

        private void OnEnable()
        {
            throwInput?.Enable();
            pullInput?.Enable();
        }

        private void OnDisable() => ResetToReady();

        private void OnDestroy()
        {
            // 분리해 둔 오브젝트 정리
            if (needle != null) Destroy(needle.gameObject);
            if (rope != null) Destroy(rope.gameObject);
            if (trajectory != null) Destroy(trajectory.gameObject);
        }

        private void Update()
        {
            if (throwInput == null || pullInput == null) return;

            // 컷신, 대화 등으로 입력이 잠기면 조작 중이던 동작을 멈춤
            if (!player.InputEnabled)
            {
                if (state == NeedleState.Charging) CancelCharge();
                else if (state == NeedleState.Retracting) StopRetract();
                return;
            }

            switch (state)
            {
                case NeedleState.Ready:
                    if (throwInput.WasPressedThisFrame()) BeginCharge();
                    break;

                case NeedleState.Charging:
                    charge = Mathf.Clamp01((Time.time - chargeStartTime) / data.MaxChargeTime);
                    Vector2 velocity = GetThrowVelocity(out Vector2 origin, out int faceDir);
                    player.SetFacingOverride(faceDir); // 조준하는 쪽을 바라봄

                    if (trajectory != null)
                    {
                        Vector2 gravity = Physics2D.gravity * data.GravityScale;
                        trajectory.Show(origin, velocity, gravity, data.LinearDamping, data.MaxRopeLength, groundLayer, charge);
                    }

                    // 뗀 순간을 놓쳐도 안전하도록 '누르고 있지 않음'으로 판정
                    if (!throwInput.IsPressed()) Throw(origin, velocity);
                    break;

                case NeedleState.Thrown:
                    if (pullInput.IsPressed()) StartRetract();
                    break;

                case NeedleState.Retracting:
                    if (!pullInput.IsPressed()) StopRetract();
                    else if (Vector2.Distance(needle.RopeAnchorPosition, handPoint.position) <= data.CatchDistance) Catch();
                    break;
            }
        }

        #endregion

        #region 상태 전환

        private void BeginCharge()
        {
            state = NeedleState.Charging;
            chargeStartTime = Time.time;
            charge = 0f;
            lastAimDir = new Vector2(player.FacingDirection, 0f); // 조준 시작은 바라보는 방향

            player.SetMoveSpeedMultiplier(data.ChargeMoveMultiplier);
            player.SetJumpEnabled(false);
            SetBool(AimingHash, hasAimingParam, true);
            ChargeStarted?.Invoke();
        }

        private void Throw(Vector2 origin, Vector2 velocity)
        {
            EndChargeEffects();
            needle.Launch(origin, velocity);
            state = NeedleState.Thrown;

            SetTrigger(ThrowHash, hasThrowParam);
            SetBool(HasNeedleHash, hasNeedleParam, false);
            Thrown?.Invoke();
        }

        private void CancelCharge()
        {
            EndChargeEffects();
            state = NeedleState.Ready;
        }

        private void StartRetract()
        {
            state = NeedleState.Retracting;
            needle.SetPulling(true);
            SetBool(PullingHash, hasPullingParam, true);
        }

        private void StopRetract()
        {
            state = NeedleState.Thrown;
            needle.SetPulling(false);
            SetBool(PullingHash, hasPullingParam, false);
        }

        private void Catch()
        {
            needle.Hide();
            state = NeedleState.Ready;
            SetBool(PullingHash, hasPullingParam, false);
            SetBool(HasNeedleHash, hasNeedleParam, true);
            Caught?.Invoke();
        }

        /// <summary>비활성화 등으로 강제로 손에 돌려놓기</summary>
        public void ResetToReady()
        {
            if (state == NeedleState.Charging) EndChargeEffects();
            if (needle != null) needle.Hide();
            state = NeedleState.Ready;
            SetBool(PullingHash, hasPullingParam, false);
            SetBool(HasNeedleHash, hasNeedleParam, true);
        }

        // 차징 때 걸어 둔 제한을 모두 풀기
        private void EndChargeEffects()
        {
            charge = 0f;
            if (player != null)
            {
                player.SetMoveSpeedMultiplier(1f);
                player.SetJumpEnabled(true);
                player.ClearFacingOverride();
            }
            if (trajectory != null) trajectory.Hide();
            SetBool(AimingHash, hasAimingParam, false);
        }

        #endregion

        #region 조준

        private Vector2 GetThrowVelocity(out Vector2 origin, out int faceDir)
        {
            origin = handPoint.position;

            // 좌우 반전에 영향받지 않는 기준점 (x는 몸 중심, y는 손 높이)
            Vector2 pivot = new Vector2(transform.position.x, handPoint.position.y);
            Vector2 toMouse = GetMouseWorldPosition() - pivot;

            // 마우스가 너무 가까우면 방향이 튀지 않게 이전 방향 유지
            if (toMouse.sqrMagnitude >= minAimDistance * minAimDistance)
                lastAimDir = toMouse.normalized;

            // 중심 근처에서는 바라보는 방향을 바꾸지 않음
            faceDir = Mathf.Abs(toMouse.x) > facingDeadZone
                ? (toMouse.x > 0f ? 1 : -1)
                : player.FacingDirection;

            return lastAimDir * data.GetThrowSpeed(charge);
        }

        private Vector2 GetMouseWorldPosition()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || Mouse.current == null)
                return (Vector2)handPoint.position + new Vector2(player.FacingDirection, 0f);

            Vector3 screen = Mouse.current.position.ReadValue();
            screen.z = -cam.transform.position.z;
            return cam.ScreenToWorldPoint(screen);
        }

        #endregion

        #region 애니메이터

        private void CacheAnimatorParams()
        {
            hasAimingParam = hasThrowParam = hasPullingParam = hasNeedleParam = false;
            if (animator == null || animator.runtimeAnimatorController == null) return;

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.nameHash == AimingHash) hasAimingParam = true;
                else if (p.nameHash == ThrowHash) hasThrowParam = true;
                else if (p.nameHash == PullingHash) hasPullingParam = true;
                else if (p.nameHash == HasNeedleHash) hasNeedleParam = true;
            }
        }

        private void SetBool(int hash, bool exists, bool value)
        {
            if (exists) animator.SetBool(hash, value);
        }

        private void SetTrigger(int hash, bool exists)
        {
            if (exists) animator.SetTrigger(hash);
        }

        #endregion
    }
}
