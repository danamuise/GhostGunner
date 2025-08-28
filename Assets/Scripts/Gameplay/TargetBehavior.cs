using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Identifies what dealt the damage so we can apply correct scoring rules.
/// - Bullet: +1 per hit (legacy behavior)
/// - ProximityBomb / FireSW: on kill, +remaining HP as bonus
/// </summary>
public enum DamageSource { Bullet, ProximityBomb, FireSW, Other }

public class TargetBehavior : MonoBehaviour
{
    [Header("Target Settings")]
    private int health;
    public SpriteRenderer targetSprite;
    public TextMeshProUGUI label;
    public SpriteRenderer dizzolveTarget;
    public SpriteRenderer zombieSprite;
    public Transform scoreText;
    [SerializeField] private GameObject hitParticles;
    private bool isDying = false; // for hitParticle instantiation

    // Animation
    private Animator zombieAnimator;

    // Persistent visual variation
    private Vector2 persistentOffset = Vector2.zero;
    private float persistentZRotation = 0f;

    [Header("Burn VFX")]
    public GameObject zombieBurnPrefab; // assign in Inspector
    [Tooltip("Seconds the ZombieBurn prefab stays alive before auto-destroy.")]
    public float zombieBurnLifetime = 5f;

    private void Awake()
    {
        // Auto-assign SpriteRenderer if missing
        if (targetSprite == null)
            targetSprite = GetComponentInChildren<SpriteRenderer>();

        // Auto-assign TMP label by name if missing
        if (label == null)
        {
            Transform labelTransform = transform.Find("Canvas/TargetHealth");
            if (labelTransform != null)
                label = labelTransform.GetComponent<TextMeshProUGUI>();
        }

        if (label == null)
            Debug.LogWarning($"{name} | TargetBehavior could not find TextMeshPro 'TargetHealth'");

        if (targetSprite == null)
            Debug.LogWarning($"{name} | TargetBehavior could not find SpriteRenderer");

        // 🔄 Get Animator
        zombieAnimator = GetComponentInChildren<Animator>();
        if (zombieAnimator == null)
            Debug.LogWarning($"{name} | TargetBehavior could not find Animator");

        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "foreground";
        }

        UpdateVisuals();
    }

    public void SetHealth(int value)
    {
        health = value;
        UpdateVisuals();
    }

    /// <summary>
    /// Legacy bullet path entry point. Keeps existing behavior: +1 score per hit.
    /// Other systems should call ApplyDamage(amount, source) to specify scoring.
    /// </summary>
    public void TakeDamage(int amount)
    {
        ApplyDamage(amount, DamageSource.Bullet);
    }

    /// <summary>
    /// Centralized damage handler. Applies damage, triggers SFX/anim, and
    /// awards score based on DamageSource. On kill by FireSW or ProximityBomb,
    /// awards the target's remaining HP as a bonus.
    /// </summary>
    [System.Obsolete]
    public void ApplyDamage(int amount, DamageSource source)
    {
        if (isDying) return;

        int preHP = health;
        health -= amount;
        Debug.Log($"{name} | Took {amount} damage from {source} — new health: {health}");

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            // Bullets: +1 per hit (legacy)
            if (source == DamageSource.Bullet)
            {
                gm.AddScore(1);
            }
        }

        if (zombieAnimator != null)
        {
            zombieAnimator.SetBool("zombie_damage", true);
            Invoke(nameof(ResetDamageAnimation), 0.25f);
        }

        if (health <= 0)
        {
            // 🔥 Log world position specifically for Fire Special Weapon kills
            if (source == DamageSource.FireSW)
            {
                Vector3 p = transform.position;
                Debug.Log($"🔥 FireSW kill @ {p.x:F2}, {p.y:F2}, {p.z:F2} | target={name}");

                // 🔥 Spawn ZombieBurn prefab at this position, parent under "Targets" if present
                if (zombieBurnPrefab != null)
                {
                    Transform parent = null;
                    GameObject targetsGO = GameObject.Find("Targets");
                    if (targetsGO != null) parent = targetsGO.transform;

                    GameObject burn = Instantiate(zombieBurnPrefab, p, Quaternion.identity, parent);

                    // NEW: log the spawn position & lifetime
                    Debug.Log($"🕯️ Spawned ZombieBurn '{burn.name}' @ {p.x:F2}, {p.y:F2}, {p.z:F2} (lifetime {zombieBurnLifetime:F1}s)");

                    // NEW: longer lifetime (configurable)
                    Destroy(burn, Mathf.Max(0.1f, zombieBurnLifetime));
                }
            }

            isDying = true;

            // 🔥💣 On kill by Fire or ProximityBomb: add remaining HP as bonus
            if (gm != null && (source == DamageSource.FireSW || source == DamageSource.ProximityBomb))
            {
                int bonus = Mathf.Max(0, preHP);
                if (bonus > 0)
                {
                    gm.AddScore(bonus);
                    Debug.Log($"+{bonus} score for {source} kill on {name}");
                }
            }

            if (zombieSprite != null) zombieSprite.enabled = false;

            if (scoreText != null)
                scoreText.gameObject.SetActive(false);

            if (hitParticles != null && dizzolveTarget != null)
            {
                Debug.Log("Spawning hit particle");
                GameObject fx = Instantiate(hitParticles, dizzolveTarget.transform.position, Quaternion.identity);
                Destroy(fx, 1.5f);
            }

            if (dizzolveTarget != null)
            {
                DissolveAndDisable dissolver = dizzolveTarget.GetComponent<DissolveAndDisable>();
                if (dissolver != null)
                    dissolver.BeginDissolve();
            }

            StartCoroutine(DestroyAfterDelay(0.4f));
        }
        else
        {
            UpdateVisuals();
        }
    }

    private void ResetDamageAnimation()
    {
        if (zombieAnimator != null)
            zombieAnimator.SetBool("zombie_damage", false);
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (zombieSprite != null) zombieSprite.enabled = true;
        Debug.Log("Destroying " + gameObject.name);
        Destroy(gameObject);
    }

    private void UpdateVisuals()
    {
        if (label != null)
            label.text = health.ToString();

        if (targetSprite != null)
        {
            float t = Mathf.InverseLerp(0, 20, health); // 0 = green, 20 = red
            targetSprite.color = Color.Lerp(Color.green, Color.red, t);
        }
    }

    public void SetOffsetAndRotation(Vector2 offset, float zRotation)
    {
        persistentOffset = offset;
        persistentZRotation = zRotation;
        transform.rotation = Quaternion.Euler(0f, 0f, persistentZRotation);
    }

    [System.Obsolete]
    public void AnimateToPosition(Vector2 gridAlignedPosition, float duration = 0.5f, bool fromEndzone = false)
    {
        int moveNumber = FindObjectOfType<GameManager>()?.GetMoveCount() ?? -1;
        Debug.Log($"🎯 TRACKING Animating target {name} on move {moveNumber}");

        Vector2 startPosition = fromEndzone
            ? new Vector2(gridAlignedPosition.x, 5.35f)
            : (Vector2)transform.position;

        Vector2 endPosition = gridAlignedPosition + persistentOffset;

        // 🧟 Enable walk animation
        if (zombieAnimator != null)
            zombieAnimator.SetBool("zombie_walk", true);

        StopAllCoroutines();
        StartCoroutine(SlideToPosition(startPosition, endPosition, duration));
    }

    private IEnumerator SlideToPosition(Vector2 startPos, Vector2 endPos, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector2.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;

        // 🛑 Stop walking animation
        if (zombieAnimator != null)
            zombieAnimator.SetBool("zombie_walk", false);
    }

    public void SetCanvasSortingOrder(int sortingOrder)
    {
        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "foreground";
            canvas.sortingOrder = sortingOrder;
        }
    }

    public int GetCurrentHealth()
    {
        return health; // private int field
    }
}
