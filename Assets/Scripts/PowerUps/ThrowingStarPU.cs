

using UnityEngine;

public class ThrowingStarPU : MonoBehaviour
{
    [Header("Power-Up Data")]
    public PowerUpData powerUpData; // Reference to the ThrowingStarPUdata asset



    private void Start()
    {

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the collider is a bullet
        if (collision.CompareTag("Bullet"))
        {
            Activate();
        }
    }

    private void Activate()
    {
        // Instantiate the ThrowingStarVFX at the current position
        if (powerUpData != null && powerUpData.pickupVFX != null)
        {
            Instantiate(powerUpData.pickupVFX, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("ThrowingStarPU: Missing pickupVFX in PowerUpData.");
        }

        // Play pickup sound effect
        if (powerUpData != null && !string.IsNullOrEmpty(powerUpData.pickupSFX))
        {
            SFXManager.Instance?.Play(powerUpData.pickupSFX);
        }

        // Destroy the power-up target
        Destroy(gameObject);
    }
}