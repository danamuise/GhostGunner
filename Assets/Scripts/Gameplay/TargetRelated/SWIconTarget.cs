using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SWIconTarget : MonoBehaviour
{
    [Header("Icon Setup")]
    public SpecialWeaponType type;           // Set in Inspector (Nuke, Fire, etc.)
    public GameObject triggerEffectObject;   // e.g., NukeSequence ... FireSequence prefab in the scene (disabled)

    [Header("Behavior")]
    public bool destroyIconAfterTrigger = true;

    private SpriteRenderer sr;
    private SpecialWeapons sw;               // cached manager
    private bool hasTriggered = false;

    // darker tint while charging
    private static readonly Color TintedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    private static readonly Color NormalColor = Color.white;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sw = FindFirstObjectByType<SpecialWeapons>();

        if (sr != null) sr.color = TintedColor;

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // safety: ensure effect starts disabled
        if (triggerEffectObject != null && triggerEffectObject.activeSelf)
        {
            triggerEffectObject.SetActive(false);
            Debug.LogWarning($"⚠️ {type} triggerEffectObject was active at start; disabling for safety.");
        }
    }

    /// <summary>
    /// Called externally by SpecialWeapons as charge progresses.
    /// Only updates visuals; arming logic stays in SpecialWeapons.
    /// </summary>
    public void SetChargeProgress(float percent01)
    {
        if (!sr) return;
        sr.color = (percent01 >= 1f) ? NormalColor : TintedColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return; // debounce multi-hits in same frame
        bool hitByBullet = other.CompareTag("Bullet");
        bool armed = (sw != null && sw.IsArmedFor(type));

        if (!hitByBullet)
        {
            // Not our collider of interest — ignore quietly
            return;
        }

        if (!armed)
        {
            Debug.Log($"🟡 {type} icon was hit by a bullet but isn’t armed yet — ignoring.");
            return;
        }

        hasTriggered = true;
        Debug.Log($"💥 {type} icon triggered (armed + bullet). Enabling sequence object and notifying manager.");

        // 1) Enable the local effect/sequence (e.g., NukeSequence / FireSequence)
        if (triggerEffectObject != null)
        {
            triggerEffectObject.SetActive(true);
            Debug.Log($"🚀 {type} triggerEffectObject enabled.");
        }
        else
        {
            Debug.LogWarning($"⚠️ {type} triggerEffectObject not assigned.");
        }

        // 2) Let SpecialWeapons handle bookkeeping (disarm, UI reset, etc.)
        if (sw != null)
        {
            sw.TriggerSpecialWeapon(type);
        }
        else
        {
            Debug.LogWarning("⚠️ SpecialWeapons manager not found in scene.");
        }

        // 3) Optionally remove the icon after triggering
        if (destroyIconAfterTrigger) Destroy(gameObject);
    }
}
