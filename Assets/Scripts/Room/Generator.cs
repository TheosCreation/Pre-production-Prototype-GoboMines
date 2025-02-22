using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Generator : MonoBehaviour
{
    public List<GameObject> roomPrefabs;
    public GameObject initialRoomPrefab;
    public int maxRooms = 10;
    public float placementDelay = 0.5f;

    private List<Room> placedRooms = new List<Room>();
    private int currentRoomCount = 0;

    IEnumerator Start()
    {
        yield return StartCoroutine(GenerateDungeon());
    }

    IEnumerator GenerateDungeon()
    {
        Debug.Log("[Generator] Spawning initial room at grid center (0,0).");
        Room initialRoom = SpawnRoom(initialRoomPrefab, new Vector2Int(25, 25), Quaternion.identity);
        if (initialRoom == null)
        {
            Debug.LogError("[Generator] Failed to spawn the initial room. Aborting generation.");
            yield break;
        }
        yield return new WaitForSeconds(placementDelay);

        UpdateAdjacentCells(initialRoom, new Vector2Int(25, 25), Quaternion.identity);

        while (currentRoomCount < maxRooms)
        {
            List<Vector2Int> availableCells = GridManager.Instance.GetAvailableCells();
            if (availableCells.Count == 0)
            {
                Debug.Log("[Generator] No more available cells for room placement. Ending generation.");
                break;
            }

            // Shuffle the list of available cells
            ShuffleList(availableCells);

            Vector2Int targetCell = availableCells[0]; // Always take the first cell after shuffling
            List<Vector2Int> requiredConnections = GridManager.Instance.GetCellConnections(targetCell);

            // Shuffle the list of room prefabs
            ShuffleList(roomPrefabs);

            bool roomPlaced = TryPlaceRoomAtCell(targetCell, requiredConnections);
            if (roomPlaced)
            {
                currentRoomCount++;
                yield return new WaitForSeconds(placementDelay);
            }
            else
            {
                GridManager.Instance.SetCellState(targetCell, GridManager.CellState.Unoccupied);
            }
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    bool TryPlaceRoomAtCell(Vector2Int targetCell, List<Vector2Int> requiredConnections)
    {
        foreach (GameObject prefab in roomPrefabs)
        {
            Room roomTemplate = prefab.GetComponent<Room>();
            if (roomTemplate == null) continue;

            // Generate a random rotation (0, 90, 180, or 270 degrees)
            float randomRotation = Random.Range(0, 4) * 90f;
            Quaternion rot = Quaternion.Euler(0, randomRotation, 0);

            if (GridManager.Instance.CanPlaceRoom(roomTemplate, targetCell, rot) &&
                HasMatchingConnection(roomTemplate, rot, requiredConnections))
            {
                Room placedRoom = SpawnRoom(prefab, targetCell, rot);
                if (placedRoom != null)
                {
                    UpdateAdjacentCells(placedRoom, targetCell, rot);
                    return true;
                }
            }
        }
        return false;
    }

    bool HasMatchingConnection(Room room, Quaternion rotation, List<Vector2Int> requiredConnections)
    {
        foreach (Vector2Int requiredDir in requiredConnections)
        {
            bool foundMatch = false;
            foreach (Transform door in room.doors)
            {
                Vector3 rotatedForward = rotation * door.forward;
                Vector2Int doorDir = DirectionFromVector(rotatedForward);
                Debug.Log("DOOR DIRECTION " + doorDir);
                if (doorDir == requiredDir)
                {
                    foundMatch = true;
                    break;
                }
            }
            if (!foundMatch) return false;
        }
        return true;
    }

    public Room SpawnRoom(GameObject prefab, Vector2Int gridPos, Quaternion rotation)
    {
        Debug.Log($"[Generator] Spawning room {prefab.name} at grid position {gridPos} with rotation {rotation.eulerAngles}.");

        Room roomTemplate = prefab.GetComponent<Room>();
        if (roomTemplate == null)
        {
            Debug.LogError($"[Generator] Prefab {prefab.name} misses Room component. Aborting spawn.");
            return null;
        }

        Vector2Int effectiveSize = roomTemplate.GetEffectiveSize(rotation);
        Vector2Int cornerPosition = new Vector2Int(
            gridPos.x - ((effectiveSize.x - 1) / 2),
            gridPos.y - ((effectiveSize.y - 1) / 2)
        );

        Vector3 worldPosition = GridManager.Instance.ConvertToWorldPosition(cornerPosition);
        worldPosition += new Vector3(
            (effectiveSize.x * GridManager.Instance.cellSize) / 2f,
            0,
            (effectiveSize.y * GridManager.Instance.cellSize) / 2f
        );

        GameObject roomObj = Instantiate(prefab, worldPosition, rotation);
        if (roomObj == null)
        {
            Debug.LogError($"[Generator] Instantiation of room {prefab.name} failed.");
            return null;
        }

        Room room = roomObj.GetComponent<Room>();
        if (room == null)
        {
            Debug.LogError($"[Generator] Instantiated object {prefab.name} does not have a Room component.");
            return null;
        }

        room.InitializeDoors();
        placedRooms.Add(room);
        GridManager.Instance.OccupyCells(room, cornerPosition, rotation);

        Debug.Log($"[Generator] Successfully spawned {roomObj.name} at grid position {cornerPosition} (world pos: {worldPosition}).");
        return room;
    }

    void UpdateAdjacentCells(Room room, Vector2Int cellPosition, Quaternion rotation)
    {
        List<Vector2Int> newCells = new List<Vector2Int>();
        foreach (Transform door in room.doors)
        {
            Vector3 rotatedForward = rotation * door.forward;
            Vector2Int doorDirection = DirectionFromVector(rotatedForward);
            GridManager.Instance.MarkAdjacentCellsAsAvailable(cellPosition, doorDirection);
            newCells.Add(doorDirection);
        }
            Debug.Log("Count "+newCells.Count);
    }

    Vector2Int DirectionFromVector(Vector3 v)
    {
        if (Mathf.Abs(v.z) > Mathf.Abs(v.x))
            return v.z > 0 ? Vector2Int.up : Vector2Int.down;
        else
            return v.x > 0 ? Vector2Int.right : Vector2Int.left;
    }
}