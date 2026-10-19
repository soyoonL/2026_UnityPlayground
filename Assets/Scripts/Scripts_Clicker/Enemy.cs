using UnityEngine;
using System.Collections;

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
    public float currentHp; // 적의 현재 체력
    public int currentKillPoint; // 현재 적 처치 포인트

    private Vector3 baseScale;
    private Coroutine hitRoutine;

    /// <summary>
    /// 전달받은 적 데이터를 바탕으로 적의 능력치와 UI를 초기화
    /// </summary>
    /// <param name="data"> 적용할 적 데이터 </param>
    /// 
    void Start()
    {
        baseScale = UImanager.Instance.enemyImage.transform.localScale;
    }


    public void InitEnemy(EnemyData data)
    {
        currentHp = data.maxHp;
        currentKillPoint = data.killPoint;

        UImanager.Instance.hpSlider.maxValue = data.maxHp;
        UImanager.Instance.hpSlider.value = currentHp;
        UImanager.Instance.enemyImage.sprite = data.enemySprite;
    }

    /// <summary>
    /// 전달받은 발사체의 데미지만큼 적의 체력을 깎고, 만약 적이 사망할 시 PointUp(),SpawnRandomEnemy() 함수 호출
    /// </summary>
    /// <param name="damage"> 적용할 발사체의 데미지 </param>
    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        UImanager.Instance.hpSlider.value = currentHp;

        if (currentHp <= 0)
        {
            GameManager.Instance.OnEnemyKilled();
        }

        if (hitRoutine != null) StopCoroutine(hitRoutine);
        hitRoutine = StartCoroutine(HitReaction());
    }

    IEnumerator HitReaction()
    {
        var img = UImanager.Instance.enemyImage;
        Transform t = img.transform;
        Vector3 squash = Vector3.Scale(baseScale, new Vector3(1.12f, 0.88f, 1f));

        img.color = new Color(1f, 0.55f, 0.55f);
        t.localScale = squash;

        for (float e = 0; e < 0.12f; e += Time.deltaTime)
        {
            t.localScale = Vector3.Lerp(squash, baseScale, e / 0.12f);
            yield return null;
        }

        img.color = Color.white;
        t.localScale = baseScale;
        hitRoutine = null;
    }
}
