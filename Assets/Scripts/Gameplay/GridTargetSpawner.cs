// 8/26/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GridTargetSpawner : MonoBehaviour
{
    [Header("Target Prefabs (Assigned in Inspector)")]
    public List<GameObject> targetPrefabs;

    [Header("Grid Reference")]
    public TargetGridManager grid;

    [Header("DDA Health Curve Reference")]
    public TargetHealthCurve targetHealthCurve;

    [Header("Target Health Randomness")]
    [Range(0f, 1f)] public float healthRandomUpperPercent = 0.1f;
    [Range(0f, 1f)] public float healthRandomLowerPercent = 0.2f;

    [Header("Spawning Rules")]
    public int minTargetsPerRow = 2;
    public int maxTargetsPerRow = 4;
    [Range(0.1f, 1.5f)] public float targetScale = 0.5f;

    [Header("Target Offset & Rotate")]
    [Range(0f, 0.2f)] public float offsetXRange = 0.1f;
    [Range(0f, 0.2f)] public float offsetYRange = 0.1f;
    [Range(0f, 0.5f)] public float rotationZRange = 0.1f;

    [Header("Health Clamp")]
    public int maxHealthClamp = 40;

    [Header("Power-Up Manager")]
    public PowerUpManager powerUpManager;

    private Transform targetParent;
    private int lastEmptyColumn = -1;
    private int targetIdCounter = 0;
    private int spawnRowCounter = 0;

    private class TargetMeta
    {
        public Vector2 offset;
        public float rotationZ;
    }

    private Dictionary<GameObject, TargetMeta> targetMetaMap = new();

    private void Awake()
    {
        if (powerUpManager == null)
        {
            powerUpManager = FindObjectOfType<PowerUpManager>();
            if (powerUpManager == null)
            {
                Debug.LogError("PowerUpManager is not assigned and could not be found in the scene!");
            }
        }

        GameObject parentObj = GameObject.Find("Targets");
        targetParent = parentObj != null ? parentObj.transform : new GameObject("Targets").transform;
    }

    public void SpawnTargetsInArea(int areaIndex, int moveCount)
    {
        Debug.Log($"moveCount: {moveCount}, areaIndex: {areaIndex}");
        Debug.Log("SpawnTargetsInArea called");

        if (targetPrefabs == null || targetPrefabs.Count == 0) return;

        int columns = grid.GetColumnCount();
        int targetsToSpawn = Random.Range(minTargetsPerRow, maxTargetsPerRow + 1);

        List<int> columnIndices = new List<int>();
        for (int i = 0; i < columns; i++) columnIndices.Add(i);
        Shuffle(columnIndices);

        int maxAllowed = Mathf.Min(targetsToSpawn, columns - 1);
        List<int> selectedColumns = columnIndices.GetRange(0, maxAllowed);

        int emptyCol = columnIndices.Find(col => !selectedColumns.Contains(col));
        if (emptyCol == lastEmptyColumn)
            emptyCol = columnIndices.Find(col => col != lastEmptyColumn && !selectedColumns.Contains(col));

        lastEmptyColumn = emptyCol;

        int baseHealth;

        if (GameState.Instance.ContinueFromLastSave && GameState.Instance.SavedTargetHealth > 0)
        {
            int ddaHealth = targetHealthCurve != null ? targetHealthCurve.GetHealthForMove(moveCount) : 1;
            baseHealth = Mathf.Min(GameState.Instance.SavedTargetHealth + ddaHealth - 1, maxHealthClamp);
            Debug.Log($"📥 Using DDA adjusted saved health: {baseHealth} (Saved={GameState.Instance.SavedTargetHealth}, DDA={ddaHealth})");
        }
        else
        {
            baseHealth = targetHealthCurve != null ? targetHealthCurve.GetHealthForMove(moveCount) : 1;
        }

        for (int i = 0; i < maxAllowed; i++)
        {
            int col = selectedColumns[i];
            Vector2 basePos = grid.GetWorldPosition(col, areaIndex);

            Vector2 offset = new Vector2(
                Random.Range(-offsetXRange, offsetXRange),
                Random.Range(-offsetYRange, offsetYRange)
            );
            float rotation = Random.Range(-rotationZRange, rotationZRange) * 360f;

            Vector2 spawnPos = basePos + offset;
            Quaternion rot = Quaternion.Euler(0, 0, rotation);

            GameObject prefab = targetPrefabs[Random.Range(0, targetPrefabs.Count)];
            GameObject newTarget = Instantiate(prefab, spawnPos, rot, targetParent);
            newTarget.name = $"Target_{targetIdCounter:D4}";
            targetIdCounter++;

            newTarget.transform.localScale = Vector3.one * targetScale;

            var anim = newTarget.GetComponent<TargetBehavior>();
            if (anim != null)
            {
                int finalHealth = CalculateTargetHealth(baseHealth, moveCount);
                anim.SetHealth(finalHealth);
                Debug.Log($"🧬 SetHealth({finalHealth}) on {newTarget.name}");
                anim.AnimateToPosition(spawnPos, 0.5f, fromEndzone: true);
            }

            targetMetaMap[newTarget] = new TargetMeta { offset = offset, rotationZ = rotation };
            AssignSortingOrderByRow(newTarget, spawnRowCounter, moveCount);
            grid.MarkCellOccupied(col, areaIndex, true);
        }

        spawnRowCounter++;
    }

    public void AdvanceAllTargetsAndSpawnNew(int dummyMoveCount)
    {
        int rows = grid.GetRowCount();
        int cols = grid.GetColumnCount();
        Dictionary<(int col, int row), GameObject> objectsToMove = new();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (!grid.IsCellOccupied(col, row)) continue;
                Vector2 pos = grid.GetWorldPosition(col, row);
                Collider2D hit = Physics2D.OverlapPoint(pos);
                if (hit && (hit.CompareTag("Target") || hit.CompareTag("PowerUp")))
                    objectsToMove[(col, row)] = hit.gameObject;
            }
        }

        grid.ClearGrid();

        foreach (var entry in objectsToMove)
        {
            int col = entry.Key.col;
            int newRow = entry.Key.row + 1;
            if (newRow >= rows)
            {
                Destroy(entry.Value);
                continue;
            }

            Vector2 baseNewPos = grid.GetWorldPosition(col, newRow);
            GameObject obj = entry.Value;

            if (targetMetaMap.TryGetValue(obj, out var meta))
            {
                baseNewPos += meta.offset;
                obj.transform.rotation = Quaternion.Euler(0, 0, meta.rotationZ);
            }

            var tb = obj.GetComponent<TargetBehavior>();
            var pum = obj.GetComponent<PowerUpMover>();

            if (tb != null)
                tb.AnimateToPosition(baseNewPos, 0.5f, false);
            else if (pum != null)
                pum.AnimateToPosition(baseNewPos, 0.5f, false);

            grid.MarkCellOccupied(col, newRow, true);

            if (newRow == 8)
            {
                Debug.Log($"<color=red>⚠️ {obj.name} reached Area 8!</color>");
                SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.material = new Material(sr.material);
                OutlineBlinker blinker = obj.GetComponentInChildren<OutlineBlinker>();
                if (blinker != null) blinker.enabled = true;
            }
        }

        SpawnTargetsInArea(0, dummyMoveCount);
        powerUpManager.TrySpawnPowerUp(dummyMoveCount); // Delegate power-up spawning to PowerUpManager
        grid.AnnounceAvailableSpacesInRow(0);

        // Call the PowerUpManager to handle power-up spawning
        /*if (powerUpManager != null)
        {
            powerUpManager.TrySpawnPowerUp(dummyMoveCount);
        }
        else
        {
            Debug.LogError("PowerUpManager reference is missing!");
        }*/
    }

    private void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            (list[i], list[rnd]) = (list[rnd], list[i]);
        }
    }

    private int CalculateTargetHealth(int baseHealth, int moveCount)
    {
        bool isSpikeMove = (moveCount % 10 == 0);
        if (isSpikeMove)
            return baseHealth;

        int upperRange = Mathf.CeilToInt(baseHealth * healthRandomUpperPercent);
        int lowerRange = Mathf.CeilToInt(baseHealth * healthRandomLowerPercent);

        int min = Mathf.Max(1, baseHealth - lowerRange);
        int max = Mathf.Min(baseHealth + upperRange, maxHealthClamp);

        return Random.Range(min, max + 1);
    }

    public void AssignSortingOrderByRow(GameObject target, int rowIndex, int moveNumber, int baseOrder = 1000)
    {
        int sortingOrder = baseOrder - rowIndex;

        SortingGroup sg = target.GetComponent<SortingGroup>();
        if (sg != null)
        {
            sg.sortingLayerName = "foreground";
            sg.sortingOrder = sortingOrder;
        }

        TargetBehavior tb = target.GetComponent<TargetBehavior>();
        if (tb != null)
            tb.SetCanvasSortingOrder(sortingOrder + 1);
    }

    public void ResetSpawnRowCounter()
    {
        spawnRowCounter = 0;
        Debug.Log("🔄 Target sorting row counter reset.");
    }
}