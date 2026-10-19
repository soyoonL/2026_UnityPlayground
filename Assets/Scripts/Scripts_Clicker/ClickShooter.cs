using UnityEditor.EditorTools;
using UnityEngine;

public class ClickShooter : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;  // 소환되는 위치
    [SerializeField] private Transform targetEnemy; // 적의 위치가 고정되어 있으므로 인스펙터에서 연결

    // 풀 Dictionary, GetContainer(), GetOrCreatPool()은 PoolManager로 이동

    /// <summary> 캐릭터 클릭 시 해당 캐릭터의 발사체를 발사 </summary>
    public void Shoot()
    {
        if (targetEnemy == null) return; // 적이 없을 경우 반환

        CharacterData currentCharacter = GameManager.Instance.currentCharacter;
        if (currentCharacter == null || currentCharacter.projectilePrefab == null) return;

        // 기존 동작 유지: 사냥 캐릭터 애니메이터가 꺼져 있으면 발사하지 않음
        if (UImanager.Instance == null) return;
        Animator huntAnimator = UImanager.Instance.huntCharacterAnimator;
        if (huntAnimator == null || !huntAnimator.isActiveAndEnabled) return;
        huntAnimator.Play("Attack", 0, 0.5f);

        Projectile bullet = PoolManager.Instance.Spawn(currentCharacter.projectilePrefab, spawnPoint.position);
        bullet.SetTarget(targetEnemy, GameManager.Instance.CurrentDamage);

        AudioManager.Instance.PlaySFX(currentCharacter.shootSfxType, true);
    }
}