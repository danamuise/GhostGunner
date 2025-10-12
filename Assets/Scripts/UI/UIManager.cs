using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("GamePlay Score UI")]
    public TextMeshProUGUI scoreFieldUI;

    [Header("Final Score UI")]
    //public TextMeshProUGUI finalScoreUI;

    [Header("Scene References")]
    private GameManager gameManager;
    public GridTargetSpawner gridTargetSpawner;
    public TargetManager targetManager;
    public TargetGridManager grid;
    public GhostShooter gun;

    private void Start()
    {
        gameManager = Object.FindFirstObjectByType<GameManager>();
        //scoreFieldUI.text = "NEWTEXT";
    }
    public void UpdateScoreDisplay(int score)
    {
        Debug.Log($"📺 Updating scoreFieldUI: {score}");
        if (scoreFieldUI != null)
        {
            scoreFieldUI.text = $"{score}";
        }
    }

    /*public void ShowFinalScore(int finalScore)
    {
        if (finalScoreUI != null)
        {
            finalScoreUI.text = $"Final Score: {finalScore}";
        }
    }*/

    public void InitializeUI()
    {
        UpdateScoreDisplay(0);
    }

    public void PlayAgain()
    {
        Debug.Log("🔁 Restarting Game...");
        SceneManager.LoadScene("SplashScreen");
        gameManager.ResetScore(); // Add ResetScore method in GameManager
        InitializeUI();
        //ShowFinalScore(0);
        targetManager.ClearAllTargets();
        SFXManager.Instance.FadeOutMusic(2f);
        grid.InitializeGrid();
        gun.EnableGun(true);
    }
}
