using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        yield return StartCoroutine(GenerateDungeon());
    }

    IEnumerator GenerateDungeon()
    {
        // Spawn the initial room at the center of the grid
        Room initialRoom = SpawnRoom(initialRoomPrefab, Vector2Int.zero, Quaternion.identity);
        yield return new WaitForSeconds(placementDelay);

        while (currentRoomCount < maxRooms)
        {
            bool roomPlaced = false;

            // Iterate through all doors to find a valid connection
            foreach (DoorData door in new List<DoorData>(allDoors))
            {
                // Calculate the target cell for the new room
                Vector2Int targetCell = door.cell + door.direction;

                // Check if the target cell is valid and unoccupied
                if (!IsCellValid(targetCell)) {
                    Debug.Log("Not Valid: ");
                    continue;
                }

                // Try to place a room at the target cell
                roomPlaced = TryPlaceRoomAtDoor(door, targetCell);
                if (roomPlaced)
                {
                    currentRoomCount++;
                    yield return new WaitForSeconds(placementDelay);
                    break; // Move to the next iteration of the while loop
                }
            }

            // If no room was placed, stop the generation
            if (!roomPlaced) break;
        }
    }

    bool TryPlaceRoomAtDoor(DoorData door, Vector2Int targetCell)
    {
        foreach (GameObject prefab in roomPrefabs)
        {
            for (int i = 0; i < 4; i++) // Try all 4 rotations
            {
                Quaternion rotation = Quaternion.Euler(0, i * 90, 0);
                Room room = prefab.GetComponent<Room>();

                // Check if the room has a door that matches the required direction
                if (HasMatchingDoor(room, rotation, -door.direction) &&
                    GridManager.Instance.CanPlaceRoom(room, targetCell, rotation))
                {
                    // Spawn the room and mark cells as occupied
                    SpawnRoom(prefab, targetCell, rotation);
                    return true;
                }
            }
        }
        return false;
    }

    Room SpawnRoom(GameObject prefab, Vector2Int gridPos, Quaternion rotation)
    {
        // Instantiate the room at the correct position and rotation
        GameObject roomObj = Instantiate(prefab,
            GridManager.Instance.ConvertToWorldPosition(gridPos),
            rotation);
        Room room = roomObj.GetComponent<Room>();
        room.InitializeDoors();
        placedRooms.Add(room);

        // Mark the cells as occupied
        GridManager.Instance.OccupyCells(room, gridPos, rotation);

        // Add the room's doors to the list of all doors
        foreach (Transform door in room.doors)
        {
            Vector2Int doorCell = GridManager.Instance.ConvertToGridPosition(door.position);
            Vector2Int doorDir = DirectionFromVector(rotation * door.forward);
            allDoors.Add(new DoorData(doorCell, doorDir));

            // Mark the cell in front of the door as available
            Vector2Int availableCell = doorCell + doorDir;
            if (IsCellValid(availableCell))
            {
                GridManager.Instance.SetCellState(availableCell, GridManager.CellState.Available);
            }
        }

        return room;
    }

    bool HasMatchingDoor(Room room, Quaternion rotation, Vector2Int requiredDir)
    {
        foreach (Transform door in room.doors)
        {
            Vector3 rotatedForward = rotation * door.forward;
            if (DirectionFromVector(rotatedForward) == requiredDir)
                return true;
        }
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
        return cell.x >= 0 && cell.x < GridManager.Instance.gridSize.x &&
               cell.y >= 0 && cell.y < GridManager.Instance.gridSize.y;
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