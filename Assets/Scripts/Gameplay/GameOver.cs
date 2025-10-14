using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText1;
    public TextMeshProUGUI highScoreText2;
    public TextMeshProUGUI highScoreText3;

    private void Start()
    {
        SFXManager.Instance.PlayMusic("GameOver", 0.3f);

        if (scoreText != null)
        {
            scoreText.text = ScoreKeeper.finalScore.ToString();
            Debug.Log($" — Final Score: {ScoreKeeper.finalScore}");
            LoadHighScores();
        }
        else
        {
            Debug.LogWarning("⚠️ GameOver.cs: scoreText reference not assigned.");
        }
    }

    public void OnPlayAgainPressed()
    {
        GameState.Instance.LevelNumber = 1;
        GameState.Instance.ContinueFromLastSave = false;
        SceneManager.LoadScene("MainGameScene"); // play again from the start
    }

    public void OnContinueClicked()
    {
        SceneManager.LoadScene("AdsScene"); // If continuing game after an ad
    }


    public void PlayButtonClickSound()
    {
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.Play("buttonClick0");
        }
    }

    private void LoadHighScores()
    {
        switch (CompareScore())
        {
            case 0:
                Debug.Log("No High Score Achieved");
                break;

            case 1:
                Debug.Log("High Score 1 Achieved: " + ScoreKeeper.finalScore.ToString());
                PlayerPrefs.SetInt("HighScore3", PlayerPrefs.GetInt("HighScore2", 0));
                PlayerPrefs.SetInt("HighScore2", PlayerPrefs.GetInt("HighScore1", 0));
                PlayerPrefs.SetInt("HighScore1", ScoreKeeper.finalScore);
                highScoreText1.GetComponent<TMPblinker>().StartBlinking();

                break;
            case 2:
                Debug.Log("High Score 2 Achieved: " + ScoreKeeper.finalScore.ToString());
                PlayerPrefs.SetInt("HighScore3", PlayerPrefs.GetInt("HighScore2", 0));
                PlayerPrefs.SetInt("HighScore2", ScoreKeeper.finalScore);
                highScoreText2.GetComponent<TMPblinker>().StartBlinking();
                break;
            
            case 3:
                Debug.Log("High Score 3 Achieved: " + ScoreKeeper.finalScore.ToString());
                PlayerPrefs.SetInt("HighScore3", ScoreKeeper.finalScore);
                highScoreText3.GetComponent<TMPblinker>().StartBlinking();
                break;

            default:
                Debug.Log("default");
                break;
        }

        highScoreText1.text = PlayerPrefs.GetInt("HighScore1").ToString();
        highScoreText2.text = PlayerPrefs.GetInt("HighScore2").ToString();
        highScoreText3.text = PlayerPrefs.GetInt("HighScore3").ToString();
    }

    private int CompareScore()
    {
        if (ScoreKeeper.finalScore >= PlayerPrefs.GetInt("HighScore1", 0))
        {
            return 1;
        }
        else if (ScoreKeeper.finalScore >= PlayerPrefs.GetInt("HighScore2", 0))
        {
            return 2;
        }
        else if (ScoreKeeper.finalScore >= PlayerPrefs.GetInt("HighScore3", 0))
        {
            return 3;
        }
        else return 0;
    }


}
