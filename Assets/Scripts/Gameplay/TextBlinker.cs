// 10/1/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using TMPro;

public class TextBlinker : MonoBehaviour
{
    [Tooltip("Speed at which the text blinks (in seconds).")]
    public float blinkSpeed = 0.25f;

    private TextMeshProUGUI textMeshPro;
    private bool isTextVisible = true;

    private void Start()
    {
        // Get the TextMeshProUGUI component attached to this GameObject
        textMeshPro = GetComponent<TextMeshProUGUI>();

        if (textMeshPro == null)
        {
            Debug.LogError("TextBlinker requires a TextMeshProUGUI component on the same GameObject.");
            enabled = false;
            return;
        }

        // Start the blinking coroutine
        StartCoroutine(BlinkText());
    }

    private System.Collections.IEnumerator BlinkText()
    {
        // Initial delay equal to blinkSpeed
        yield return new WaitForSeconds(blinkSpeed);

        while (true)
        {
            // Toggle text visibility
            isTextVisible = !isTextVisible;
            textMeshPro.enabled = isTextVisible;

            // Wait for the specified blink speed
            yield return new WaitForSeconds(blinkSpeed);
        }
    }
}