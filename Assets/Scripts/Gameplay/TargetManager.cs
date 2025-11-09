// TargetManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    // 🚫 Global gate to prevent targets from advancing
    public static bool blockAdvance = false;

    // 🔢 Epoch increments whenever we (re)block. Any in-flight move with an older epoch must abort.
    private static uint advanceEpoch = 0;

    // 🧵 Track active move coroutines so we can cancel them on block
    private static readonly Dictionary<int, Coroutine> activeMoves = new Dictionary<int, Coroutine>();
    private static int nextMoveId = 1;

    [Header("Scene References")]
    public Transform targetsParent;

    [Header("Animation Settings")]
    public float moveDuration = 0.35f;
    public float easing = 2.5f;

    [Header("Game Over Settings")]
    public float gameOverY = -3.0f;

    private void Start()
    {
        blockAdvance = false; // Reset on scene load
        Debug.Log("TargetManager.Start(): blockAdvance reset to FALSE");
    }

    /// <summary>
    /// Flip the global advance lock. When enabling the block, cancels any in-flight moves.
    /// </summary>
    public static void SetAdvanceBlocked(bool on)
    {
        if (blockAdvance == on) return;

        blockAdvance = on;

        if (on)
        {
            advanceEpoch++; // mark existing moves stale
            Debug.Log($"🚫 Advance BLOCKED (epoch={advanceEpoch}). Stopping in-flight moves…");
            StopAllActiveMoves();
        }
        else
        {
            Debug.Log("✅ Advance UNBLOCKED.");
        }
    }

    /// <summary>
    /// Safely start a downward move. Honors the global block and registers the coroutine for cancellation.
    /// Call this (not StartCoroutine(MoveTargetsDown(...))).
    /// </summary>
    public Coroutine StartMoveTargetsDown(float rowSpacing)
    {
        if (blockAdvance)
        {
            Debug.Log("↩️ MoveTargetsDown request ignored (blocked).");
            return null;
        }

        int id = nextMoveId++;
        uint myEpoch = advanceEpoch;
        Coroutine c = StartCoroutine(MoveTargetsDownRoutine(rowSpacing, id, myEpoch));
        activeMoves[id] = c;
        return c;
    }

    /// <summary>
    /// Internal runner that can abort mid-lerp if a block engages (epoch changes).
    /// </summary>
    private IEnumerator MoveTargetsDownRoutine(float rowSpacing, int moveId, uint myEpoch)
    {
        // Bail if a block somehow engaged between request and coroutine start
        if (blockAdvance || myEpoch != advanceEpoch)
        {
            activeMoves.Remove(moveId);
            yield break;
        }

        List<Transform> targets = new List<Transform>();
        foreach (Transform child in targetsParent)
        {
            if (child != null)
                targets.Add(child);
        }

        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> endPositions = new List<Vector3>();

        foreach (Transform target in targets)
        {
            Vector3 start = target.position;
            Vector3 end = new Vector3(start.x, start.y - rowSpacing, start.z);
            startPositions.Add(start);
            endPositions.Add(end);
        }

        float t = 0f;
        bool aborted = false;

        while (t < 1f)
        {
            // ⛔ Abort immediately if someone engaged the block after we started
            if (blockAdvance || myEpoch != advanceEpoch)
            {
                Debug.Log($"⛔ MoveTargetsDown aborted mid-lerp (blocked/epoch change). myEpoch={myEpoch}, currentEpoch={advanceEpoch}");
                aborted = true;
                break;
            }

            t += Time.deltaTime / moveDuration;
            float eased = 1f - Mathf.Pow(1f - t, easing);

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    Vector3 pos = Vector3.Lerp(startPositions[i], endPositions[i], eased);
                    if (!float.IsNaN(pos.x) && !float.IsNaN(pos.y))
                        targets[i].position = pos;
                }
            }

            yield return null;
        }

        // If we finished naturally (not blocked), snap to end positions for precision.
        if (!aborted)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                    targets[i].position = endPositions[i];
            }
        }

        activeMoves.Remove(moveId);
    }

    /// <summary>
    /// Stop any currently running MoveTargetsDown coroutines across the scene.
    /// </summary>
    private static void StopAllActiveMoves()
    {
        if (activeMoves.Count == 0) return;

        var mgr = FindObjectOfType<TargetManager>();
        if (mgr != null)
        {
            // Collect to temp list to avoid modifying while iterating
            var toStop = new List<Coroutine>(activeMoves.Values);
            foreach (var c in toStop)
            {
                if (c != null) mgr.StopCoroutine(c);
            }
        }
        activeMoves.Clear();
    }

    public void ClearAllTargets()
    {
        foreach (Transform t in targetsParent)
        {
            if (t != null)
                Destroy(t.gameObject);
        }
    }
}
