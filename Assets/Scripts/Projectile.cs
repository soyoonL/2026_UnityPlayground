using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class Projectile : MonoBehaviour
{
    private IObjectPool<Projectile> managedPool; // pool 객체(관리자 시스템)의 주소를 담은 변수
    private Transform target; // 적의 위치를 담을 변수
    private float speed = 10f; // 발사체의 속도
    public int damage; // 발사체의 데미지

    /// <summary> 발사체가 반납될 객체 풀의 참조를 등록 </summary>
    public void SetPool(IObjectPool<Projectile> pool)
    {
        managedPool = pool;
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

        StopAllCoroutines();
        StartCoroutine(MoveToTarget());
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
    /// 적과 충돌 시 데미지를 입히고 발사체를 비활성화(유니티 이벤트 함수)
    /// </summary>
    /// <param name="other">충돌한 적의 Collider2D</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            ReleaseToPool();
        }
    }

    /// <summary> 발사체를 풀에 반환(비활성화)하거나, 풀이 없는 경우 오브젝트를 파괴 </summary>
    private void ReleaseToPool()
    {
        if(managedPool != null)
        {
            managedPool.Release(this); // 스스로를 비활성화하고 반납
        }
        else
        {
            Destroy(gameObject); // managedPool이 null인 상태에서 managedPool.Release(this);를 호출하면 에러가 발생할 수 있어서 이를 대비한 안전장치
        }
    }
}
