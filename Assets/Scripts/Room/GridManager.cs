
using System;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : Singleton<GridManager>
{
    [Serializable]
    public struct CellData
    {
        public CellState state;
        public List<Vector2Int> availableConnections;
    }

    public Vector2Int gridSize = new Vector2Int(50, 50);
    public float cellSize = 5f;
    public enum CellState { Unoccupied, Available, Occupied }
    private CellData[,] grid;

    protected override void Awake()
    {
        base.Awake();
        Debug.Log("[GridManager] Initializing grid...");
        InitializeGrid();
    }

    void InitializeGrid()
    {
        grid = new CellData[gridSize.x, gridSize.y];
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                grid[x, y].state = CellState.Unoccupied;
                grid[x, y].availableConnections = new List<Vector2Int>();
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

    public void MarkAdjacentCellsAsAvailable(Vector2Int cell, Vector2Int doorDirection)
    {
        Vector2Int targetCell = cell + doorDirection;
        if (IsValidCell(targetCell))
        {
            if (grid[targetCell.x, targetCell.y].state != CellState.Occupied)
            {
                grid[targetCell.x, targetCell.y].state = CellState.Available;
                if (!grid[targetCell.x, targetCell.y].availableConnections.Contains(-doorDirection))
                {
                    grid[targetCell.x, targetCell.y].availableConnections.Add(-doorDirection);
                }
                Debug.Log($"[GridManager] Marked cell ({targetCell.x}, {targetCell.y}) as Available with connection {-doorDirection}.");
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
                if (grid[x, y].state == CellState.Available)
                {
                    availableCells.Add(new Vector2Int(x, y));
                }
            }
        }
        return availableCells;
    }

    public List<Vector2Int> GetCellConnections(Vector2Int cell)
    {
        if (IsValidCell(cell))
        {
            return grid[cell.x, cell.y].availableConnections;
        }
        return new List<Vector2Int>();
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
                if (grid[gridPosition.x + x, gridPosition.y + y].state == CellState.Occupied)
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
                grid[gridPosition.x + x, gridPosition.y + y].state = CellState.Occupied;
                grid[gridPosition.x + x, gridPosition.y + y].availableConnections.Clear();
                Debug.Log($"[GridManager] Occupied cell ({gridPosition.x + x}, {gridPosition.y + y}) for room {room.gameObject.name}");
            }
        }
    }

    public void SetCellState(Vector2Int cell, CellState newState)
    {
        if (IsValidCell(cell))
        {
            CellData currentCell = grid[cell.x, cell.y];

            if (currentCell.state == CellState.Available && newState == CellState.Unoccupied)
            {
                Debug.Log($"[GridManager] Preserving Available state for cell ({cell.x}, {cell.y})");
                return;
            }

            currentCell.state = newState;
            if (newState != CellState.Available)
            {
                currentCell.availableConnections.Clear();
            }
            grid[cell.x, cell.y] = currentCell;
            Debug.Log($"[GridManager] Set cell ({cell.x}, {cell.y}) to state {newState}.");
        }
        else
        {
            Debug.LogWarning($"[GridManager] Attempt to set cell state for out-of-bounds cell ({cell.x}, {cell.y}).");
        }
    }

    public bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < gridSize.x && cell.y >= 0 && cell.y < gridSize.y;
    }

    void OnDrawGizmos()
    {
        if (grid == null) return;
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                switch (grid[x, y].state)
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
                Vector3 pos = new Vector3(x * 1.1f, 0, y * 1.1f);
                Gizmos.DrawCube(pos, new Vector3(cellSize, 0.1f, cellSize));
            }
        }
    }
}