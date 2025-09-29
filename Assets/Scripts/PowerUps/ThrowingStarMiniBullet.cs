// 9/25/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

// 9/25/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

// 9/24/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

public class ThrowingStarMiniBullet : MonoBehaviour
{
    [SerializeField] private GameObject hitParticlesPrefab; // Reference to the HitParticles prefab
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // Cache the SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"{name} collided with {collision.gameObject.name} (Tag: {collision.gameObject.tag})");

        // If the bullet hits a Target
        if (collision.gameObject.CompareTag("Target"))
        {
            Debug.Log($"{name} hit target: {collision.gameObject.name}");

            TargetBehavior tb = collision.gameObject.GetComponentInParent<TargetBehavior>();
            if (tb != null)
            {
                int hp = tb.GetCurrentHealth();
                if (hp > 0)
                {
                    // Apply all damage to destroy the target and award score
                    tb.ApplyDamage(hp, DamageSource.ProximityBomb);
                    Debug.Log($"{name} | Target destroyed: {collision.gameObject.name} – awarded score");
                }
            }
            else
            {
                // No TargetBehavior path (won’t award score)
                Destroy(collision.gameObject);
                Debug.Log($"{name} | Target destroyed without awarding score: {collision.gameObject.name}");
            }

            // Instantiate the HitParticles prefab at the collision point
            if (hitParticlesPrefab != null)
            {
                Instantiate(hitParticlesPrefab, transform.position, Quaternion.identity);
            }

            // Destroy miniBullet
            Destroy(gameObject);
        }
    }
}