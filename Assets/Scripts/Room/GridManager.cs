using System.Collections.Generic;
using UnityEngine;

public class GridManager : Singleton<GridManager>
{
    public Vector2Int gridSize = new Vector2Int(50, 50);
    public float cellSize = 5f;
    public enum CellState { Unoccupied, Available, Occupied }
    public CellState[,] grid;

    protected override void Awake()
    {
        base.Awake();
        Debug.Log("[GridManager] Initializing grid...");
        InitializeGrid();
    }

    void InitializeGrid()
    {
        grid = new CellState[gridSize.x, gridSize.y];
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                grid[x, y] = CellState.Unoccupied;
            }
        }
        Debug.Log($"[GridManager] Grid initialized with size {gridSize}. All cells set to Unoccupied.");
    }

    public Vector3 ConvertToWorldPosition(Vector2Int gridPosition)
    {
        Vector3 worldPos = new Vector3(
           gridPosition.x * cellSize,
           0,
           gridPosition.y * cellSize
       );
        Debug.Log($"[GridManager] Converted grid position {gridPosition} to world position {worldPos}.");
        return worldPos;
    }

    public Vector2Int ConvertToGridPosition(Vector3 worldPosition)
    {
        Vector2Int gridPos = new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / cellSize),
            Mathf.FloorToInt(worldPosition.z / cellSize)
        );
        Debug.Log($"[GridManager] Converted world position {worldPosition} to grid position {gridPos}.");
        return gridPos;
    }
    public void MarkAdjacentCellsAsAvailable(Vector2Int cell)
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int adjacentCell = cell + dir;
            if (adjacentCell.x >= 0 && adjacentCell.x < gridSize.x &&
                adjacentCell.y >= 0 && adjacentCell.y < gridSize.y &&
                grid[adjacentCell.x, adjacentCell.y] != CellState.Occupied)
            {
                SetCellState(adjacentCell, CellState.Available);
            }
        }
    }

    public List<Vector2Int> GetAvailableCells()
    {
        List<Vector2Int> availableCells = new List<Vector2Int>();
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (grid[x, y] == CellState.Available)
                {
                    availableCells.Add(new Vector2Int(x, y));
                }
            }
        }
        return availableCells;
    }

public bool CanPlaceRoom(Room room, Vector2Int gridPosition, Quaternion rotation)
    {
        Vector2Int effectiveSize = room.GetEffectiveSize(rotation);
        if (gridPosition.x < 0 || gridPosition.y < 0 ||
            gridPosition.x + effectiveSize.x > gridSize.x ||
            gridPosition.y + effectiveSize.y > gridSize.y)
        {
            Debug.LogWarning($"[GridManager] Room {room.gameObject.name} with size {effectiveSize} cannot be placed at {gridPosition}: Out of bounds.");
            return false;
        }

        for (int x = 0; x < effectiveSize.x; x++)
        {
            for (int y = 0; y < effectiveSize.y; y++)
            {
                if (grid[gridPosition.x + x, gridPosition.y + y] == CellState.Occupied)
                {
                    Debug.LogWarning($"[GridManager] Cannot place room {room.gameObject.name} due to occupied cell at ({gridPosition.x + x}, {gridPosition.y + y}).");
                    return false;
                }
            }
        }
        Debug.Log($"[GridManager] Room {room.gameObject.name} can be placed at {gridPosition} with size {effectiveSize}.");
        return true;
    }

    public void OccupyCells(Room room, Vector2Int gridPosition, Quaternion rotation)
    {
        Vector2Int effectiveSize = room.GetEffectiveSize(rotation);
        for (int x = 0; x < effectiveSize.x; x++)
        {
            for (int y = 0; y < effectiveSize.y; y++)
            {
                grid[gridPosition.x + x, gridPosition.y + y] = CellState.Occupied;
                Debug.Log($"[GridManager] Occupied cell ({gridPosition.x + x}, {gridPosition.y + y}) for room {room.gameObject.name}");
            }
        }
    }
    public void SetCellState(Vector2Int cell, CellState state)
    {
        Debug.Log("STATE IS "+ state + " IN " + cell);
        if (cell.x >= 0 && cell.x < gridSize.x && cell.y >= 0 && cell.y < gridSize.y)
        {
            grid[cell.x, cell.y] = state;
            Debug.Log($"[GridManager] Set cell ({cell.x}, {cell.y}) to state {state}.");
        }
        else
        {
            Debug.LogWarning($"[GridManager] Attempt to set cell state for out-of-bounds cell ({cell.x}, {cell.y}).");
        }
    }

    void OnDrawGizmos()
    {
        if (grid == null) return;
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                switch (grid[x, y])
                {
                    case CellState.Available:
                        Gizmos.color = Color.blue;
                        break;
                    case CellState.Occupied:
                        Gizmos.color = Color.red;
                        break;
                    default:
                        Gizmos.color = Color.green;
                        break;
                }
                Vector3 pos =(new Vector3(x*1.1f,0, y*1.1f));
                Gizmos.DrawCube(pos, new Vector3(cellSize, 0.1f, cellSize));
            }
        }
    }
}