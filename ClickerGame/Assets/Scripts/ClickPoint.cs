using UnityEngine;

public class ClickPoint : MonoBehaviour
{
    public int point = 0;
   
    public void PointUp()
    {
        point++;
        Debug.Log(point);
    }

   
}
