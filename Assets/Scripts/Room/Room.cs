using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public List<Transform> doors = new List<Transform>();
    public Vector2Int size;

    public void InitializeDoors()
    {
        doors.Clear();
        FindDoorsRecursive(transform);
    }

    private void FindDoorsRecursive(Transform parent)
    {
        if (parent.CompareTag("Door"))
        {
            doors.Add(parent);
        }

        foreach (Transform child in parent)
        {
            FindDoorsRecursive(child);
        }
    }

    public Vector2Int GetEffectiveSize(Quaternion rotation)
    {
        float angle = rotation.eulerAngles.y;

        if (Mathf.Abs(angle % 180) > 45)
        {
            return new Vector2Int(size.y, size.x);
        }

        return size;
    }
}