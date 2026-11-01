using UnityEngine;

/// <summary>
/// 피해를 받을 수 있는 대상 (적, 플레이어 공용).
/// 공격하는 쪽은 상대의 구체 클래스를 몰라도 이 인터페이스로만 데미지를 준다.
/// </summary>
public interface IDamageable
{
    /// <summary>이미 죽은 상태인지 (죽은 대상에게 중복 타격 방지)</summary>
    bool IsDead { get; }

    /// <summary>피해를 받는다.</summary>
    /// <param name="damage">피해량</param>
    /// <param name="hitDirection">공격자 → 피격자 방향 (넉백에 사용)</param>
    void TakeDamage(int damage, Vector2 hitDirection);
}