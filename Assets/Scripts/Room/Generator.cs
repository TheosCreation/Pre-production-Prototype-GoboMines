using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Generator;
using Random = UnityEngine.Random;
public class Generator : MonoBehaviour
{
    public List<GameObject> roomPrefabs;
    public GameObject initialRoomPrefab;
    public int maxRooms = 10;
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

    private bool isGenerating = false;

    void Start()
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

    // room templates to find door positions and stuff, speed reasons
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

            // First Pass: use normal chance
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
                Debug.Log("[Generator] First pass failed. Trying second pass that ignores spawn chance.");
                ShuffleList(availableCells);
                foreach (Vector2Int targetCell in availableCells)
                {
                    if (processedCells.Contains(targetCell))
                    {
                      //  continue;
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

            if (!roomPlaced)
            {
                Debug.Log("[Generator] No valid placements found even in second pass. Ending generation.");
                break;
            }
        }

        CompleteGeneration();
    }
    // on complete
    private void CompleteGeneration()
    {
        isGenerating = false;
        OnGenerationComplete?.Invoke();

        foreach (Door door in activeDoors)
        {
           // Instantiate(blockedDoorPrefab, door.position, door.direction);
        }
    }
    // starts gen courutine kinda useless 
    public void StartGeneration()
    {
        if (!isGenerating)
        {
            StartCoroutine(GenerateDungeon());
        }
    }
    // randomizing
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
    public bool TryPlaceRoomAtCell(Vector2Int targetCell, List<Vector2Int> requiredConnections, bool ignoreSpawnChance = false)
    {
        List<GameObject> candidatePrefabs = new List<GameObject>();

        if (!ignoreSpawnChance)
        {
            candidatePrefabs = roomPrefabs.Where(p => Random.value <= p.GetComponent<Room>().spawnChance).ToList();
        }
        else
        {
            candidatePrefabs = new List<GameObject>(roomPrefabs);
        }

        if (candidatePrefabs.Count == 0 && !ignoreSpawnChance)
        {
            return false;
        }

        ShuffleList(candidatePrefabs);
        Debug.Log($"[Generator] Room {targetCell}");
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
                    Debug.Log($"[Generator] cant place {roomRef}");
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
         //           Debug.Log($"[Generator] meets required {meetsRequired}");
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
            //    Debug.Log($"[Generator] allConnectionsFilled {allConnectionsFilled}");
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
                //    Debug.Log($"[Generator] rotatedDoorDir == connectingDoorDir {rotatedDoorDir == connectingDoorDir}");
                    if (rotatedDoorDir == connectingDoorDir)
                        continue;

                    Vector2Int doorTarget = GetDoorTargetCell(bottomLeftCell, effectiveSize, rotatedDoorDir);
                    if (GridManager.Instance.IsValidCell(doorTarget))
                    {
                        var cell = GridManager.Instance.grid[doorTarget.x, doorTarget.y];
                        //         Debug.Log($"[Generator] this check {(cell.state != GridManager.CellState.Unoccupied) && (cell.availableConnections == null || cell.availableConnections.Count == 0)}");
                            Debug.Log(prefab.name);
                            Debug.Log(GridManager.Instance.grid[targetCell.x, targetCell.y].availableConnections.Count);
                            Debug.Log(GridManager.Instance.grid[doorTarget.x, doorTarget.y].availableConnections.Count);
                        if (GridManager.Instance.grid[doorTarget.x, doorTarget.y].state == GridManager.CellState.Occupied &&
                                (GridManager.Instance.grid[targetCell.x, targetCell.y].availableConnections == null || GridManager.Instance.grid[targetCell.x, targetCell.y].availableConnections.Count <= 1))
                        {
                            
                            validPlacement = false;
                            break;
                        }
                    }
                }
                if (!validPlacement) { 
         
                    continue;
                }

                Vector2Int matchingConnection = availableConnections[0];

                Vector2Int reversedConnection = new Vector2Int(-matchingConnection.x, matchingConnection.y);
                Vector2Int offset = new Vector2Int(
                    Mathf.CeilToInt((reversedConnection.x * effectiveSize.x) / 2f),
                    Mathf.CeilToInt((reversedConnection.y * effectiveSize.y) / 2f)
                );

                Room placedRoom = SpawnRoom(prefab, targetCell, rot, matchingConnection);
                UpdateAdjacentCells(placedRoom, targetCell, rot);
                return true;
            }
        }
        return false;
    }
    // rotates rooms to place them
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

    // room spawning code
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

        new Vector2Int(gridPos.x - doorConnectionDirection.x, gridPos.y - doorConnectionDirection.y);

        Vector3 worldPosition = GridManager.Instance.ConvertToWorldPosition(bottomLeftCell) +
                                  new Vector3((effectiveSize.x * GridManager.Instance.cellSize) / 2f, 0,
                                              (effectiveSize.y * GridManager.Instance.cellSize) / 2f);

        GameObject roomObj = Instantiate(prefab, worldPosition, rotation, transform);
        Room room = roomObj.GetComponent<Room>();

        room.InitializeDoors();

        foreach (Room.DoorInfo door in room.doors)
        {
            Vector2Int rotatedDirection = RotateDirection(door.direction, (int)rotation.eulerAngles.y);
         //   AddNewDoor(gridPos, rotatedDirection);
        }

        placedRooms.Add(room);

        GridManager.Instance.OccupyCells(room, bottomLeftCell, rotation);

        return room;
    }
    private Vector2Int GetDoorTargetCell(Vector2Int bottomLeftCell, Vector2Int effectiveSize, Vector2Int doorDir)
    {
        Vector2Int roomCenter = bottomLeftCell + new Vector2Int(effectiveSize.x / 2, effectiveSize.y / 2);
        return roomCenter + doorDir;
    }

    // updates cells
    void UpdateAdjacentCells(Room room, Vector2Int cellPosition, Quaternion rotation)
    {
        foreach (Room.DoorInfo door in room.doors)
        {
            GridManager.Instance.MarkAdjacentCellsAsAvailable(cellPosition, door.direction, room.size);
        }
    }


}


