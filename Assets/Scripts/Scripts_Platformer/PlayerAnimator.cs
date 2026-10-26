using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    // 문자열 대신 해시 사용 → 매 프레임 문자열 비교 비용 제거
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private PlayerController controller;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable() => controller.Jumped += HandleJumped;  // 구독
    private void OnDisable() => controller.Jumped -= HandleJumped;  // 구독 해제 (필수)

    // Run: 지속 상태 → 매 프레임 값을 읽어서 전달
    private void Update()
    {
        animator.SetFloat(SpeedHash, Mathf.Abs(controller.Velocity.x));
        animator.SetBool(IsGroundedHash, controller.IsGrounded);
    }

    // Jump: 순간 이벤트 → 방송이 올 때만 실행
    private void HandleJumped() => animator.SetTrigger(JumpHash);
}
