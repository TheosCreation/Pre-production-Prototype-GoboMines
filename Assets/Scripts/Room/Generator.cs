using Mono.Cecil.Cil;
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
    private int currentRoomCount = 0;
    private HashSet<Vector2Int> processedCells = new HashSet<Vector2Int>();

    IEnumerator Start()
    {
        yield return StartCoroutine(GenerateDungeon());
    }

    IEnumerator GenerateDungeon()
    {
        Room initialRoom = SpawnRoom(initialRoomPrefab, new Vector2Int(25, 25), Quaternion.identity);
        processedCells.Add(new Vector2Int(25, 25));

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

            ShuffleList(availableCells);
            bool roomPlaced = false;

            foreach (Vector2Int targetCell in availableCells)
            {
                if (processedCells.Contains(targetCell))
                {
                    continue;
                }

                List<Vector2Int> requiredConnections = GridManager.Instance.GetCellConnections(targetCell);
                if (TryPlaceRoomAtCell(targetCell, requiredConnections))
                {
                    processedCells.Add(targetCell);
                    currentRoomCount++;
                    roomPlaced = true;
                    yield return new WaitForSeconds(placementDelay);
                    break;
                }
                else
                {
                    GridManager.Instance.SetCellState(targetCell, GridManager.CellState.Available);
                    processedCells.Add(targetCell);
                }
            }

            if (!roomPlaced)
            {
                Debug.Log("[Generator] No valid placements found in current pass. Ending generation.");
                break;
            }
        }
    }
    // This fully works
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
   
    public bool TryPlaceRoomAtCell(Vector2Int targetCell, List<Vector2Int> requiredConnections)
    {
        List<GameObject> shuffledPrefabs = new List<GameObject>(roomPrefabs);
        ShuffleList(shuffledPrefabs);

        foreach (GameObject prefab in shuffledPrefabs)
        {
            Room roomTemplate = prefab.GetComponent<Room>();
            if (roomTemplate == null)
            {
                Debug.Log("No room template found");
                continue;
            }
            List<int> rotationAngles = new List<int> { 0, 90, 180, 270 };
            ShuffleList(rotationAngles);

            foreach (int angle in rotationAngles)
            {
                Quaternion rot = Quaternion.Euler(0, angle, 0);

                if (GridManager.Instance.CanPlaceRoom(roomTemplate, targetCell, rot))
                {
                    
                    Room placedRoom = SpawnRoom(prefab, targetCell, rot);
                    UpdateAdjacentCells(placedRoom, targetCell, rot);
                    return true;
                   
                }
            }
        }
        return false;
    }

    public Room SpawnRoom(GameObject prefab, Vector2Int gridPos, Quaternion rotation)
    {
        Room roomTemplate = prefab.GetComponent<Room>();

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

        Room room = roomObj.GetComponent<Room>();

        room.InitializeDoors();
        placedRooms.Add(room);
        GridManager.Instance.OccupyCells(room, cornerPosition, rotation);
        return room;
    }

    void UpdateAdjacentCells(Room room, Vector2Int cellPosition, Quaternion rotation)
    {
        foreach (Room.DoorInfo door in room.doors)
        {
            GridManager.Instance.MarkAdjacentCellsAsAvailable(cellPosition, door.direction);
        }
    }
}