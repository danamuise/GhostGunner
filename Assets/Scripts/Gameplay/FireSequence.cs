// FireSequence.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FireSequence : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Delay before ANY targets begin to be destroyed (lets flames pass over first).")]
    public float targetDestroyDelay = 0.5f;
    public float targetDelayTime = 0.25f;   // [0, 0, 1×, 2×, ...] row stagger
    public float postHold = 0.35f;          // pause after last row
    public SpriteRenderer tinter;

    [Header("Grid / Targets")]
    public string targetTag = "Target"; // kept for consistency (not required by this impl)
    private TargetGridManager grid;

    [Header("Gun (match NukeSequence)")]
    public GhostShooter gun;

    private bool running;

    private void OnEnable()
    {
        Debug.Log($"🔥 FireSequence ENABLED on '{name}' (activeInHierarchy={gameObject.activeInHierarchy})");
        if (running) return;
        running = true;
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        grid = FindObjectOfType<TargetGridManager>();

        // Audio
        SFXManager.Instance?.StopMusic();
        SFXManager.Instance?.Play("FireSequenceSFX", 1f, 1f, 1f);

        // 🔒 Block advances for the whole sweep
        TargetManager.SetAdvanceBlocked(true);
        Debug.Log("🔥 FireSequence: advance BLOCKED.");

        if (gun != null)
        {
            gun.DisableGun();
            Debug.Log("🔫 Gun disabled at start of FireSequence.");
        }

        // Let any just-enabled targets register in grid
        yield return null;

        if (grid == null)
        {
            Debug.LogWarning("🔥 FireSequence: No TargetGridManager found.");
            yield return FinishAndMaybeLoad();
            yield break;
        }

        // Find occupied bounds
        if (!FindOccupiedBounds(grid, out int minCol, out int maxCol, out int minRow, out int maxRow))
        {
            Debug.LogWarning("🔥 FireSequence: No occupied cells found.");
            yield return FinishAndMaybeLoad();
            yield break;
        }

        Debug.Log($"🔥 Bounds cols[{minCol}..{maxCol}] rows[{minRow}..{maxRow}]");

        // Global pre-delay so flames pass over before destruction starts
        if (targetDestroyDelay > 0f)
            yield return new WaitForSeconds(targetDestroyDelay);

        // Clear rows top→down with delay pattern [0, 0, 1×, 2×, ...]
        int idxFromTop = 0;
        for (int row = maxRow; row >= minRow; row--, idxFromTop++)
        {
            int delayMult = Mathf.Max(0, idxFromTop - 1);
            float delay = delayMult * targetDelayTime;
            if (delay > 0f) yield return new WaitForSeconds(delay);

            int killed = 0;

            for (int col = minCol; col <= maxCol; col++)
            {
                if (!grid.IsCellInBounds(col, row)) continue;

                GameObject target = grid.GetTargetAt(col, row);
                if (target == null || !target.activeInHierarchy) continue;

                TargetBehavior tb = target.GetComponentInParent<TargetBehavior>();
                if (tb != null)
                {
                    int hp = tb.GetCurrentHealth();
                    if (hp > 0)
                    {
                        tb.ApplyDamage(hp, DamageSource.FireSW); // scoring handled in TargetBehavior
                        killed++;
                    }
                }
                else
                {
                    Object.Destroy(target); // fallback (no score)
                    killed++;
                    Debug.LogWarning($"🔥 Row {row} col {col}: Destroyed target w/o TargetBehavior.");
                }
            }

            Debug.Log($"🔥 Row {row} cleared ({killed} targets).");
        }

        if (postHold > 0f) yield return new WaitForSeconds(postHold);

        yield return FinishAndMaybeLoad();
    }

    private IEnumerator FinishAndMaybeLoad()
    {
        // If you want to resume gameplay in the SAME scene, uncomment:
        // TargetManager.SetAdvanceBlocked(false);
        // if (gun != null) gun.EnableGun(true);

        // Fade to black and wait for it to finish
        yield return FadeToBlack(1.0f);

        // Load the next level after a brief delay (implemented in FadeToBlack)
        yield break;
    }

    // Scan a generous range and shrink to the min/max cells that actually contain targets.
    private static bool FindOccupiedBounds(TargetGridManager gm, out int minC, out int maxC, out int minR, out int maxR)
    {
        minC = maxC = minR = maxR = 0;
        bool any = false;

        // generous probe window; adjust if your grid is larger
        const int PROBE_MIN = -64;
        const int PROBE_MAX = 64;

        int samples = 0, hits = 0;

        for (int r = PROBE_MAX; r >= PROBE_MIN; r--)
        {
            for (int c = PROBE_MIN; c <= PROBE_MAX; c++)
            {
                if (!gm.IsCellInBounds(c, r)) continue;
                samples++;

                var t = gm.GetTargetAt(c, r);
                if (t == null || !t.activeInHierarchy) continue;
                hits++;

                if (!any)
                {
                    minC = maxC = c;
                    minR = maxR = r;
                    any = true;
                }
                else
                {
                    if (c < minC) minC = c; if (c > maxC) maxC = c;
                    if (r < minR) minR = r; if (r > maxR) maxR = r;
                }
            }
        }

        Debug.Log($"🔥 FindOccupiedBounds: sampled={samples}, occupied={hits}, any={any}");
        return any;
    }

    private IEnumerator FadeToBlack(float fadeTime)
    {
        if (tinter == null)
        {
            Debug.LogWarning("FadeToBlack: No SpriteRenderer (tinter) assigned.");
            yield break;
        }

        Color startColor = tinter.color;
        float elapsedTime = 0f;

        // Gradually increase the alpha value from 0 to 1 over fadeTime
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeTime);
            tinter.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // Ensure the alpha is set to 1 at the end
        tinter.color = new Color(startColor.r, startColor.g, startColor.b, 1f);

        // Add a delay before loading the next level
        float delayBeforeLoad = 0.5f; // Adjust this value as needed
        yield return new WaitForSeconds(delayBeforeLoad);

        // ✅ Unblock just before scene transition
        TargetManager.SetAdvanceBlocked(false);

        // Load the next level after the delay
        LoadChallengeLevel1();
    }

    public void LoadChallengeLevel1()
    {
        GameState.Instance.challegeLevel2 = true;
        Debug.Log("🔄 Loading ChallengeLevel1 scene...");
        SceneManager.LoadScene("ChallengeLevel1");
    }
}
