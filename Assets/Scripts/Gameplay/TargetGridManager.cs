using UnityEngine;
using System.Collections.Generic; // Needed for List

public class TargetGridManager : MonoBehaviour
{
    [Header("Grid Dimensions")]
    public int rows = 12;
    public int columns = 5;

    [Header("Cell Settings")]
    public float cellWidth = 1.2f;
    public float cellHeight = 0.9f;

    [Header("Grid Offset")]
    public Vector2 gridOrigin = new Vector2(0f, 4.5f); // top-center of Area 1

    [Header("Debug")]
    public bool showGridGizmos = true;

    private bool[,] gridOccupied;

    public void InitializeGrid()
    {
        gridOccupied = new bool[columns, rows];
        ClearGrid();
    }

    public void ClearGrid()
    {
        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                gridOccupied[col, row] = false;
            }
        }
    }

    public void MarkCellOccupied(int col, int row, bool occupied)
    {
        if (col < 0 || col >= columns || row < 0 || row >= rows) return;
        gridOccupied[col, row] = occupied;
    }

    public bool IsCellOccupied(int col, int row)
    {
        if (col < 0 || col >= columns || row < 0 || row >= rows) return true;
        return gridOccupied[col, row];
    }

    public Vector2 GetWorldPosition(int col, int row)
    {
        float startX = -(columns - 1) * 0.5f * cellWidth;
        float x = startX + col * cellWidth;
        float y = gridOrigin.y - row * cellHeight;
        return new Vector2(x, y);
    }

    public int GetColumnCount() => columns;
    public int GetRowCount() => rows;

    // 🔥 New simple debug helper
    [System.Obsolete]
    public void AnnounceAvailableSpacesInRow(int rowIndex)
    {
        Debug.Log("AnnounceAvailableSpacesInRow");
        List<int> availableColumns = new List<int>();

        for (int col = 0; col < columns; col++)
        {
            if (!IsCellOccupied(col, rowIndex))
                availableColumns.Add(col + 1); // Player-facing numbering (1–5)
        }

        if (availableColumns.Count > 0)
        {
            string colList = string.Join(", ", availableColumns);
            Debug.Log($"[TargetGridManager] Move {FindObjectOfType<GameManager>()?.GetMoveCount() ?? -1} — Available spaces in Row {rowIndex + 1}: Columns {colList}");
        }
        else
        {
            Debug.Log($"[TargetGridManager] Move {FindObjectOfType<GameManager>()?.GetMoveCount() ?? -1} — No available spaces in Row {rowIndex + 1}");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGridGizmos || !Application.isPlaying) return;

        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                Vector2 pos = GetWorldPosition(col, row);
                bool occupied = gridOccupied != null && gridOccupied[col, row];

                Gizmos.color = occupied ? new Color(1f, 0.5f, 0.5f, 0.75f) : new Color(0.3f, 0.9f, 1f, 0.5f);
                Gizmos.DrawWireCube(pos, new Vector3(cellWidth * 0.9f, cellHeight * 0.9f, 0));
            }
        }
    }
#endif

    public List<int> GetAvailableColumnsInRow(int rowIndex)
    {
        List<int> availableCols = new List<int>();

        for (int col = 0; col < columns; col++)
        {
            if (!IsCellOccupied(col, rowIndex))
            {
                availableCols.Add(col);
            }
        }

        return availableCols;
    }

    // Convert world position to grid column and row (returns false if out of bounds)
    public bool GetGridCoordinates(Vector2 worldPos, out int col, out int row)
    {
        float startX = -(columns - 1) * 0.5f * cellWidth;

        col = Mathf.RoundToInt((worldPos.x - startX) / cellWidth);
        row = Mathf.RoundToInt((gridOrigin.y - worldPos.y) / cellHeight);

        if (col < 0 || col >= columns || row < 0 || row >= rows)
        {
            col = -1;
            row = -1;
            return false;
        }

        return true;
    }

    // Check if a cell is within grid bounds
    public bool IsCellInBounds(int col, int row)
    {
        return col >= 0 && col < columns && row >= 0 && row < rows;
    }

    // Placeholder: return the GameObject occupying the cell, if any (null if untracked)
    public GameObject GetTargetAt(int col, int row)
    {
        // TODO: Replace this with your actual target tracking system
        Vector2 worldPos = GetWorldPosition(col, row);
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        return hit != null ? hit.gameObject : null;
    }


}
