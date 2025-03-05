using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class Room : MonoBehaviour
{
    [System.Serializable]
    public class DoorInfo
    {
        public Transform doorTransform;
        public Vector2Int direction;
    }
    
    public List<DoorInfo> doors = new List<DoorInfo>();
    public Vector2Int size;
    [Range(0f, 1f)]
    public float spawnChance = 1f;
    private Vector3 CalculateCenter()
    {
        Bounds bounds = new Bounds();
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        if (allRenderers.Length > 0)
        {
            bounds = allRenderers[0].bounds;
            for (int i = 1; i < allRenderers.Length; i++)
            {
                bounds.Encapsulate(allRenderers[i].bounds);
            }
        }
        return bounds.center;
    }

    public void InitializeRoom()
    {
        doors.Clear();
        Vector3 center = CalculateCenter();
        FindDoorsIterative(center);
    }

    private void Start()
    {
        FindAndSpawnOres();
    }

    private void FindAndSpawnOres()
    {
        Stack<Transform> transformStack = new Stack<Transform>();
        transformStack.Push(transform);

        while (transformStack.Count > 0)
        {
            Transform current = transformStack.Pop();

            if (current.CompareTag("Ore"))
            {
                SpawnRandomOre(current);
            }

            foreach (Transform child in current)
            {
                transformStack.Push(child);
            }
        }

    }

    private void SpawnRandomOre(Transform transformToSpawn)
    {
        OreNode randomOre = GridManager.Instance.ores[UnityEngine.Random.Range(0, GridManager.Instance.ores.Count)];
        OreNode oreSpawned = Instantiate(randomOre, transformToSpawn.position, transformToSpawn.rotation);
    }

    private void FindDoorsIterative(Vector3 center)
    {
        Stack<Transform> transformStack = new Stack<Transform>();
        transformStack.Push(transform);

        while (transformStack.Count > 0)
        {
            Transform current = transformStack.Pop();

            if (current.CompareTag("Door"))
            {
                Vector3 doorPos = current.position;
                Vector3 relativePos = doorPos - center;

                Vector2Int direction = Vector2Int.zero;
                if (Mathf.Abs(relativePos.x) > Mathf.Abs(relativePos.z))
                {
                    direction = relativePos.x > 0 ? Vector2Int.right : Vector2Int.left;
                }
                else
                {
                    direction = relativePos.z > 0 ? Vector2Int.up : Vector2Int.down;
                }

                doors.Add(new DoorInfo
                {
                    doorTransform = current,
                    direction = direction
                });
            }

            foreach (Transform child in current)
            {
                transformStack.Push(child);
            }
        }
    }

    public void UpdateDoors()
    {
        Vector3 center = CalculateCenter();
        doors.Clear();
        FindDoorsIterative(center);
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

    private void OnDrawGizmos()
    {
        Vector3 center = CalculateCenter();
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(center, 0.1f);

    }
}
