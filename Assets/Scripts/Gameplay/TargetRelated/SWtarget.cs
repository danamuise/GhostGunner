using UnityEngine;

public class SWTarget : MonoBehaviour
{
    [Header("Special Weapon")]
    [Tooltip("Which special weapon this target grants when hit by a bullet.")]
    public SpecialWeaponType weaponType = SpecialWeaponType.Nuke;

    [Header("Pickup FX (optional)")]
    [Tooltip("Child GameObject name to enable when picked up (e.g., a Particle System). Leave blank to auto-detect first ParticleSystem child.")]
    public string pickupVFXChildName = "PickupVFX";
    [Tooltip("Seconds before auto-destroying the spawned VFX (if detached). Set <=0 to skip auto-destroy.")]
    public float pickupVFXLifetime = 2f;
    [Tooltip("Detach VFX so it stays at the impact point instead of moving with the target to the console.")]
    public bool detachVFX = true;

    [Header("Audio (optional)")]
    public string pickupSFX = "PUCollect";
    public float sfxVolume = 1f;

    [Header("Visuals")]
    [Tooltip("Hide sprites immediately on hit to avoid flicker while the object animates to the console.")]
    public bool hideSpritesImmediately = true;

    private bool isActivated = false;
    private Collider2D cachedCollider;
    private Rigidbody2D rb2d;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider2D>();
        rb2d = GetComponent<Rigidbody2D>();
    }

    [System.Obsolete]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return;
        if (!other.CompareTag("Bullet")) return;

        isActivated = true;

        // Prevent repeat triggers + physics jitter
        if (cachedCollider) cachedCollider.enabled = false;
        if (rb2d)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
            rb2d.simulated = false;
        }

        // Optional: show pickup VFX
        TryPlayPickupVFX();

        // Optional: SFX
        if (!string.IsNullOrEmpty(pickupSFX) && SFXManager.Instance != null)
        {
            SFXManager.Instance.Play(pickupSFX, sfxVolume);
        }

        // Hide sprites immediately if desired (SpecialWeapons will handle lifecycle)
        if (hideSpritesImmediately)
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                sr.enabled = false;
        }

        // Notify SpecialWeapons manager (it will move this object to console, spawn icon, etc.)
        var specialWeapons = FindObjectOfType<SpecialWeapons>();
        if (specialWeapons != null)
        {
            specialWeapons.OnSpecialWeaponCollected(weaponType, gameObject);
        }
        else
        {
            Debug.LogWarning("⚠️ SpecialWeapons manager not found in scene.");
        }

        // Do NOT destroy here — SpecialWeapons controls this object's lifecycle
    }

    private void TryPlayPickupVFX()
    {
        Transform vfxTransform = null;

        if (!string.IsNullOrEmpty(pickupVFXChildName))
        {
            var t = transform.Find(pickupVFXChildName);
            if (t != null) vfxTransform = t;
        }

        // Fallback: find first ParticleSystem child
        if (vfxTransform == null)
        {
            var ps = GetComponentInChildren<ParticleSystem>(includeInactive: true);
            if (ps != null) vfxTransform = ps.transform;
        }

        if (vfxTransform == null) return;

        if (detachVFX)
        {
            vfxTransform.SetParent(null, worldPositionStays: true);
        }

        vfxTransform.gameObject.SetActive(true);

        // Auto-destroy if requested (only safe if detached or if you’re okay with it disappearing)
        if (pickupVFXLifetime > 0f)
        {
            Destroy(vfxTransform.gameObject, pickupVFXLifetime);
        }
    }
}
