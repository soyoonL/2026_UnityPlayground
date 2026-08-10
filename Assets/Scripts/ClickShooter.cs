using UnityEngine;

public class ClickShooter : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;  // 불꽃 틀
    [SerializeField] public Transform enemyTransform;     // 적 위치

    public void Shoot()
    {
        if(enemyTransform!=null)
        {
            Debug.Log("발사!");
            GameObject fire = Instantiate(projectilePrefab, transform.position, transform.rotation);
            fire.GetComponent<Projectile>().SetTarget(enemyTransform);
        }
       
    }
}
