
using System.Collections.Generic;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    [Header("References")]
    public TargetGridManager gridTargetManager;      // Assign in Inspector
    public Transform powerUpParent;                 // Container for all power-ups
    [SerializeField] private GameManager gameManager; // Assign in Inspector
    [SerializeField] private BulletPool bulletPool;

    [Header("Power-Ups")]
    public List<PowerUpData> powerUps; // List of power-up ScriptableObjects

    [Header("VFX / SFX Settings")]
    public Vector2 vfxOffset = new Vector2(0f, 0.9f);

    private int totalTargetSpawnCycles = 0;
    private bool hasSpawnedPowerUpThisMove = false; // Tracks if a power-up has already spawned this move

    private PowerUpData lastAlternatedPowerUp = null;

    private void Awake()
    {
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


    private PowerUpData SelectPowerUp(int move)
    {
        PowerUpData selectedPowerUp = null;
        float currentTime = Time.time; // Use Unity's time to track cooldowns

        // Sort power-ups by priority (1 = highest priority, larger numbers = lower priority)
        powerUps.Sort((a, b) => a.priority.CompareTo(b.priority));

        foreach (var powerUp in powerUps)
        {
            // Check if the power-up is eligible
            if (powerUp.IsEligible(move, bulletPool.GetTotalBulletCount(), bulletPool.GetEnabledBulletCount(), gameManager.GetScore(), GameState.Instance.LevelNumber))
            {
                // Check cooldown
                if (powerUp.cooldown > 0 && currentTime < powerUp.LastUsedTime + powerUp.cooldown)
                {
                    continue; // Skip if the power-up is still on cooldown
                }

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
                powerUp.LastUsedTime = currentTime; // Update the last used time for cooldown tracking
                break; // Stop at the first eligible power-up
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