
using System;
using System.Collections.Generic;
using UnityEditor;
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
    public enum CellState { Unoccupied, Available, Occupied, SecondPass }
    public CellData[,] grid;
    
    public bool ShowGizmos = true;
    protected override void Awake()
    {
        base.Awake();
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
    }


    public Vector3 ConvertToWorldPosition(Vector2Int gridPosition)
    {
        float offsetX = -(gridSize.x * cellSize) / 2f;
        float offsetZ = -(gridSize.y * cellSize) / 2f;

        Vector3 worldPos = new Vector3(
            gridPosition.x * cellSize + offsetX,
            0,
            gridPosition.y * cellSize + offsetZ
        );
        return worldPos;
    }

    public Vector2Int ConvertToGridPosition(Vector3 worldPosition)
    {
        float offsetX = -(gridSize.x * cellSize) / 2f;
        float offsetZ = -(gridSize.y * cellSize) / 2f;

        Vector2Int gridPos = new Vector2Int(
            Mathf.FloorToInt((worldPosition.x - offsetX) / cellSize),
            Mathf.FloorToInt((worldPosition.z - offsetZ) / cellSize)
        );
        return gridPos;
    }

    public void MarkAdjacentCellsAsAvailable(Vector2Int cell, Vector2Int doorDirection, Vector2Int effectiveSize)
    {
        Vector2Int offset = (doorDirection * new Vector2Int(Mathf.FloorToInt(effectiveSize.x / 2)+1, Mathf.FloorToInt(effectiveSize.y / 2)+1));
        Vector2Int targetCell = cell + offset;
        if (IsValidCell(targetCell))
        {
            if (grid[targetCell.x, targetCell.y].state != CellState.Occupied && grid[targetCell.x, targetCell.y].state != CellState.SecondPass)
            {
                grid[targetCell.x, targetCell.y].state = CellState.Available; 
                if (!grid[targetCell.x, targetCell.y].availableConnections.Contains(-doorDirection))
                {
                    grid[targetCell.x, targetCell.y].availableConnections.Add(-doorDirection);
                }
            }
            else
            {
               // grid[targetCell.x, targetCell.y].state = CellState.SecondPass;
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
                if (grid[gridPosition.x + x, gridPosition.y + y].state == CellState.Occupied || grid[gridPosition.x + x, gridPosition.y + y].state == CellState.SecondPass)
                {
                    Debug.LogWarning($"[GridManager] Cannot place room {room.gameObject.name} due to occupied cell at ({gridPosition.x + x}, {gridPosition.y + y}).");
                    return false;
                }
            }
        }
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
            if (newState != CellState.Available || newState != CellState.SecondPass)
            {
                currentCell.availableConnections.Clear();
            }
            grid[cell.x, cell.y] = currentCell;
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
        if (grid == null || !ShowGizmos)
            return;

        // Loop through our grid cells.
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 bottomLeftPos = ConvertToWorldPosition(gridPos);
                Vector3 centerPos = bottomLeftPos + new Vector3(cellSize / 2f, 0, cellSize / 2f);

                switch (grid[x, y].state)
                {
                    case CellState.Available:
                        Gizmos.color = new Color(0, 0, 1, 0.3f); 
                        break;
                    case CellState.Occupied:
                        Gizmos.color = new Color(1, 0, 0, 0.3f); 
                        break;
                    case CellState.SecondPass:
                        Gizmos.color = new Color(1, 0, 1, 0.3f);
                        break;

                    default:
                        Gizmos.color = new Color(0, 1, 0, 0.3f);
                        break;
                }

                Gizmos.DrawCube(centerPos, new Vector3(cellSize, 1f, cellSize));

                if (grid[x, y].availableConnections != null &&
                    grid[x, y].availableConnections.Count > 0)
                {
                    Gizmos.color = Color.yellow;
                    foreach (Vector2Int connection in grid[x, y].availableConnections)
                    {
                        Vector3 directionVector = new Vector3(connection.x, 0, connection.y).normalized;

                        Vector3 connectionEnd = centerPos + directionVector * (cellSize * 0.5f);

                        Gizmos.DrawLine(centerPos, connectionEnd);

                        Gizmos.DrawSphere(connectionEnd, 0.1f);
                    }
                }
            }
        }
    }


}