using System;
using UnityEngine;

/// <summary>
/// 풀링되는 모든 오브젝트의 부모 클래스입니다.
/// "어느 풀로 돌아가는지"와 "이중 반납 방지"를 여기서 한 번만 처리하므로,
/// 자식 클래스(Projectile, HitEffect 등)는 ReturnToPool()만 부르면 됩니다.
/// </summary>
public abstract class PoolableObject : MonoBehaviour
{
    // 반납할 때 실행할 함수. PoolManager가 생성 시점에 한 번만 넣어줌
    private Action releaseAction;

    /// <summary> 이미 풀로 반납된 상태면 true </summary>
    protected bool IsReleased { get; private set; }

    /// <summary> PoolManager 전용: 생성 시 "반납 방법"을 등록 </summary>
    public void SetReleaseAction(Action action)
    {
        releaseAction = action;
    }

    /// <summary> PoolManager 전용: 풀에서 꺼낼 때 반납 상태를 초기화 </summary>
    public void MarkSpawned()
    {
        IsReleased = false;
    }

    /// <summary> 풀로 반납. 여러 번 불려도 한 번만 처리됨 </summary>
    public void ReturnToPool()
    {
        if (IsReleased) return;
        IsReleased = true;

        if (releaseAction != null) releaseAction();
        else Destroy(gameObject); // 풀 없이 씬에 직접 배치된 경우
    }
}