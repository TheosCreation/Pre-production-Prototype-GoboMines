using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public List<Transform> doors = new List<Transform>();
    public Vector2Int size;

    public void InitializeDoors()
    {
      //  doors.Clear();
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Door"))
            {
                doors.Add(child);
            }
        }
    }

    public Vector2Int GetEffectiveSize(Quaternion rotation)
    {
        float angle = rotation.eulerAngles.y;
        if (Mathf.Abs(angle % 180) > 45) // 90 or 270 degrees
        {
            return new Vector2Int(size.y, size.x);
        }
        return size;
    }
}