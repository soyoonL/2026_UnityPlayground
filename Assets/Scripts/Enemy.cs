using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct EnemyData // 적 관련 데이터
{
    public string enemyName; // 적 이름
    public float maxHp; // 적 최대 체력
    public int killPoint; // 적 처치 포인트
    public Sprite enemySprite; // 적 이미지
}

public class Enemy : MonoBehaviour
{
    [SerializeField] Slider hpSlider; // 적 체력바 UI 저장
    [SerializeField] Image enemyImage; // 적 이미지 저장하고, 적이 사망 시 랜덤한 적의 이미지로 교체하는 데 사용

    public float currentHp; // 적의 현재 체력
    public int currentKillPoint; // 현재 적 처치 포인트

    /// <summary>
    /// 전달받은 적 데이터를 바탕으로 적의 능력치와 UI를 초기화
    /// </summary>
    /// <param name="data"> 적용할 적 데이터 </param>
    public void InitEnemy(EnemyData data)
    {
        currentHp = data.maxHp;
        currentKillPoint = data.killPoint;

        hpSlider.maxValue = data.maxHp;
        hpSlider.value = currentHp;
        enemyImage.sprite = data.enemySprite;
    }

    /// <summary>
    /// 전달받은 발사체의 데미지만큼 적의 체력을 깎고, 만약 적이 사망할 시 PointUp(),SpawnRandomEnemy() 함수 호출
    /// </summary>
    /// <param name="damage"> 적용할 발사체의 데미지 </param>
    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        hpSlider.value = currentHp;

        if (currentHp <= 0)
        {
            GameManager.Instance.PointUp();
            GameManager.Instance.SpawnRandomEnemy();
        }
    }
}
