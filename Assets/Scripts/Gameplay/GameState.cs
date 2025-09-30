using UnityEngine;


public class GameState : MonoBehaviour
{
    // Singleton instance
    public static GameState Instance { get; private set; }

    // Game data
    public int CurrentScore { get; set; }
    public int CurrentLevel { get; set; }
    public int AvailableSpecialWeapons { get; set; }
    public int SavedTargetHealth { get; set; } = -1;
    public int SavedBulletCount { get; set; } = -1;
    public bool ContinueFromLastSave { get; set; } = false; // are we starting fresh or continuing a game?

    public int LevelNumber = 1; // Default to Level 1

    // Special Weapons (for in-session use only)
    public SpecialWeaponType SpecialWeaponUnlocked { get; set; } = SpecialWeaponType.None;
    public bool IsSpecialWeaponCharged = false;

    public int[] HighScores { get; private set; } = new int[5];

    // Special weapon usage tracking
    private bool nukeHasBeenUsed = false;
    private bool fireHasBeenUsed = false;

    public bool GetNukeHasBeenUsed()
    {
        return nukeHasBeenUsed;
    }

    public void SetNukeHasBeenUsed(bool value)
    {
        nukeHasBeenUsed = value;
    }

    public bool GetFireHasBeenUsed()
    {
        return fireHasBeenUsed;
    }

    public void SetFireHasBeenUsed(bool value)
    {
        fireHasBeenUsed = value;
    }
    private bool bonusPowerUps = false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadHighScores();

        // Only reset if starting a new game
        // ResetGameState();
    }
    public bool BonusPowerUps
    {
        get => bonusPowerUps;
        set => bonusPowerUps = value;
    }

    public void ResetGameState()
    {
        CurrentScore = 0;
        CurrentLevel = 1;
        AvailableSpecialWeapons = 0;
        SavedTargetHealth = -1;
        SavedBulletCount = -1;
        ContinueFromLastSave = false;
        LevelNumber = 1;
        SpecialWeaponUnlocked = SpecialWeaponType.None;
        IsSpecialWeaponCharged = false;
    }

    // Load high scores from PlayerPrefs
    public void LoadHighScores()
    {
        for (int i = 0; i < HighScores.Length; i++)
        {
            HighScores[i] = PlayerPrefs.GetInt($"HighScore_{i}", 0);
        }
    }

    // Save high scores to PlayerPrefs
    public void SaveHighScores()
    {
        for (int i = 0; i < HighScores.Length; i++)
        {
            PlayerPrefs.SetInt($"HighScore_{i}", HighScores[i]);
        }
        PlayerPrefs.Save();
    }

    // Call this to try to insert a new high score
    public void CheckAndUpdateHighScores()
    {
        for (int i = 0; i < HighScores.Length; i++)
        {
            if (CurrentScore > HighScores[i])
            {
                // shift down lower scores
                for (int j = HighScores.Length - 1; j > i; j--)
                {
                    HighScores[j] = HighScores[j - 1];
                }
                HighScores[i] = CurrentScore;
                SaveHighScores();
                break;
            }
        }
    }

    [System.Obsolete]
    public void SaveState()
    {
        TargetHealthCurve curve = FindObjectOfType<TargetHealthCurve>();
        GameManager gm = FindObjectOfType<GameManager>();
        GhostShooter shooter = FindObjectOfType<GhostShooter>();

        if (curve != null && gm != null && shooter != null)
        {
            int move = gm.GetMoveCount();
            SavedTargetHealth = curve.GetHealthForMove(move);
            SavedBulletCount = shooter.bulletPool.GetEnabledBulletCount();
            CurrentScore = gm.GetScore(); // ✅ Save score too

            Debug.LogFormat("<color=green>💾 GameState saved — Health: {0}, Bullets: {1}, Score: {2}</color>", SavedTargetHealth, SavedBulletCount, CurrentScore);
        }
        else
        {
            Debug.LogWarning("⚠️ GameState.SaveState() — Missing curve, manager, or shooter.");
        }
    }

    public void LoadState()
    {
        Debug.Log($"📦 GameState loaded — Health: {SavedTargetHealth}, Bullets: {SavedBulletCount}, Score: {CurrentScore}");
    }

    public void PlayButtonClickSound()
    {
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.Play("buttonClick0");
        }
    }
}
