

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpData", menuName = "GhostGunn/PowerUpData")]
public class PowerUpData : ScriptableObject
{
    [Header("Core Settings")]
    public string powerUpName;
    public GameObject powerUpPrefab;
    public int priority = 1;

    [Header("Cooldown Settings")]
    public int cooldown = 1;

    [Header("Other Settings")]
    public int spawnAfterMove = 0;
    public int requiredLevel = 0;
    public int requiredScore = 0;
    public GameObject pickupVFX;
    public string pickupSFX = "PUCollect";
    [Range(0f, 1f)] public float probability = 1.0f;
    public bool alternateWithPrevious = false;
    public bool requiresMaxBullets = false;
    public bool stopAfterMaxBullets = false;

    [Header("Special Settings")]
    public bool spawnOnlyOnce = false; // New property to track if the power-up should spawn only once

    [HideInInspector] public int elapsedMoves = 0;

    /// <summary>
    /// Resets the runtime state of the power-up.
    /// </summary>
    public void ResetRuntimeState()
    {
        Debug.Log($"[PowerUpData] ResetRuntimeState called");
        elapsedMoves = cooldown; // Initialize elapsedMoves to cooldown
    }

    /// <summary>
    /// Determines if the power-up is eligible to spawn based on the current game state.
    /// </summary>
    public bool IsEligible(int currentMove, int totalBulletCount, int enabledBulletCount, int currentScore, int currentLevel, HashSet<string> spawnedOncePowerUps)
    {
        // Check if the power-up should spawn only once and has already been spawned
        if (spawnOnlyOnce && spawnedOncePowerUps.Contains(powerUpName))
        {
            Debug.Log($"[PowerUpData] {powerUpName} has already been spawned and is marked as spawnOnlyOnce.");
            return false;
        }

        // Check cooldown
        if (elapsedMoves > 0)
        {
            elapsedMoves--; // Decrement elapsedMoves
            Debug.Log($"[PowerUpData] {powerUpName} is on cooldown. ElapsedMoves: {elapsedMoves}");
            return false;
        }

        // Check spawn after move
        if (currentMove < spawnAfterMove) return false;

        // Check level requirement
        if (requiredLevel > 0 && currentLevel != requiredLevel) return false;

        // Check score requirement
        if (requiredScore > 0 && currentScore < requiredScore) return false;

        // Check probability
        if (Random.value > probability) return false;

        // Check if max bullets are required
        if (requiresMaxBullets && totalBulletCount != enabledBulletCount) return false;

        // Stop spawning after max bullets reached
        if (stopAfterMaxBullets && totalBulletCount == enabledBulletCount) return false;

        return true;
    }
}