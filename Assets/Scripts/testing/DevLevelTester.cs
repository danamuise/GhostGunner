// FOR TESTING ONLY. Do not include in final build.

using UnityEngine;
using UnityEngine.SceneManagement;

public class DevLevelTester : MonoBehaviour
{
    [Header("Mock Game State")]
    public int mockScore = 2000;
    public int mockHealth = 3;
    public int mockBulletCount = 6;

    [Header("Startup Settings")]
    public int level = 1;               // 1 or 2 (default = 1)
    public bool autoStart = true;
    public bool bonusPowerUps = false;

    private void Start()
    {
        if (autoStart)
        {
            StartTestLevel(level);
        }
    }

    public void StartTestLevel(int levelToStart)
    {
        if (bonusPowerUps)
        {
            GameState.Instance.BonusPowerUps = true;
        }

        if (levelToStart != 1 && levelToStart != 2)
        {
            Debug.LogWarning("⚠️ DevLevelTester: Only Level 1 or 2 are supported for testing.");
            return;
        }

        GameState.Instance.CurrentScore = mockScore;
        GameState.Instance.SavedTargetHealth = mockHealth;
        GameState.Instance.SavedBulletCount = mockBulletCount;
        GameState.Instance.LevelNumber = levelToStart;
        GameState.Instance.ContinueFromLastSave = true;

        Debug.Log($"🚀 Starting Level {levelToStart} with mock data — Score: {mockScore}, Health: {mockHealth}, Bullets: {mockBulletCount}");
        SceneManager.LoadScene("MainGameScene");
    }
}
