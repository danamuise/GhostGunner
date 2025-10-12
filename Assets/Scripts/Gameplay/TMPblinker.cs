using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPblinker : MonoBehaviour
{
    [Tooltip("Full cycle duration (fade out and back in).")]
    public float cycleSeconds = 1.0f;

    [Tooltip("Minimum alpha during pulse (0 = fully invisible, 0.2 = dim).")]
    [Range(0f, 1f)] public float minAlpha = 0f;

    private TextMeshProUGUI tmp;
    private bool isBlinking = false;
    private float blinkTimer = 0f;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            enabled = false;
            return;
        }

        tmp.enableAutoSizing = false; // ensures alpha and font size are manually controlled
    }

    void Update()
    {
        if (!isBlinking) return;

        if (cycleSeconds <= 0f) return;

        blinkTimer += Time.unscaledDeltaTime;

        // PingPong gives a smooth fade in/out pattern
        float t = Mathf.PingPong(blinkTimer / (cycleSeconds * 0.5f), 1f);
        float alpha = Mathf.Lerp(minAlpha, 1f, t);

        Color c = tmp.color;
        c.a = alpha;
        tmp.color = c;
    }

    public void StartBlinking()
    {
        if (!isBlinking)
        {
            isBlinking = true;
            blinkTimer = 0f;
        }
    }

    public void StopBlinking()
    {
        if (isBlinking)
        {
            isBlinking = false;
            SetAlpha(1f); // ensure visible when stopped
        }
    }

    private void SetAlpha(float alpha)
    {
        Color c = tmp.color;
        c.a = alpha;
        tmp.color = c;
    }
}
