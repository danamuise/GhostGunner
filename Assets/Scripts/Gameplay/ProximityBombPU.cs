// ProximityBombPU.cs
using UnityEngine;
using System.Collections.Generic;

public class ProximityBombPU : MonoBehaviour
{
    [Header("Detection (optional/legacy)")]
    public float checkRadius = 0.1f;
    public LayerMask targetLayer;

    [Header("VFX/SFX")]
    public PowerUpData powerUpData;

    private TargetGridManager gridManager;

    [System.Obsolete]
    private void Awake()
    {
        gridManager = FindObjectOfType<TargetGridManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bullet"))
        {
            Activate();
        }
    }

    [System.Obsolete]
    public void Activate()
    {
        if (gridManager == null)
        {
            Debug.LogWarning("ProximityBombPU: No TargetGridManager found.");
            return;
        }

        // Get grid coordinates of the power-up
        if (!gridManager.GetGridCoordinates(transform.position, out int col, out int row))
        {
            Debug.LogWarning("ProximityBombPU: Failed to get grid coordinates.");
            return;
        }

        List<(int c, int r)> affectedCells = new();

        // Add horizontal range (left/right 2)
        for (int dx = -2; dx <= 2; dx++)
        {
            if (dx != 0) affectedCells.Add((col + dx, row));
        }

        // Add vertical range (up/down 2)
        for (int dy = -2; dy <= 2; dy++)
        {
            if (dy != 0) affectedCells.Add((col, row + dy));
        }

        int destroyed = 0;

        foreach (var (c, r) in affectedCells)
        {
            if (!gridManager.IsCellInBounds(c, r)) continue;

            GameObject target = gridManager.GetTargetAt(c, r);
            if (target != null && target.CompareTag("Target"))
            {
                TargetBehavior tb = target.GetComponentInParent<TargetBehavior>();
                if (tb != null)
                {
                    int hp = tb.GetCurrentHealth();
                    if (hp > 0)
                    {
                        // ✅ Award score via TargetBehavior using DamageSource.ProximityBomb
                        tb.ApplyDamage(hp, DamageSource.ProximityBomb);
                        destroyed++;
                        Debug.Log($"💣 ProximityBomb destroyed target at col={c} row={r} (HP={hp})");
                    }
                }
                else
                {
                    // No TargetBehavior path (won’t award score)
                    Destroy(target);
                    destroyed++;
                    Debug.LogWarning($"💣 ProximityBomb destroyed target without TargetBehavior at col={c} row={r}");
                }
            }
        }

        // Camera shake
        CameraShaker.Instance?.Shake(0.2f, 0.15f);

        // Pickup VFX/SFX
        PowerUpManager manager = FindObjectOfType<PowerUpManager>();
        if (manager != null && powerUpData != null)
        {
            manager.PlayPickupEffects(transform.position, powerUpData);
        }

        Debug.Log($"💣 ProximityBombPU finished. Destroyed {destroyed} targets.");
        Destroy(gameObject);
    }
}
