using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static GridManager;
using Random = UnityEngine.Random;

public class Generator : MonoBehaviour
{
    public List<GameObject> roomPrefabs;
    public Room initialRoom;
    public int maxRooms = 10;   
    private int currentMaxRooms = 10;   
    public int roomsIncreaseFactor = 20;   
    public float placementDelay = 0.5f;

    public bool useSeed = false;
    public int seed = 0;
    public int currentSeed = 0;
    public GameObject blockedDoorPrefab;

    private List<Room> placedRooms = new List<Room>();
    private int currentRoomCount = 0;
    private HashSet<Vector2Int> processedCells = new HashSet<Vector2Int>();
    private Dictionary<GameObject, Room> roomTemplates = new Dictionary<GameObject, Room>();

    public delegate void GenerationCompleteHandler();
    public event GenerationCompleteHandler OnGenerationComplete;
    public delegate void GenerationResetHandler();
    public event GenerationCompleteHandler OnGenerationReset;
    List<int> rotationAngles = new List<int> { 0, 90, 180, 270 };

    [System.Serializable]
    public struct Door
    {
        public Vector3 position;
        public Quaternion direction;

        public Door(Vector3 position, Quaternion direction)
        {
            this.position = position;
            this.direction = direction;
        }
    }
    private List<Door> activeDoors = new List<Door>();

    public bool isGenerating = false;

    public bool regenerateDungeon = false;

    private void Start()
    {
        currentMaxRooms = maxRooms;
        GameManager.Instance.onHostEvent.AddListener(Init);
    }

    private void Update()
    {
        // Check the flag every frame (or use another mechanism to trigger regeneration).
        if (regenerateDungeon)
        {
            regenerateDungeon = false;
            ResetDungeon();
        }
    }

    public void Init()
    {
        if (useSeed)
        {
            Random.InitState(seed);
            currentSeed = seed;
        }
        else
        {
            int randomSeed = (int)System.DateTime.Now.Ticks;
            currentSeed = randomSeed;
            Random.InitState(randomSeed);
        }
        InitializeRoomTemplates();
        StartCoroutine(GenerateDungeon());
    }

    // Room templates to find door positions and for speed reasons
    private void InitializeRoomTemplates()
    {
        foreach (GameObject prefab in roomPrefabs)
        {
            GameObject templateObject = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            templateObject.SetActive(false);
            DontDestroyOnLoad(templateObject);

            Room roomComponent = templateObject.GetComponent<Room>();
            roomComponent.InitializeRoom(false);
            roomTemplates[prefab] = roomComponent;
        }
    }

    IEnumerator GenerateDungeon()
    {
        isGenerating = true;

        Vector2Int halfGridSize = new Vector2Int(GridManager.Instance.gridSize.x / 2, GridManager.Instance.gridSize.y / 2);
        Vector2Int effectiveSize = initialRoom.GetEffectiveSize(Quaternion.identity);
        Vector2Int bottomLeftCell = new Vector2Int(
            halfGridSize.x - Mathf.FloorToInt(effectiveSize.x / 2f),
            halfGridSize.y - Mathf.FloorToInt(effectiveSize.y / 2f)
        );

        InitilizeRoomAndGrid(initialRoom, bottomLeftCell, Quaternion.identity);

        processedCells.Add(halfGridSize);

        yield return new WaitForSeconds(placementDelay);
        UpdateAdjacentCells(initialRoom, halfGridSize, Quaternion.identity);

        List<Vector2Int> availableCells = new List<Vector2Int>();
        bool roomPlaced = false;
        while (currentRoomCount < currentMaxRooms)
        {
            availableCells = GridManager.Instance.GetAvailableCells();
            if (availableCells.Count == 0)
            {
                Debug.Log("[Generator] No more available cells for room placement. Ending generation.");
                break;
            }

            ShuffleList(availableCells);
            roomPlaced = false;

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
                Debug.Log("[Generator] No valid placements found even in second pass. Ending generation.");
                break;
            }
        }

        while (availableCells.Count > 0)
        {
            availableCells = GridManager.Instance.GetAvailableCells();
            ShuffleList(availableCells);
            foreach (Vector2Int targetCell in availableCells)
            {
                if (processedCells.Contains(targetCell))
                {
                    // Optionally continue or allow a second pass
                }
                List<Vector2Int> requiredConnections = GridManager.Instance.GetCellConnections(targetCell);
                if (TryPlaceRoomAtCell(targetCell, requiredConnections, true))
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
        }

        CompleteGeneration();
    }

    // On complete
    private void CompleteGeneration()
    {
        isGenerating = false;
        OnGenerationComplete?.Invoke();
        UiManager.Instance.OpenPlayerHud();
    }

    // Starts gen coroutine if not already generating
    public void StartGeneration()
    {
        if (!isGenerating)
        {
            StartCoroutine(GenerateDungeon());
        }
    }

    // Randomizing list
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

    public bool TryPlaceRoomAtCell(Vector2Int targetCell, List<Vector2Int> requiredConnections, bool secondPass = false)
    {
        List<GameObject> candidatePrefabs = new List<GameObject>();

        if (!secondPass)
        {
            candidatePrefabs = roomPrefabs.Where(p => Random.value <= p.GetComponent<Room>().spawnChance).ToList();
        }
        else
        {
            candidatePrefabs = new List<GameObject>(roomPrefabs);
        }

        if (candidatePrefabs.Count == 0 && !secondPass)
        {
            return false;
        }

        ShuffleList(candidatePrefabs);

        foreach (GameObject prefab in candidatePrefabs)
        {
            Room roomRef = prefab.GetComponent<Room>();
            if (roomRef == null)
            {
                Debug.Log("No room component found on prefab: " + prefab.name);
                continue;
            }
            Room roomTemplate;
            roomTemplates.TryGetValue(prefab, out roomTemplate);
            if (secondPass)
            {
                if (roomTemplate.doors.Count != requiredConnections.Count)
                {
                    continue;
                }
            }
            if (roomTemplate == null)
            {
                Debug.Log("No room template found for prefab: " + prefab.name);
                continue;
            }
            ShuffleList(rotationAngles);
            foreach (int angle in rotationAngles)
            {
                Quaternion rot = Quaternion.Euler(0, angle, 0);
                if (!GridManager.Instance.CanPlaceRoom(roomRef, targetCell, rot))
                {
                    Debug.Log($"[Generator] Cannot place {roomRef}");
                    continue;
                }

                List<Vector2Int> availableConnections = GridManager.Instance.grid[targetCell.x, targetCell.y].availableConnections;
                List<Vector2Int> rotatedDoorDirections = new List<Vector2Int>();
                foreach (Room.DoorInfo door in roomTemplate.doors)
                {
                    rotatedDoorDirections.Add(RotateDirection(door.direction, angle));
                }

                bool meetsRequired = true;
                if (requiredConnections != null && requiredConnections.Count > 0)
                {
                    foreach (Vector2Int req in requiredConnections)
                    {
                        if (!rotatedDoorDirections.Contains(req))
                        {
                            meetsRequired = false;
                            break;
                        }
                    }
                }
                if (!meetsRequired)
                {
                    continue;
                }

                bool allConnectionsFilled = true;
                foreach (Vector2Int connection in availableConnections)
                {
                    if (!rotatedDoorDirections.Contains(connection))
                    {
                        allConnectionsFilled = false;
                        break;
                    }
                }
                if (!allConnectionsFilled)
                {
                    continue;
                }

                Vector2Int effectiveSize = roomTemplate.GetEffectiveSize(rot);
                Vector2Int bottomLeftCell = new Vector2Int(
                    targetCell.x - Mathf.FloorToInt(effectiveSize.x / 2f),
                    targetCell.y - Mathf.FloorToInt(effectiveSize.y / 2f)
                );
                Vector2Int connectingDoorDir = availableConnections[0];

                bool validPlacement = true;
                foreach (Room.DoorInfo door in roomTemplate.doors)
                {
                    Vector2Int rotatedDoorDir = RotateDirection(door.direction, angle);
                    if (rotatedDoorDir == connectingDoorDir)
                        continue;

                    Vector2Int doorTarget = GetDoorTargetCell(bottomLeftCell, effectiveSize, rotatedDoorDir);
                    if (GridManager.Instance.IsValidCell(doorTarget))
                    {
                        CellData targetCellData = GridManager.Instance.grid[doorTarget.x, doorTarget.y];

                        if (targetCellData.state == GridManager.CellState.Occupied && !secondPass)
                        {
                            bool hasMatchingConnection = false;
                            if (targetCellData.availableConnections != null)
                            {
                                Vector2Int oppositeDirection = new Vector2Int(-door.direction.x, -door.direction.y);
                                hasMatchingConnection = targetCellData.availableConnections.Contains(oppositeDirection);
                            }

                            if (!hasMatchingConnection)
                            {
                                validPlacement = false;
                                break;
                            }
                        }
                    }
                }
                if (!validPlacement)
                {
                    continue;
                }

                Vector2Int matchingConnection = availableConnections[0];

                Room placedRoom = SpawnRoom(prefab, targetCell, rot, matchingConnection);
                UpdateAdjacentCells(placedRoom, targetCell, rot);
                return true;
            }
        }
        return false;
    }

    // Rotates room door directions to match orientation
    private Vector2Int RotateDirection(Vector2Int direction, int angle)
    {
        int rotationSteps = (angle / 90) % 4;
        Vector2Int rotatedDirection = direction;

        for (int i = 0; i < rotationSteps; i++)
        {
            rotatedDirection = new Vector2Int(rotatedDirection.y, -rotatedDirection.x);
        }

        return rotatedDirection;
    }

    // Room spawning code
    public Room SpawnRoom(GameObject prefab, Vector2Int gridPos, Quaternion rotation, Vector2Int doorConnectionDirection)
    {
        doorConnectionDirection = new Vector2Int(-doorConnectionDirection.x, doorConnectionDirection.y);
        Room roomTemplate = prefab.GetComponent<Room>();
        Vector2Int effectiveSize = roomTemplate.GetEffectiveSize(rotation);

        Vector2Int offset = new Vector2Int(Mathf.CeilToInt((doorConnectionDirection.x * effectiveSize.x) / 2f),
                                           Mathf.CeilToInt((doorConnectionDirection.y * effectiveSize.y) / 2f));

        Vector2Int bottomLeftCell = new Vector2Int(
            gridPos.x - Mathf.FloorToInt(effectiveSize.x / 2f),
            gridPos.y - Mathf.FloorToInt(effectiveSize.y / 2f)
        );

        Vector3 worldPosition = GridManager.Instance.ConvertToWorldPosition(bottomLeftCell) +
                                  new Vector3((effectiveSize.x * GridManager.Instance.cellSize) / 2f, 0,
                                              (effectiveSize.y * GridManager.Instance.cellSize) / 2f);

        GameObject roomObj = Instantiate(prefab, worldPosition, rotation);
        Room room = roomObj.GetComponent<Room>();

        roomObj.GetComponent<NetworkObject>().Spawn(true);
        roomObj.transform.parent = this.transform;

        InitilizeRoomAndGrid(room, bottomLeftCell, rotation);

        return room;
    }

    public void InitilizeRoomAndGrid(Room room, Vector2Int bottomLeftCell, Quaternion roomRotation)
    {
        room.InitializeRoom(true);
        placedRooms.Add(room);
        GridManager.Instance.OccupyCells(room, bottomLeftCell, roomRotation);
    }

    private Vector2Int GetDoorTargetCell(Vector2Int bottomLeftCell, Vector2Int effectiveSize, Vector2Int doorDir)
    {
        Vector2Int roomCenter = bottomLeftCell + new Vector2Int(effectiveSize.x / 2, effectiveSize.y / 2);
        return roomCenter + doorDir;
    }

    // Updates cells adjacent to the room's doors
    void UpdateAdjacentCells(Room room, Vector2Int cellPosition, Quaternion rotation)
    {
        foreach (Room.DoorInfo door in room.doors)
        {
            GridManager.Instance.MarkAdjacentCellsAsAvailable(cellPosition, door.direction, room.size);
        }
    }

    public void ResetDungeon()
    {
        OnGenerationReset?.Invoke();
        currentMaxRooms = maxRooms + (GameManager.Instance.day * roomsIncreaseFactor);
        foreach (Room room in placedRooms)
        {
            if(room == initialRoom)
            {
               continue;
            }
            if (room != null)
            {
                NetworkObject netObj = room.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Despawn(true);
                }
                room.DeleteOres();
                Destroy(room.gameObject);
            }
        }
        placedRooms.Clear();

        GridManager.Instance.InitializeGrid();

        int newSeed = (int)System.DateTime.Now.Ticks;
        currentSeed = newSeed;
        Random.InitState(newSeed);

        currentRoomCount = 0;
        processedCells.Clear();

        StartCoroutine(GenerateDungeon());
    }
}