// 10/11/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

public class SpriteBlinker : MonoBehaviour
{
    [Tooltip("Speed at which the sprite blinks (in seconds).")]
    public float blinkSpeed = 0.5f;

    private SpriteRenderer[] spriteRenderers;
    private bool isVisible = true;

    private void Start()
    {
        // Get all SpriteRenderer components on this GameObject and its children
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (spriteRenderers.Length == 0)
        {
            Debug.LogWarning("⚠️ SpriteBlinker: No SpriteRenderer components found on this GameObject or its children.");
            enabled = false;
            return;
        }

        // Start the blinking coroutine
        StartCoroutine(BlinkSprites());
    }

    private System.Collections.IEnumerator BlinkSprites()
    {
        while (true)
        {
            // Toggle visibility
            isVisible = !isVisible;

            // Apply visibility to all SpriteRenderers
            foreach (var spriteRenderer in spriteRenderers)
            {
                spriteRenderer.enabled = isVisible;
            }

            // Wait for the specified blink speed
            yield return new WaitForSeconds(blinkSpeed);
        }
    }
}