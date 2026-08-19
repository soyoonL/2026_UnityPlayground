using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform target; // 적의 위치를 담을 변수
    private float speed = 10f; // 발사체의 속도
    public int damage = 10; // 발사체의 데미지

    /// <summary> 발사체 생성 시 코루틴 시작 </summary>
    private void Start()
    {
        StartCoroutine(MoveToTarget());
    }

    /// <summary>
    /// 발사체가 추적할 적의 위치와 적용할 데미지 초기화
    /// </summary>
    /// <param name="enemyTarget"> 적용할 적의 위치 </param>
    /// <param name="newDamage"> 적용할 발사체의 데미지 </param>
    public void SetTarget(Transform enemyTarget,int newDamage)
    {
        target = enemyTarget;
        damage = newDamage;
    }

    IEnumerator MoveToTarget()
    {
        while (target != null )
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position,speed * Time.deltaTime);
            yield return null;
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// 적과 충돌 시 데미지를 입히고 발사체를 파괴(유니티 이벤트 함수)
    /// </summary>
    /// <param name="other">충돌한 적의 Collider2D</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
