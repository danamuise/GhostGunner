using UnityEngine;

public class AddBulletPowerUp : MonoBehaviour
{
    public PowerUpData powerUpData; // 👈 Assigned in prefab

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
        BulletPool bulletPool = FindObjectOfType<BulletPool>();
        if (bulletPool != null)
        {
            GameObject bullet = bulletPool.GetNextAvailableBullet();
            if (bullet != null)
            {
                bullet.transform.position = transform.position;
                bullet.SetActive(true);

                GhostBullet ghost = bullet.GetComponent<GhostBullet>();
                if (ghost != null)
                {
                    PowerUpManager powerUpManager = FindObjectOfType<PowerUpManager>();
                    if (powerUpManager != null && powerUpData != null)
                    {
                        powerUpManager.PlayPickupEffects(transform.position, powerUpData);
                    }

                    ghost.EnterGhostMode(); // 🔮 Skip tank — go directly to GhostMode
                }

                Debug.Log("🧲 AddBulletPowerUp Activated: Spawned ghost bullet!");
            }
            else
            {
                Debug.LogWarning("⚠️ AddBulletPU: No bullet available to activate.");
            }
        }

        Destroy(gameObject);
    }
}
