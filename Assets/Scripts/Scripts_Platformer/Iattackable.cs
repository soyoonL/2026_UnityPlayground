/// <summary>
/// 공격을 할 수 있는 주체.
/// </summary>
public interface IAttackable
{
    /// <summary>한 번 공격할 때의 피해량</summary>
    int AttackDamage { get; }

    /// <summary>지금 공격할 수 있는지 (쿨타임, 생존 여부 등)</summary>
    bool CanAttack { get; }

    /// <summary>공격을 실행한다.</summary>
    void Attack();
}