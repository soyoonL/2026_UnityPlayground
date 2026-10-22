using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2D 플랫포머 플레이어 컨트롤러 (Unity 6.x / Input System / Rigidbody2D)
/// - 입력 수집: Update + Input 콜백 / 물리 처리: FixedUpdate (velocity 1회 읽기 → 1회 쓰기)
/// - 점프 선입력(Jump Buffer) + 코요테 타임 + 가변 점프 높이
/// - 확장: public 프로퍼티 / C# 이벤트 / protected virtual 메서드 / 외부 제어 API
/// - 매 프레임 GC 할당 없음 (OverlapBox 단일 결과 버전, 타임스탬프 방식 타이머)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    #region Inspector

    [Header("Input")]
    [Tooltip("Vector2 액션 (기본 Input Actions의 Player/Move)")]
    [SerializeField] private InputActionReference moveAction;
    [Tooltip("Button 액션 (기본 Input Actions의 Player/Jump)")]
    [SerializeField] private InputActionReference jumpAction;

    [Header("Move")]
    [SerializeField, Min(0f)] private float moveSpeed = 8f;
    [Tooltip("목표 속도까지 가속하는 정도 (units/s²)")]
    [SerializeField, Min(0f)] private float acceleration = 90f;
    [Tooltip("입력이 없을 때 멈추는 정도 (units/s²)")]
    [SerializeField, Min(0f)] private float deceleration = 110f;
    [Tooltip("공중에서 가감속 배율 (1 = 지상과 동일)")]
    [SerializeField, Range(0f, 1f)] private float airControl = 0.8f;

    [Header("Jump")]
    [Tooltip("점프 최고 높이 (유닛). gravityScale을 바꿔도 높이는 유지됨")]
    [SerializeField, Min(0f)] private float jumpHeight = 3.5f;
    [Tooltip("점프 키를 일찍 떼면 상승 속도에 곱해지는 값 (1 = 가변 점프 끔)")]
    [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
    [SerializeField, Min(0f)] private float maxFallSpeed = 20f;

    [Header("Jump Assist")]
    [Tooltip("착지 전 점프 입력을 기억하는 시간 (초)")]
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;
    [Tooltip("발판을 벗어난 뒤에도 점프를 허용하는 시간 (초)")]
    [SerializeField, Min(0f)] private float coyoteTime = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Rigidbody 위치 기준 오프셋 (보통 발 밑)")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);

    [Header("Visual")]
    [Tooltip("좌우 반전할 대상. 비우면 자기 자신을 반전")]
    [SerializeField] private Transform visualRoot;

    #endregion

    #region Runtime State

    private Rigidbody2D rb;
    private InputAction move;
    private InputAction jump;

    private float moveInput;
    private bool jumpHeld;
    private bool isJumping;        // 점프 상승 중 (착지 판정 무시용)
    private bool jumpCutApplied;
    private bool inputEnabled = true;

    // 타이머 대신 타임스탬프 사용 → 매 프레임 감산 연산 불필요
    private float lastJumpPressedTime = float.NegativeInfinity;
    private float lastGroundedTime = float.NegativeInfinity;

    #endregion

    #region Public API (확장 포인트)

    public bool IsGrounded { get; private set; }
    public bool IsJumping => isJumping;
    public int FacingDirection { get; private set; } = 1;
    public float MoveInput => moveInput;
    public Vector2 Velocity => rb.linearVelocity;
    public bool InputEnabled => inputEnabled;

    /// <summary>점프 실행 시 (사운드, 이펙트, 애니메이션 트리거)</summary>
    public event Action Jumped;
    /// <summary>공중 → 지상 전환 시</summary>
    public event Action Landed;
    /// <summary>바라보는 방향 변경 시 (1 = 오른쪽, -1 = 왼쪽)</summary>
    public event Action<int> FacingChanged;

    /// <summary>입력 잠금 (컷신, 피격 경직, 대화창 등)</summary>
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!enabled)
        {
            moveInput = 0f;
            jumpHeld = false;
            lastJumpPressedTime = float.NegativeInfinity;
        }
    }

    /// <summary>점프 요청 (모바일 버튼, AI, 리플레이 등 외부 입력용). 선입력 버퍼에 들어감</summary>
    public void RequestJump()
    {
        if (!inputEnabled) return;
        jumpHeld = true;
        lastJumpPressedTime = Time.time;
    }

    /// <summary>점프 키 해제 (가변 점프용)</summary>
    public void ReleaseJump() => jumpHeld = false;

    #endregion

    #region Unity Lifecycle

    // 컴포넌트를 처음 붙일 때 권장 설정 자동 적용 (에디터 전용 호출)
    protected virtual void Reset()
    {
        var body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.gravityScale = 3f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        groundLayer = LayerMask.GetMask("Ground");
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        if (visualRoot == null) visualRoot = transform;

        move = moveAction != null ? moveAction.action : null;
        jump = jumpAction != null ? jumpAction.action : null;
    }

    protected virtual void OnEnable()
    {
        move?.Enable();

        if (jump != null)
        {
            jump.performed += OnJumpPerformed;
            jump.canceled += OnJumpCanceled;
            jump.Enable();
        }
    }

    protected virtual void OnDisable()
    {
        if (jump != null)
        {
            jump.performed -= OnJumpPerformed;
            jump.canceled -= OnJumpCanceled;
        }

        moveInput = 0f;
        jumpHeld = false;
    }

    protected virtual void Update()
    {
        moveInput = (inputEnabled && move != null) ? move.ReadValue<Vector2>().x : 0f;
        UpdateFacing();
    }

    protected virtual void FixedUpdate()
    {
        Vector2 velocity = rb.linearVelocity;

        CheckGround(velocity.y);
        HandleMovement(ref velocity);
        HandleJump(ref velocity);
        HandleGravity(ref velocity);

        rb.linearVelocity = velocity;
    }

    #endregion

    #region Input Callbacks

    private void OnJumpPerformed(InputAction.CallbackContext _) => RequestJump();
    private void OnJumpCanceled(InputAction.CallbackContext _) => ReleaseJump();

    #endregion

    #region Core Logic

    private void CheckGround(float verticalVelocity)
    {
        // 상승이 끝나면 점프 상태 해제
        if (isJumping && verticalVelocity <= 0f) isJumping = false;

        bool wasGrounded = IsGrounded;

        // 점프 직후 판정 박스가 아직 땅에 겹쳐 있어도 착지로 보지 않음 (이중 점프 방지)
        IsGrounded = !isJumping &&
                     Physics2D.OverlapBox(rb.position + groundCheckOffset, groundCheckSize, 0f, groundLayer) != null;

        if (IsGrounded)
        {
            lastGroundedTime = Time.time; // 코요테 타임 기준점 갱신
            if (!wasGrounded) OnLanded();
        }
    }

    protected virtual void HandleMovement(ref Vector2 velocity)
    {
        float target = moveInput * moveSpeed;
        float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
        if (!IsGrounded) rate *= airControl;

        velocity.x = Mathf.MoveTowards(velocity.x, target, rate * Time.fixedDeltaTime);
    }

    protected virtual void HandleJump(ref Vector2 velocity)
    {
        // 가변 점프: 키를 떼면 상승 속도를 한 번 줄임 (점프한 다음 스텝부터 적용)
        if (isJumping && !jumpHeld && !jumpCutApplied && velocity.y > 0f)
        {
            velocity.y *= jumpCutMultiplier;
            jumpCutApplied = true;
        }

        float now = Time.time;
        bool hasBufferedJump = now - lastJumpPressedTime <= jumpBufferTime; // 선입력
        bool canJump = now - lastGroundedTime <= coyoteTime;                // 지상 or 코요테

        if (hasBufferedJump && canJump)
        {
            PerformJump(ref velocity);
        }
    }

    protected virtual void PerformJump(ref Vector2 velocity)
    {
        velocity.y = CalculateJumpVelocity();

        isJumping = true;
        jumpCutApplied = false;
        IsGrounded = false;

        // 버퍼와 코요테를 모두 소모 → 한 번의 입력으로 두 번 점프되지 않음
        lastJumpPressedTime = float.NegativeInfinity;
        lastGroundedTime = float.NegativeInfinity;

        Jumped?.Invoke();
    }

    protected virtual void HandleGravity(ref Vector2 velocity)
    {
        if (velocity.y < -maxFallSpeed) velocity.y = -maxFallSpeed;
    }

    protected virtual void OnLanded() => Landed?.Invoke();

    // v = √(2gh) : 원하는 높이에 정확히 도달하는 초기 속도
    private float CalculateJumpVelocity()
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        return Mathf.Sqrt(2f * gravity * jumpHeight);
    }

    private void UpdateFacing()
    {
        if (Mathf.Abs(moveInput) < 0.01f) return;

        int dir = moveInput > 0f ? 1 : -1;
        if (dir == FacingDirection) return; // 방향이 바뀔 때만 Transform 수정

        FacingDirection = dir;

        Vector3 scale = visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        visualRoot.localScale = scale;

        FacingChanged?.Invoke(dir);
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying && rb != null ? rb.position : (Vector2)transform.position;
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(origin + groundCheckOffset, groundCheckSize);
    }

    #endregion
}