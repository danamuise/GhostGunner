// 8/27/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

[CreateAssetMenu(fileName = "SOtester", menuName = "GhostGunn/PowerUpData")]
public class SOtester : ScriptableObject
{
    [Header("Core Settings")]
    [Tooltip("The name of the power-up (for identification purposes).")]
    public string powerUpName;

    [Tooltip("The prefab to spawn for this power-up.")]
    public GameObject powerUpPrefab;

    [Tooltip("The priority of this power-up. Higher numbers mean higher priority.")]
    public int priority = 1;

    [Tooltip("The minimum number of moves required between spawns of this power-up.")]
    public int cooldown = 1;

    [Header("Pickup Effects")]
    [Tooltip("Optional visual effect prefab to spawn when the power-up is collected.")]
    public GameObject pickupVFX;

    [Tooltip("The name of the sound effect to play when the power-up is collected. Must be registered in the SFXManager.")]
    public string pickupSFX = "PUCollect";

    [Header("Probability")]
    [Tooltip("The likelihood of this power-up spawning (0 = never, 1 = always).")]
    [Range(0f, 1f)]
    public float probability = 1.0f;

    [Header("Spawning Rules")]
    [Tooltip("If true, this power-up alternates with the previous one (cannot spawn consecutively).")]
    public bool alternateWithPrevious;

    [Tooltip("The interval (in moves) at which this power-up can spawn (e.g., every 4 moves).")]
    public int spawnInterval = 4;

    [Tooltip("If true, this power-up can only spawn when the player has the maximum number of bullets.")]
    public bool requiresMaxBullets;

    [Tooltip("If true, this power-up stops spawning entirely once the player has reached the maximum number of bullets.")]
    public bool stopAfterMaxBullets;

    [Tooltip("If true, this power-up can only spawn on odd-numbered moves.")]
    public bool requiresOddMove;

    [Tooltip("The minimum score the player must have for this power-up to spawn.")]
    public int requiredScore = 0;

    [Tooltip("The minimum level the player must reach for this power-up to spawn.")]
    public int requiredLevel = 0;

    [Tooltip("The move number after which this power-up becomes eligible to spawn.")]
    public int spawnAfterMove = 0;

    private int timesUsed = 0;
    private int lastUsedMove = -1000;

    public int TimesUsed => timesUsed;
    public int LastUsedMove => lastUsedMove;

    public void ResetRuntimeState()
    {
        lastUsedMove = -1000;
        timesUsed = 0;
    }

    // Determines whether this power-up can be used on the current move
    public bool IsAvailable(int currentMove)
    {
        if (timesUsed >= 20) return false;

        int movesSinceLastUse = currentMove - lastUsedMove;
        if (movesSinceLastUse < cooldown) return false;

        if (Random.value > probability) return false;

        return true;
    }

    public bool IsEligible(int currentMove, int maxBullets, int enabledBullets, int currentScore, int currentLevel)
    {
        // Check if the current move is after the spawnAfterMove
        if (currentMove < spawnAfterMove) return false;

        // Check cooldown
        int movesSinceLastUse = currentMove - lastUsedMove;
        if (movesSinceLastUse < cooldown) return false;

        // Check spawn interval
        if (movesSinceLastUse % spawnInterval != 0) return false;

        // Check probability
        if (Random.value > probability) return false;

        // Check score requirement
        if (currentScore < requiredScore) return false;

        // Check level requirement
        if (currentLevel < requiredLevel) return false;

        // Check if max bullets are required
        if (requiresMaxBullets && maxBullets != enabledBullets) return false;

        // Check if the move is odd (if required)
        if (requiresOddMove && currentMove % 2 == 0) return false;

        // Stop spawning after max bullets reached
        if (stopAfterMaxBullets && maxBullets == enabledBullets) return false;

        return true;
    }

    public void MarkAsUsed(int currentMove)
    {
        lastUsedMove = currentMove;
        timesUsed++;
    }
}