// 8/26/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
[System.Serializable]
public class PowerUpRule
{
    public string powerUpName; // Name of the power-up
    public PowerUpData powerUpData; // Reference to the power-up prefab and data
    public int priority; // Priority for selection (higher priority gets checked first)
    public bool alternateWithPrevious; // Whether this power-up alternates with the previous one
    public int spawnInterval; // Spawn interval in moves (e.g., every 4 moves)
    public bool requiresMaxBullets; // Whether this power-up requires max bullets to spawn
    public bool requiresOddMove; // Whether this power-up spawns only on odd-numbered moves
    public int minMove; // Minimum move number to start spawning this power-up
    public int requiredScore; // Minimum score required to spawn this power-up
    public int requiredLevel; // Level requirement for this power-up

    // Custom eligibility logic (optional)
    public System.Func<int, GameManager, BulletPool, bool> customEligibility;
}
