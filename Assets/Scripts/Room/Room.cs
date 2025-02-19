using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public List<Transform> doors = new List<Transform>();
    public Vector2Int size;

    public void InitializeDoors()
    {
        Debug.Log($"[Room {gameObject.name}] Initializing doors...");
        doors.Clear();
        FindDoorsRecursive(transform);
        Debug.Log($"[Room {gameObject.name}] Finished door initialization. Found {doors.Count} door(s).");
    }

    private void FindDoorsRecursive(Transform parent)
    {
        if (parent.CompareTag("Door"))
        {
            Debug.Log($"[Room {gameObject.name}] Found door: {parent.name} at position {parent.position}.");
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
        Debug.Log($"[Room {gameObject.name}] Getting effective size with rotation angle {angle}.");

        if (Mathf.Abs(angle % 180) > 45)
        {
            Debug.Log($"[Room {gameObject.name}] Rotation swapped dimensions: Original {size}, Effective ({size.y}, {size.x}).");
            return new Vector2Int(size.y, size.x);
        }

        Debug.Log($"[Room {gameObject.name}] Effective size remains {size}.");
        return size;
    }
}