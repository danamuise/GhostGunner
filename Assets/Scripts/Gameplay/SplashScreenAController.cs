using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class SplashScreenAController : MonoBehaviour
{
    [Header("TextMeshPro References")]
    public TMP_Text text1;
    public TMP_Text text2;
    public TMP_Text text3; // Funny message
    public TMP_Text text4; // Contact message
    public TMP_Text text5; // Link message
    public GameObject skipButton;

    [Header("GameObject References")]
    public GameObject ghostParent;
    public GameObject wordBalloon;

    [System.Serializable]
    public class SplashMessagePair
    {
        [TextArea] public string funnyMessage;
        [TextArea] public string contactMessage;
    }

    [Header("Splash Message Pairs")]
    public SplashMessagePair[] splashMessages;

    void Start()
    {
        // 🔊 Play intro music
        SFXManager.Instance.PlayMusic("IntroMusic", 0.3f);

        // Initial setup
        text1.gameObject.SetActive(true);
        text2.gameObject.SetActive(true);

        // Pick a random pair of messages
        RandomizeSplashText();

        // Begin coroutine sequence
        StartCoroutine(PlaySplashSequence());
    }

    void RandomizeSplashText()
    {
        if (splashMessages.Length > 0)
        {
            int index = Random.Range(0, splashMessages.Length);
            text3.text = splashMessages[index].funnyMessage;
            text4.text = splashMessages[index].contactMessage;
        }
    }

    IEnumerator PlaySplashSequence()
    {
        yield return new WaitForSeconds(1f);
        ghostParent.SetActive(true);

        yield return new WaitForSeconds(1f);
        wordBalloon.SetActive(true);
        text3.gameObject.SetActive(true);

        yield return new WaitForSeconds(3f);
        text3.gameObject.SetActive(false);
        text4.gameObject.SetActive(true);

        yield return new WaitForSeconds(2f);
        text5.gameObject.SetActive(true);
        skipButton.SetActive(true);


    yield return new WaitForSeconds(3f);
        GoToSplashScreenB();
    }

    public void GoToSplashScreenB()
    {
        SceneManager.LoadScene("SplashScreenB");
    }
}
