using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform target;
    private float speed = 10f;
    public int damage = 10;

    private void Start()
    {
        StartCoroutine(MoveToTarget());
    }
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
