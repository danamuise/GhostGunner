using Unity.VisualScripting;
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
    
    //public bool IsSpecialWeaponCharged = false;
    public bool challegeLevel2 { get; set; } = false;
    public bool BonusPointsAwarded { get; set; } = false;
    

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
        InitializePPhighScores();
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

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
        CurrentLevel = 0;
        AvailableSpecialWeapons = 0;
        SavedTargetHealth = -1;
        SavedBulletCount = -1;
        ContinueFromLastSave = false;
        LevelNumber = 1;
        SpecialWeaponUnlocked = SpecialWeaponType.None;
        //IsSpecialWeaponCharged = false;
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
        Debug.LogFormat("<color=green>💾 GameState Loaded — Health: {0}, Bullets: {1}, Score: {2}</color>", SavedTargetHealth, SavedBulletCount, CurrentScore);
    }

    public void PlayButtonClickSound()
    {
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.Play("buttonClick0");
        }
    }

    
    // this function is for testing game over score display
    public void ResetPPhighScores()
    {
        PlayerPrefs.SetInt("HighScore1", 0);
        PlayerPrefs.SetInt("HighScore2", 0);
        PlayerPrefs.SetInt("HighScore3", 0);
        Debug.Log("PlayerPref HighScores reset");
    }

    private void InitializePPhighScores()
    {
        int PPhighScore = PlayerPrefs.GetInt("HighScore1", 0);
        Debug.Log("PPhighScore: " + PPhighScore.ToString());
        if (PPhighScore > 0)
        {
            Debug.Log("PlayerPref High Scores have already been initialized: HighScore1: " + PlayerPrefs.GetInt("HighScore1", 0) + ", HighScore2: " + PlayerPrefs.GetInt("HighScore2", 0) + ", HighScore3: " + PlayerPrefs.GetInt("HighScore3", 0));
        } else
        {
            Debug.Log("PlayerPref HighScore1 initialized: " + PlayerPrefs.GetInt("HighScore1", 0));
        }
    }
}
