// 9/22/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

// 9/22/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System.Collections.Generic;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The TargetGridManager responsible for managing the grid where power-ups spawn.")]
    public TargetGridManager gridTargetManager;

    [Tooltip("The parent transform under which all spawned power-ups will be organized.")]
    public Transform powerUpParent;

    [Tooltip("The GameManager instance to track game state and score.")]
    [SerializeField] private GameManager gameManager;

    [Tooltip("The BulletPool instance to track bullet-related conditions.")]
    [SerializeField] private BulletPool bulletPool;

    [Header("Power-Ups")]
    [Tooltip("List of all power-up ScriptableObjects available for spawning.")]
    public List<PowerUpData> powerUps;

    [Header("VFX / SFX Settings")]
    [Tooltip("Offset for the visual effects when a power-up is picked up.")]
    public Vector2 vfxOffset = new Vector2(0f, 0.9f);

    private int totalTargetSpawnCycles = 0;

    [Tooltip("Tracks if a power-up has already been spawned during the current move.")]
    private bool hasSpawnedPowerUpThisMove = false;

    [Tooltip("Tracks the last alternated power-up to ensure alternation logic.")]
    private PowerUpData lastAlternatedPowerUp = null;

    private void Awake()
    {
        ResetHasBeenUsed();

        if (gridTargetManager == null)
        {
            Debug.LogError("[PowerUpManager] TargetGridManager is not assigned!");
        }

        if (gameManager == null)
        {
            Debug.LogError("[PowerUpManager] GameManager is not assigned!");
        }

        if (bulletPool == null)
        {
            Debug.LogError("[PowerUpManager] BulletPool is not assigned!");
        }
    }

    public void TrySpawnPowerUp(int move)
    {
        if (powerUps == null || powerUps.Count == 0)
        {
            Debug.LogWarning("[PowerUpManager] No power-ups assigned.");
            return;
        }

        if (bulletPool == null)
        {
            Debug.LogWarning("[PowerUpManager] BulletPool is not found.");
            return;
        }

        // Reset the flag for this move
        hasSpawnedPowerUpThisMove = false;

        List<int> availableCols = gridTargetManager.GetAvailableColumnsInRow(0);
        if (availableCols == null || availableCols.Count == 0)
        {
            Debug.LogWarning("[PowerUpManager] No available columns for power-up spawn.");
            return;
        }

        PowerUpData selectedPU = SelectPowerUp(move);
        if (selectedPU == null || selectedPU.powerUpPrefab == null)
        {
            Debug.Log("[PowerUpManager] No eligible power-up selected.");
            return;
        }

        // Spawn the selected power-up
        SpawnPowerUp(selectedPU, availableCols, move);

        // Mark that a power-up has been spawned this move
        hasSpawnedPowerUpThisMove = true;
    }

    // 9/23/2025 AI-Tag
    // This was created with the help of Assistant, a Unity Artificial Intelligence product.

    // 9/23/2025 AI-Tag
    // This was created with the help of Assistant, a Unity Artificial Intelligence product.

    private PowerUpData SelectPowerUp(int move)
    {
        Debug.Log($"[PowerUpManager] Current Level: {GameState.Instance.CurrentLevel}");
        PowerUpData selectedPowerUp = null;

        // Sort power-ups by priority (1 = highest priority, larger numbers = lower priority)
        powerUps.Sort((a, b) => a.priority.CompareTo(b.priority));

        foreach (var powerUp in powerUps)
        {
            // Check if the power-up is eligible
            if (powerUp.IsEligible(move, bulletPool.GetTotalBulletCount(), bulletPool.GetEnabledBulletCount(), gameManager.GetScore(), GameState.Instance.LevelNumber))
            {
                // Special weapon logic
                if (powerUp.powerUpName == "NukePU" && GameState.Instance.GetNukeHasBeenUsed())
                {
                    continue; // Skip if the Nuke has already been used
                }

                if (powerUp.powerUpName == "FirePU" && GameState.Instance.GetFireHasBeenUsed())
                {
                    continue; // Skip if the Fire has already been used
                }

                // Ensure alternation based on alternateWithPrevious
                if (powerUp.alternateWithPrevious && lastAlternatedPowerUp == powerUp)
                {
                    continue; // Skip if this power-up alternates and was the last one used
                }

                // Select this power-up
                selectedPowerUp = powerUp;
                lastAlternatedPowerUp = powerUp; // Update the last alternated power-up

                // Reset cooldown for the selected power-up
                powerUp.elapsedMoves = powerUp.cooldown;

                Debug.Log($"[PowerUpManager] {powerUp.powerUpName} selected at move {move}. Cooldown reset to {powerUp.cooldown}.");
                break; // Stop at the first eligible power-up
            }
            else
            {
                Debug.Log($"[PowerUpManager] {powerUp.powerUpName} is NOT eligible at move {move}. ElapsedMoves: {powerUp.elapsedMoves}");
            }
        }

        // Mark special weapons as used
        if (selectedPowerUp != null && selectedPowerUp.powerUpName == "NukePU")
        {
            GameState.Instance.SetNukeHasBeenUsed(true);
        }
        else if (selectedPowerUp != null && selectedPowerUp.powerUpName == "FirePU")
        {
            GameState.Instance.SetFireHasBeenUsed(true);
        }

        return selectedPowerUp;
    }

    private void SpawnPowerUp(PowerUpData powerUpData, List<int> availableCols, int move)
    {
        int chosenCol = availableCols[Random.Range(0, availableCols.Count)];
        Vector2 spawnPos = gridTargetManager.GetWorldPosition(chosenCol, 0);

        GameObject newPU = Instantiate(powerUpData.powerUpPrefab, spawnPos, Quaternion.identity, powerUpParent);

        PowerUpMover mover = newPU.GetComponent<PowerUpMover>();
        if (mover != null)
        {
            mover.AnimateToPosition(spawnPos, 0.5f, fromEndzone: true);
        }

        gridTargetManager.MarkCellOccupied(chosenCol, 0, true);
        Debug.Log($"[PowerUpManager] Spawned: {powerUpData.powerUpName} — Move {move}, Column {chosenCol + 1}");
    }

    public void PlayPickupEffects(Vector2 position, PowerUpData powerUpData)
    {
        if (powerUpData == null) return;

        // Spawn VFX
        if (powerUpData.pickupVFX != null)
        {
            Vector2 vfxPos = position + vfxOffset;
            Vector3 worldPos = new Vector3(vfxPos.x, vfxPos.y, 0f);
            GameObject vfx = Instantiate(powerUpData.pickupVFX, worldPos, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        // Play SFX
        if (!string.IsNullOrEmpty(powerUpData.pickupSFX))
        {
            SFXManager.Instance.Play(powerUpData.pickupSFX, 0.5f, 0.9f, 1.1f);
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void DrawDebugMarker(Vector3 pos, Color color)
    {
#if UNITY_EDITOR
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = pos;
        marker.transform.localScale = Vector3.one * 0.2f;
        marker.GetComponent<Renderer>().material.color = color;
        marker.name = "💥 VFX Debug Marker";
        Destroy(marker, 2f); // Auto-destroy after 2 seconds
#endif
    }

    // Function to reset HasBeenUsed for all power-ups
    public void ResetHasBeenUsed()
    {
        foreach (var powerUp in powerUps)
        {
            powerUp.ResetRuntimeState(); // This will reset HasBeenUsed to false
        }
    }
}