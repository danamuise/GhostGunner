
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
                tb.TakeDamage(1);
                Debug.Log($"{name} | Ghost Mode hit: {collision.gameObject.name} – health reduced");

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
}