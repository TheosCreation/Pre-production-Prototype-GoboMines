using System.Collections;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
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
    private Dictionary<GameObject, Room> roomTemplates = new Dictionary<GameObject, Room>();
    public delegate void GenerationCompleteHandler();
    public event GenerationCompleteHandler OnGenerationComplete;

    private bool isGenerating = false;
    void Start()
    {
        InitializeRoomTemplates();
        StartCoroutine(GenerateDungeon());
    }
    private void InitializeRoomTemplates()
    {
        foreach (GameObject prefab in roomPrefabs)
        {
            GameObject templateObject = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            templateObject.SetActive(false); 
            DontDestroyOnLoad(templateObject);

            Room roomComponent = templateObject.GetComponent<Room>();
            roomComponent.InitializeDoors();

            roomTemplates[prefab] = roomComponent;
        }
    }
    IEnumerator GenerateDungeon()
    {
        isGenerating = true;

        Vector2Int halfGridSize = new Vector2Int(GridManager.Instance.gridSize.x / 2, GridManager.Instance.gridSize.y / 2);
        Room initialRoom = SpawnRoom(initialRoomPrefab, halfGridSize, Quaternion.identity, Vector2Int.zero);
        processedCells.Add(halfGridSize);

        yield return new WaitForSeconds(placementDelay);
        UpdateAdjacentCells(initialRoom, halfGridSize, Quaternion.identity);

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

        CompleteGeneration();
    }

    private void CompleteGeneration()
    {
        isGenerating = false;
        OnGenerationComplete?.Invoke();
    }

    public void StartGeneration()
    {
        if (!isGenerating)
        {
            StartCoroutine(GenerateDungeon());
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

    public bool TryPlaceRoomAtCell(Vector2Int targetCell, List<Vector2Int> requiredConnections)
    {
        List<GameObject> shuffledPrefabs = new List<GameObject>(roomPrefabs);
        ShuffleList(shuffledPrefabs);

        foreach (GameObject prefab in shuffledPrefabs)
        {
            Room roomRef = prefab.GetComponent<Room>();
            Room roomTemplate; 
            roomTemplates.TryGetValue(prefab, out roomTemplate);
            
            if (roomRef == null)
            {
                Debug.Log("No room template found");
                continue;
            }
            List<int> rotationAngles = new List<int> { 0, 90, 180, 270 };
            ShuffleList(rotationAngles);

            foreach (int angle in rotationAngles)
            {
                Quaternion rot = Quaternion.Euler(0, angle, 0);

                if (GridManager.Instance.CanPlaceRoom(roomRef, targetCell, rot))
                {
                    foreach (Vector2Int connection in GridManager.Instance.grid[targetCell.x, targetCell.y].availableConnections)
                    {
                        Vector2Int invertedConnection = new Vector2Int(-connection.x, -connection.y);

                        foreach (Room.DoorInfo door in roomTemplate.doors)
                        {
                            Vector2Int rotatedDirection = RotateDirection(door.direction, angle);
                            if (invertedConnection == rotatedDirection)
                            {
                                Vector2Int offset = (invertedConnection * new Vector2Int(Mathf.FloorToInt(roomTemplate.size.x / 2) , Mathf.FloorToInt(roomTemplate.size.y / 2) ));
                                Room placedRoom = SpawnRoom(prefab, targetCell+ offset, rot, connection);
                                UpdateAdjacentCells(placedRoom, targetCell, rot);
                                return true;
                               
                            }
                        }

                    }
                    Debug.Log("first angle didnt work");
                }
            }
        }
        return false;
    }

    private Vector2Int RotateDirection(Vector2Int direction, int angle)
    {
        int rotationSteps = (angle / 90) % 4;
        Vector2Int rotatedDirection = direction;

        for (int i = 0; i < rotationSteps; i++)
        {
            rotatedDirection = new Vector2Int(-rotatedDirection.y, rotatedDirection.x);
        }

        return rotatedDirection;
    }
    public Room SpawnRoom(GameObject prefab, Vector2Int gridPos, Quaternion rotation, Vector2Int doorConnectionDirection)
    {
        doorConnectionDirection = new Vector2Int(-doorConnectionDirection.x, doorConnectionDirection.y);
        Room roomTemplate = prefab.GetComponent<Room>();
        Vector2Int effectiveSize = roomTemplate.GetEffectiveSize(rotation);

        Vector2Int offset = new Vector2Int(Mathf.CeilToInt((doorConnectionDirection.x * effectiveSize.x) / 2), Mathf.CeilToInt((doorConnectionDirection.y * effectiveSize.y) / 2));

        Vector2Int bottomLeftCell = new Vector2Int(
            gridPos.x - Mathf.FloorToInt(effectiveSize.x / 2f),
            gridPos.y - Mathf.FloorToInt(effectiveSize.y / 2f)
        );

        Vector3 worldPosition = GridManager.Instance.ConvertToWorldPosition(bottomLeftCell) +
                                  new Vector3((effectiveSize.x * GridManager.Instance.cellSize) / 2f, 0,
                                              (effectiveSize.y * GridManager.Instance.cellSize) / 2f);

        GameObject roomObj = Instantiate(prefab, worldPosition, rotation,transform);
        Room room = roomObj.GetComponent<Room>();

        room.InitializeDoors();
        placedRooms.Add(room);

        GridManager.Instance.OccupyCells(room, bottomLeftCell, rotation);

        return room;
    }

    void UpdateAdjacentCells(Room room, Vector2Int cellPosition, Quaternion rotation)
    {
        foreach (Room.DoorInfo door in room.doors)
        {
            GridManager.Instance.MarkAdjacentCellsAsAvailable(cellPosition, door.direction,room.size);
        }
    }
}