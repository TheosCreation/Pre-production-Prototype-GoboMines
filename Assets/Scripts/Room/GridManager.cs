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
    }

    public Vector3 ConvertToWorldPosition(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * cellSize, 0, gridPosition.y * cellSize);
    }

    public Vector2Int ConvertToGridPosition(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / cellSize),
            Mathf.FloorToInt(worldPosition.z / cellSize)
        );
    }

    public bool CanPlaceRoom(Room room, Vector2Int gridPosition, Quaternion rotation)
    {
        Vector2Int effectiveSize = room.GetEffectiveSize(rotation);
        if (gridPosition.x < 0 || gridPosition.y < 0 ||
            gridPosition.x + effectiveSize.x > gridSize.x ||
            gridPosition.y + effectiveSize.y > gridSize.y) return false;

        for (int x = 0; x < effectiveSize.x; x++)
            for (int y = 0; y < effectiveSize.y; y++)
                if (grid[gridPosition.x + x, gridPosition.y + y] == CellState.Occupied)
                    return false;
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
                Debug.Log($"Occupied cell: ({gridPosition.x + x}, {gridPosition.y + y})");
            }
        }
    }

    public void SetCellState(Vector2Int cell, CellState state)
    {
        if (cell.x >= 0 && cell.x < gridSize.x && cell.y >= 0 && cell.y < gridSize.y)
        {
            grid[cell.x, cell.y] = state;
            Debug.Log($"Set cell ({cell.x}, {cell.y}) to {state}");
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
                Vector3 pos = ConvertToWorldPosition(new Vector2Int(x, y));
                Gizmos.DrawWireCube(pos, new Vector3(cellSize, 0.1f, cellSize));
            }
        }
    }
}