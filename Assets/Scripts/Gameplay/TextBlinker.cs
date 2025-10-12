// 10/11/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class TextBlinker : MonoBehaviour
{
    [Tooltip("Speed at which the text blinks (in seconds).")]
    public float blinkSpeed = 0.25f;

    private TextMeshProUGUI textMeshPro;
    private Coroutine blinkingCoroutine;

    private void Awake()
    {
        // Get the TextMeshProUGUI component attached to this GameObject
        textMeshPro = GetComponent<TextMeshProUGUI>();

        if (textMeshPro == null)
        {
            Debug.LogError("TextBlinker requires a TextMeshProUGUI component on the same GameObject.");
            enabled = false;
            return;
        }
    }

    public void StartBlinking()
    {
        Debug.Log("Start Blinking");
        if (blinkingCoroutine == null)
        {
            blinkingCoroutine = StartCoroutine(BlinkText());
        }
        else
        {
            Debug.Log("Blinking is already running.");
        }
    }

    public void StopBlinking()
    {
        if (blinkingCoroutine != null)
        {
            StopCoroutine(blinkingCoroutine);
            blinkingCoroutine = null;

            // Ensure the text is fully visible when blinking stops
            //SetAlpha(1f);
        }
    }

    private System.Collections.IEnumerator BlinkText()
    {
        bool isTextVisible = true;
        Debug.Log("Start Blinking coroutine");

        var wait = new WaitForSecondsRealtime(blinkSpeed); // ← key change

        while (true)
        {
            isTextVisible = !isTextVisible;

            if (isTextVisible)
            {
                Debug.Log("Set font size to 12");
                textMeshPro.fontSize = 12f;
            }
            else
            {
                Debug.Log("Set font size to 0");
                textMeshPro.fontSize = 0.01f; // avoid 0; TMP can be weird at 0
            }

            Debug.Log("isTextVisible: " + isTextVisible);
            Debug.Log($"timeScale={Time.timeScale}, enabled={enabled}, active={gameObject.activeInHierarchy}");
            yield return wait; // unscaled wait
        }
    }

    private void SetAlpha(float alpha)
    {
        if (textMeshPro != null)
        {
            Color color = textMeshPro.color;
            color.a = alpha; // Set the alpha value
            textMeshPro.color = color;
        }
    }

    private void OnDisable()
    {
        // Stop blinking when the object is disabled
        //StopBlinking();
    }
}