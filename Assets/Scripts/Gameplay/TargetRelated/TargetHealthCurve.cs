using UnityEngine;
using System.Collections.Generic;

public class TargetHealthCurve : MonoBehaviour
{
    [Header("Health Curve Settings")]
    public float healthSlope = 0.8f;
    public float spikeMagnitudePercent = 0.15f;
    public int spikeInterval = 10;
    public int spikeAscentDuration = 3;
    public float spikeDropPercent = 0.10f;
    public int totalMoves = 200;

    [Header("Debug Display Settings")]
    public bool showOverlay = true; // Toggle overlay on/off in Inspector

    private List<int> healthHistory = new List<int>();
    private List<float> spikeHistory = new List<float>();
    private int horizontalShift = 5;

    private void Start()
    {
        // Precompute health curve
        for (int i = 1; i <= totalMoves; i++)
        {
            CalculateHealthAndSpikeAtMove(i, out int health, out float spike);
            healthHistory.Add(health);
            spikeHistory.Add(spike);
        }
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!showOverlay) return;

        int rawMoveCount = GetCurrentMoveCount();
        int displayMoveCount = Mathf.Max(1, rawMoveCount); // 🔥 Always show at least 1

        GUILayout.BeginArea(new Rect(10, 10, 300, 120));
        GUILayout.Label($"Move: {displayMoveCount}");

        if (displayMoveCount > 0 && displayMoveCount <= healthHistory.Count)
        {
            GUILayout.Label($"Current Health: {healthHistory[displayMoveCount - 1]}");
            GUILayout.Label($"Spike Value: {spikeHistory[displayMoveCount - 1]:F2}");
        }
        GUILayout.EndArea();
    }

#endif

    private void CalculateHealthAndSpikeAtMove(int move, out int finalHealth, out float spikeValue)
    {
        float baseHealth;

        if (move <= 5)
        {
            baseHealth = 1f;
            spikeValue = 0f;
        }
        else
        {
            int shiftedMove = move - horizontalShift;
            float baseValue = healthSlope * shiftedMove;

            int moveInCycle = shiftedMove % spikeInterval;
            int distanceFromSpike = (spikeInterval - moveInCycle) % spikeInterval;

            if (moveInCycle == 0)
            {
                // Full spike
                spikeValue = baseValue * spikeMagnitudePercent;
                baseHealth = baseValue * (1f + spikeMagnitudePercent);
            }
            else if (moveInCycle == 1)
            {
                // Drop after spike
                spikeValue = -baseValue * spikeDropPercent;
                baseHealth = baseValue * (1f - spikeDropPercent);
            }
            else if (distanceFromSpike < spikeAscentDuration)
            {
                // Gradual buildup to spike
                float ascentStep = (spikeAscentDuration - distanceFromSpike) / (float)spikeAscentDuration;
                spikeValue = baseValue * spikeMagnitudePercent * ascentStep;
                baseHealth = baseValue * (1f + spikeMagnitudePercent * ascentStep);
            }
            else
            {
                // Normal curve
                spikeValue = 0f;
                baseHealth = baseValue;
            }
        }

        finalHealth = Mathf.CeilToInt(baseHealth);
    }

    public int GetHealthForMove(int move)
    {
        if (move <= 0 || move > healthHistory.Count)
        {
            Debug.LogWarning($"[HealthCurveGenerator] Requested move {move} is out of range.");
            return 1;
        }

        return healthHistory[move - 1];
    }

    [System.Obsolete]
    private int GetCurrentMoveCount()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
            return gm.GetMoveCount();
        else
            return 0;
    }
}


