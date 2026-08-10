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
    public void SetTarget(Transform enemyTarget)
    {
        target = enemyTarget;
    }

    IEnumerator MoveToTarget()
    {
        while (Vector3.Distance(transform.position,target.position)>=0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position,speed * Time.deltaTime);
            yield return null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponent<Enemy>();

        if (other.CompareTag("Enemy"))
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
