

using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class BonusPoints : MonoBehaviour
{
    public TMP_Text bonusText; // Reference to the TMP text field
    public float countDuration = 5.0f;
    private int currentPoints = 0; // Current bonus points
    private bool isOscillating = false; // Flag to check if oscillation is active
    private float oscillationSpeed = 1f; // Speed of oscillation
    private bool hasTriggeredOscillationSFX = false; // Flag to ensure SFX is triggered only once

    // Start is called before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (bonusText != null)
        {
            bonusText.text = "0"; // Initialize the text field
        }
        AwardBonusPoints();
    }

    // Function to start awarding bonus points
    public void AwardBonusPoints()
    {
        StartCoroutine(AddPointsOverTime(1000, countDuration));
    }

    // Coroutine to add points over time
    private System.Collections.IEnumerator AddPointsOverTime(int targetPoints, float duration)
    {
        SFXManager.Instance.PlayLoopingSFX("PointsTally", 1.0f, 1.0f);
        float elapsedTime = 0f;
        int startingPoints = currentPoints;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            currentPoints = Mathf.RoundToInt(Mathf.Lerp(startingPoints, targetPoints, elapsedTime / duration));
            if (bonusText != null)
            {
                bonusText.text = currentPoints.ToString();
            }
            yield return null;
        }

        currentPoints = targetPoints;
        if (bonusText != null)
        {
            bonusText.text = currentPoints.ToString();
        }

        // Start oscillation when points reach the target
        isOscillating = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isOscillating && bonusText != null)
        {
            // Trigger SFX only once when oscillation starts
            if (!hasTriggeredOscillationSFX)
            {
                SFXManager.Instance.StopLoopingSFX("PointsTally", 0.5f);
                SFXManager.Instance.Play("bonusScore");
                hasTriggeredOscillationSFX = true; // Ensure this block is not called again
            }

            // Handle oscillation of the text field
            float scale = 0.1f + Mathf.PingPong(Time.time * oscillationSpeed, 0.1f);
            bonusText.transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}