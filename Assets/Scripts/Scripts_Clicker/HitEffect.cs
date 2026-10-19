using UnityEngine;

public class HitEffect : PoolableObject
{
    [SerializeField] private float effectDuration = 0.4f; // 애니메이션 길이를 바꾸면 이 값도 맞춰야 함

    private Animator animator;

    // 풀 주소(managedPool), Init(), 반납 처리는 PoolableObject가 처리

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (animator != null) animator.Play(0, 0, 0f);

        // Invoke는 오브젝트가 비활성화돼도 취소되지 않으므로 OnDisable에서 직접 취소함
        Invoke(nameof(ReturnToPool), effectDuration);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
    }
}