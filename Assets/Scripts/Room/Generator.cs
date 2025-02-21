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
    private List<DoorData> allDoors = new List<DoorData>();
    private int currentRoomCount = 0;

    IEnumerator Start()
    {
        Debug.Log("[Generator] Starting dungeon generation...");
        yield return StartCoroutine(GenerateDungeon());
        Debug.Log("[Generator] Dungeon generation complete!");
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

        // Add initial room's doors to the pool
        foreach (Transform door in initialRoom.doors)
        {
            Vector3 doorForward = door.forward;
            Vector2Int doorDirection = DirectionFromVector(doorForward);
            Vector2Int cellPosition = GridManager.Instance.ConvertToGridPosition(door.position);
            DoorData doorData = new DoorData(cellPosition, doorDirection);
            allDoors.Add(doorData);
            Debug.Log($"[Generator] Added initial room door at {cellPosition} facing {doorDirection}");
        }

        while (currentRoomCount < maxRooms)
        {
            bool roomPlaced = false;
            Debug.Log($"[Generator] Attempting to place room #{currentRoomCount + 1}.");

            // Create a copy of available doors
            List<DoorData> doorSnapshot = new List<DoorData>(allDoors);
        
            foreach (DoorData door in doorSnapshot)
            {
                Debug.Log($"[Generator] Checking door at cell {door.cell} with direction {door.direction}.");
                Vector2Int targetCell = door.cell + door.direction;
                Debug.Log($"[Generator] Target cell calculated: {targetCell}.");

                if (!IsCellValid(targetCell))
                {
                    Debug.LogWarning($"[Generator] Target cell {targetCell} is out of grid bounds. Skipping door.");
                    continue;
                }

                roomPlaced = TryPlaceRoomAtDoor(door, targetCell);
                if (roomPlaced)
                {
                    currentRoomCount++;
                    Debug.Log($"[Generator] Room placed at {targetCell}. Total rooms: {currentRoomCount}.");
                    yield return new WaitForSeconds(placementDelay);
                    break;
                }
                else
                {
                    Debug.Log($"[Generator] Unable to place a room at door from {door.cell} to target {targetCell}.");
                }
            }

            if (!roomPlaced)
            {
                Debug.LogWarning("[Generator] No valid door found for room placement. Ending generation.");
                break;
            }
        }
    }

    bool TryPlaceRoomAtDoor(DoorData door, Vector2Int targetCell)
    {
        Debug.Log($"[Generator] Attempting to attach room at target cell {targetCell} from door at {door.cell} facing {door.direction}.");

        // Shuffle available prefabs
        List<GameObject> shuffledPrefabs = new List<GameObject>(roomPrefabs);
        for (int i = 0; i < shuffledPrefabs.Count; i++)
        {
            int randomIndex = Random.Range(i, shuffledPrefabs.Count);
            GameObject temp = shuffledPrefabs[randomIndex];
            shuffledPrefabs[randomIndex] = shuffledPrefabs[i];
            shuffledPrefabs[i] = temp;
        }

        foreach (GameObject prefab in shuffledPrefabs)
        {
            Room roomTemplate = prefab.GetComponent<Room>();
            if (roomTemplate == null)
            {
                Debug.LogError($"[Generator] Prefab {prefab.name} is missing a Room component. Skipping.");
                continue;
            }

            Vector2Int effectiveSize = roomTemplate.GetEffectiveSize(Quaternion.identity);
            if (targetCell.x + effectiveSize.x >= GridManager.Instance.gridSize.x ||
                targetCell.y + effectiveSize.y >= GridManager.Instance.gridSize.y)
            {
                Debug.LogWarning($"[Generator] Not enough space for room {prefab.name} at {targetCell} with size {effectiveSize}. Skipping.");
                continue;
            }

            for (int i = 0; i < 4; i++)
            {
                Quaternion rotation = Quaternion.Euler(0, i * 90, 0);
                Debug.Log($"[Generator] Trying prefab {prefab.name} with rotation {rotation.eulerAngles}.");

                if (HasMatchingDoor(roomTemplate, rotation, -door.direction))
                {
                    Debug.Log($"[Generator] Prefab {prefab.name} has a matching door for direction {-door.direction}.");
                    if (GridManager.Instance.CanPlaceRoom(roomTemplate, targetCell, rotation))
                    {
                        Debug.Log($"[GridManager] GridManager approved placement for {prefab.name} at {targetCell} with rotation {rotation.eulerAngles}.");

                        Room placedRoom = SpawnRoom(prefab, targetCell, rotation);
                        if (placedRoom != null)
                        {
                            // Mark the target cell as available since it has a door pointing to it
                            GridManager.Instance.MarkAdjacentCellsAsAvailable(door.cell);

                            // Add new doors from the placed room to the pool
                            foreach (Transform newDoor in placedRoom.doors)
                            {
                                Vector3 doorForward = rotation * newDoor.forward;
                                Vector2Int doorDirection = DirectionFromVector(doorForward);
                                Vector2Int doorCellPosition = GridManager.Instance.ConvertToGridPosition(newDoor.position);

                                // Only add doors that aren't connecting to existing rooms
                                DoorData newDoorData = new DoorData(doorCellPosition, doorDirection);
                                bool isConnectedToExistingDoor = false;

                                foreach (DoorData existingDoor in allDoors)
                                {
                                    if (existingDoor.cell + existingDoor.direction == newDoorData.cell &&
                                        newDoorData.cell + newDoorData.direction == existingDoor.cell)
                                    {
                                        isConnectedToExistingDoor = true;
                                        break;
                                    }
                                }

                                if (!isConnectedToExistingDoor)
                                {
                                    allDoors.Add(newDoorData);
                                    Debug.Log($"[Generator] Added new door at {doorCellPosition} facing {doorDirection}");
                                }
                            }
                        }

                        return true;
                    }
                }
                else
                {
                    Debug.Log($"[Generator] Prefab {prefab.name} does NOT have a matching door with rotation {rotation.eulerAngles}.");
                }
            }
        }
        Debug.LogWarning($"[Generator] Failed to attach room at door from {door.cell} to target {targetCell}.");
        return false;
    }
    public Room SpawnRoom(GameObject prefab, Vector2Int gridPos, Quaternion rotation)
    {
        Debug.Log($"[Generator] Spawning room {prefab.name} at grid position {gridPos} with rotation {rotation.eulerAngles}.");

        // Get the room component early for size calculations
        Room roomTemplate = prefab.GetComponent<Room>();
        if (roomTemplate == null)
        {
            Debug.LogError($"[Generator] Prefab {prefab.name} misses Room component. Aborting spawn.");
            return null;
        }

        // Calculate effective size considering rotation
        Vector2Int effectiveSize = roomTemplate.GetEffectiveSize(rotation);

        // Calculate the bottom-left corner position
        Vector2Int cornerPosition = new Vector2Int(
            gridPos.x - ((effectiveSize.x - 1) / 2),
            gridPos.y - ((effectiveSize.y - 1) / 2)
        );

        // Convert to world position using the corner as reference
        Vector3 worldPosition = GridManager.Instance.ConvertToWorldPosition(cornerPosition);

        // Adjust world position to center the room properly
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

    bool HasMatchingDoor(Room room, Quaternion rotation, Vector2Int requiredDir)
    {
        Debug.Log($"[Generator] Searching for a matching door in room {room.gameObject.name} for direction {requiredDir} with rotation {rotation.eulerAngles}.");
        foreach (Transform door in room.doors)
        {
            Vector3 rotatedForward = rotation * door.forward;
            Vector2Int doorDir = DirectionFromVector(rotatedForward);
            Debug.Log($"[Generator] Door {door.name} rotated forward: {rotatedForward} (Grid direction: {doorDir}).");

            if (doorDir == requiredDir)
            {
                Debug.Log($"[Generator] Matching door found: {door.name}.");
                return true;
            }
        }
        Debug.LogWarning($"[Generator] No matching door found in room {room.gameObject.name} for direction {requiredDir}.");
        return false;
    }

    Vector2Int DirectionFromVector(Vector3 v)
    {
        if (Mathf.Abs(v.z) > Mathf.Abs(v.x))
            return v.z > 0 ? Vector2Int.up : Vector2Int.down;
        else
            return v.x > 0 ? Vector2Int.right : Vector2Int.left;
    }

    bool IsCellValid(Vector2Int cell)
    {
        bool valid = cell.x >= 0 && cell.x < GridManager.Instance.gridSize.x &&
                     cell.y >= 0 && cell.y < GridManager.Instance.gridSize.y;
        if (!valid)
            Debug.LogWarning($"[Generator] Cell {cell} is out of bounds (Grid size: {GridManager.Instance.gridSize}).");
        else
            Debug.Log($"[Generator] Cell {cell} is valid.");
        return valid;
    }
}

public class DoorData
{
    public Vector2Int cell;
    public Vector2Int direction;

    public DoorData(Vector2Int cell, Vector2Int direction)
    {
        this.cell = cell;
        this.direction = direction;
    }
}