using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChallengeMode1 : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject ghost;
    [SerializeField] private GameObject wordBalloon;
    [SerializeField] public GameObject wordBalloon1;
    [SerializeField] private GameObject zombieGraphic0;
    [SerializeField] private GameObject zombieGraphic1;
    [SerializeField] private GameObject familesGraphic;
    [SerializeField] private GameObject desktop;
    [SerializeField] private GameObject stateAdvanceButton;
    [SerializeField] private GameObject civilianPhotos;
    [SerializeField] private GameObject timerObject;
    [SerializeField] private GameObject timeBar;
    [SerializeField] private Button btn_nextLevel;
    [SerializeField] private GameObject BonusPowerUps;
    [SerializeField] private GameObject PowerUp1;
    [SerializeField] private GameObject PowerUp2;
    [SerializeField] private GameObject PowerUp3;

    [Header("Text Objects")]
    [SerializeField] private TextMeshProUGUI CL1_textObject;
    [SerializeField] private TextMeshProUGUI CL2_textObject;
    [SerializeField] private TextMeshProUGUI CL3_textObject;
    [SerializeField] private TextMeshProUGUI CL4_textObject;
    [SerializeField] private TextMeshProUGUI CL5_textObject;
    [SerializeField] public TextMeshProUGUI CL6_textObject;
    [SerializeField] public TextMeshProUGUI CL7_textObject;
    [SerializeField] public TextMeshProUGUI CL8_textObject;
    [SerializeField] public TextMeshProUGUI CL9_textObject;
    [SerializeField] public TextMeshProUGUI CL10_textObject;
    [SerializeField] public TextMeshProUGUI CL11_textObject;
    [SerializeField] public TextMeshProUGUI CL12_textObject;

    [Header("Animation Settings")]
    [SerializeField] private float ghostStartY = -6.20f;
    [SerializeField] private float ghostTargetY = -4.72f;
    [SerializeField] private float ghostOvershoot = 0.2f;
    [SerializeField] private float ghostMoveDuration = 1.0f;
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private float blinkSpeed = 0.5f;

    private bool timerStarted = false;

    [Header("Book Reference")]
    [SerializeField] private Book book;

    private SpriteRenderer wordBalloonRenderer;
    private SpriteRenderer zombieGraphic0Renderer;
    private SpriteRenderer zombieGraphic1Renderer;
    private SpriteRenderer familesGraphicRenderer;
    private SpriteRenderer desktopRenderer;
    private SpriteRenderer stateAdvanceButtonRenderer;
    private bool challengeCompleted = false;

    private Coroutine blinkCoroutine;
    private int currentState = 0;

    private void Awake()
    {
        wordBalloonRenderer = wordBalloon.GetComponent<SpriteRenderer>();
        zombieGraphic0Renderer = zombieGraphic0.GetComponent<SpriteRenderer>();
        zombieGraphic1Renderer = zombieGraphic1.GetComponent<SpriteRenderer>();
        familesGraphicRenderer = familesGraphic.GetComponent<SpriteRenderer>();
        desktopRenderer = desktop.GetComponent<SpriteRenderer>();
        stateAdvanceButtonRenderer = stateAdvanceButton.GetComponent<SpriteRenderer>();

        // Clean initial state
        wordBalloonRenderer.enabled = false;
        zombieGraphic0Renderer.enabled = false;
        zombieGraphic1Renderer.enabled = false;
        familesGraphicRenderer.enabled = false;
        desktopRenderer.enabled = false;
        stateAdvanceButtonRenderer.enabled = false;

        CL1_textObject.gameObject.SetActive(false);
        CL2_textObject.gameObject.SetActive(false);
        CL3_textObject.gameObject.SetActive(false);
        CL4_textObject.gameObject.SetActive(false);
        CL5_textObject.gameObject.SetActive(false);

        civilianPhotos.SetActive(false);

        Vector3 pos = ghost.transform.position;
        pos.y = ghostStartY;
        ghost.transform.position = pos;
    }

    private void Start()
    {
        PlayChallengeMusic();
        Stage0sequence();
    }

    public void PlayChallengeMusic()
    {
        SFXManager.Instance.PlayMusic("challengeZone1", 0.5f);
        Debug.Log("🎵 Playing challengeZone1 music");
    }

    public void OnAdvanceButtonClicked()
    {
        SFXManager.Instance.Play("buttonBoing");

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        stateAdvanceButtonRenderer.enabled = true;

        currentState++;
        RunCurrentState();
    }

    private void RunCurrentState()
    {
        Debug.Log($"🚦 Running State: {currentState}");

        switch (currentState)
        {
            case 1:
                Stage1sequence();
                break;
            case 2:
                Stage2sequence();
                break;
            case 3:
                Stage3sequence();
                break;
            case 4:
                Stage4sequence();
                break;
            case 5:
                Stage5sequence();
                break;
            case 6:
                Stage6sequence();
                break;
            case 7:
                Stage7sequence();
                break;
            default:
                Debug.Log($"🚦 State not implemented: {currentState}");
                break;
        }
    }

    // -------------------- STATES --------------------

    public void Stage0sequence()
    {
        Debug.Log("▶ Stage 0 sequence");
        StartCoroutine(Stage0sequenceCoroutine());
    }

    private IEnumerator Stage0sequenceCoroutine()
    {
        yield return StartCoroutine(MoveGhostWithOvershoot());

        wordBalloonRenderer.enabled = true;
        CL1_textObject.gameObject.SetActive(true);

        zombieGraphic0Renderer.enabled = true;
        yield return StartCoroutine(FadeInSprite(zombieGraphic0Renderer));

        stateAdvanceButtonRenderer.enabled = true;
        blinkCoroutine = StartCoroutine(BlinkSprite(stateAdvanceButtonRenderer));
    }

    public void Stage1sequence()
    {
        Debug.Log("▶ Stage 1 sequence");
        StartCoroutine(Stage1sequenceCoroutine());
    }

    private IEnumerator Stage1sequenceCoroutine()
    {
        yield return StartCoroutine(FadeOutToBlack(zombieGraphic0Renderer));

        zombieGraphic1Renderer.enabled = true;
        yield return StartCoroutine(FadeInSprite(zombieGraphic1Renderer));

        CL1_textObject.gameObject.SetActive(false);
        CL2_textObject.gameObject.SetActive(true);

        blinkCoroutine = StartCoroutine(BlinkSprite(stateAdvanceButtonRenderer));
    }

    public void Stage2sequence()
    {
        Debug.Log("▶ Stage 2 sequence");
        StartCoroutine(Stage2sequenceCoroutine());
    }

    private IEnumerator Stage2sequenceCoroutine()
    {
        yield return StartCoroutine(FadeOutToBlack(zombieGraphic1Renderer));

        familesGraphicRenderer.enabled = true;
        yield return StartCoroutine(FadeInSprite(familesGraphicRenderer));

        CL2_textObject.gameObject.SetActive(false);
        CL3_textObject.gameObject.SetActive(true);

        blinkCoroutine = StartCoroutine(BlinkSprite(stateAdvanceButtonRenderer));
    }

    public void Stage3sequence()
    {
        Debug.Log("▶ Stage 3 sequence");
        StartCoroutine(Stage3sequenceCoroutine());
    }

    private IEnumerator Stage3sequenceCoroutine()
    {
        yield return StartCoroutine(FadeOutToBlack(familesGraphicRenderer));

        desktopRenderer.enabled = true;
        yield return StartCoroutine(FadeInSprite(desktopRenderer));

        CL3_textObject.gameObject.SetActive(false);
        CL4_textObject.gameObject.SetActive(true);

        civilianPhotos.SetActive(true);

        blinkCoroutine = StartCoroutine(BlinkSprite(stateAdvanceButtonRenderer));
    }

    public void Stage4sequence()
    {
        Debug.Log("▶ Stage 4 sequence STARTED");

        CL4_textObject.gameObject.SetActive(false);
        wordBalloon.SetActive(false);
        stateAdvanceButton.SetActive(false);

        StartCoroutine(Stage4sequenceCoroutine());
        StartCoroutine(DelayedWordBalloon1AndTextToggle());
    }

    private IEnumerator DelayedWordBalloon1AndTextToggle()
    {
        yield return new WaitForSeconds(1f);

        wordBalloon1.SetActive(true);
        CL5_textObject.gameObject.SetActive(true);
    }

    public void HideWordBalloon1AndText()
    {
        wordBalloon1.SetActive(false);
        CL5_textObject.gameObject.SetActive(false);
        CL6_textObject.gameObject.SetActive(false);
        CL7_textObject.gameObject.SetActive(false);
        CL8_textObject.gameObject.SetActive(false);
        CL9_textObject.gameObject.SetActive(false);
        CL10_textObject.gameObject.SetActive(false);
    }

    private IEnumerator Stage4sequenceCoroutine()
    {
        Vector3 from = ghost.transform.position;
        Vector3 to = new Vector3(-0.3389f, from.y, from.z);
        yield return StartCoroutine(MoveGhostToNewXPosition(from, to, 1.0f));

        MoveBookInForStage4();
    }

    private void MoveBookInForStage4()
    {
        if (book != null)
        {
            if (!book.gameObject.activeSelf)
            {
                book.gameObject.SetActive(true);
                Debug.Log("📖 Book GameObject activated");
            }
            book.MoveBookIn();
            Debug.Log("📖 Called Book.MoveBookIn() from Stage 4");

            if (timerObject != null)
            {
                timerObject.SetActive(true);
                Debug.Log("⏱️ Timer object enabled (not started yet)");
            }
        }
    }

    public void Stage5sequence()
    {
        Debug.Log("▶ Stage 5 started: gameplay timer begins");

        if (timerStarted)
        {
            Debug.Log("▶ Stage 5 already started, ignoring duplicate call.");
            return;
        }
        timerStarted = true;

        if (timeBar != null)
        {
            timeBar.transform.localScale = new Vector3(1f, timeBar.transform.localScale.y, timeBar.transform.localScale.z);
            timeBar.SetActive(true);
        }
        StartCoroutine(StartStage5Timer(60f));
    }

    private IEnumerator StartStage5Timer(float duration)
    {
        Debug.Log("⏱️ Timer coroutine STARTED");

        float timeRemaining = duration;
        float fullWidth = 356.6f;
        Transform tf = timeBar.transform;
        tf.localScale = new Vector3(fullWidth, tf.localScale.y, tf.localScale.z);

        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            float percent = Mathf.Clamp01(timeRemaining / duration);
            float scaledX = percent * fullWidth;
            tf.localScale = new Vector3(scaledX, tf.localScale.y, tf.localScale.z);
            yield return null;
        }

        if (!challengeCompleted)
        {
            Debug.Log("challenge failed");

            if (timeBar != null)
            {
                timerObject.SetActive(false);
            }

            currentState = 7;
            RunCurrentState();
        }
        else
        {
            Debug.Log("✅ Challenge already completed, ignoring timer expiry.");
        }

    }

    public void Stage6sequence()
    {
        Debug.Log("✅ Challenge Success - Stage 6");
        challengeCompleted = true;
        CleanupAfterChallenge();
       
        if (GameState.Instance != null != null)
        {
            GameState.Instance.BonusPowerUps = true;
            Debug.Log("🎉 Bonus power-ups enabled in GameState.");
        }
        else
        {
            Debug.Log("🎉 Bonus power-ups not enabled.");
        }


        if (BonusPowerUps != null)
        {
            BonusPowerUps.SetActive(true);

            // Start coroutine to enable power-ups with delays
            StartCoroutine(EnablePowerUpsWithDelay());
            // In GameManager script, set GameManager.bonusPowerUps = true
        }

        // play the succeed animation on finalPhotos
        GameObject finalPhotos = GameObject.Find("finalPhotos");
        if (finalPhotos != null)
        {
            Animator anim = finalPhotos.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("succeed", true);
                Debug.Log("🎞️ finalPhotos animator 'succeed' bool set true");
            }
            else
            {
                Debug.LogWarning("⚠️ finalPhotos has no Animator attached");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ finalPhotos object not found in scene");
        }

        // show CL_text11
        if (CL11_textObject != null)
        {
            CL11_textObject.gameObject.SetActive(true);
            Debug.Log("📝 CL11_textObject shown in Stage 6");
        }
        else
        {
            Debug.LogWarning("⚠️ CL11_textObject not assigned in Inspector");
        }
    }

    private IEnumerator EnablePowerUpsWithDelay()
    {
        if (PowerUp1 != null)
        {
            PowerUp1.SetActive(true);
            SFXManager.Instance.Play("bonusPowerUp");
            Debug.Log("⚡ PowerUp1 enabled");

        }

        yield return new WaitForSeconds(0.5f);

        if (PowerUp2 != null)
        {
            PowerUp2.SetActive(true);
            SFXManager.Instance.Play("bonusPowerUp");
            Debug.Log("⚡ PowerUp2 enabled");
        }

        yield return new WaitForSeconds(0.5f);

        if (PowerUp3 != null)
        {
            PowerUp3.SetActive(true);
            SFXManager.Instance.Play("bonusPowerUp");
            Debug.Log("⚡ PowerUp3 enabled");
        }
    }

    public void Stage7sequence()
    {
        
        Debug.Log("❌ Challenge Failed - Stage 7");
        CleanupAfterChallenge();

        GameObject finalPhotos = GameObject.Find("finalPhotos");
        if (finalPhotos != null)
        {
            Animator anim = finalPhotos.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("fail", true);
            }
        }

        if (CL12_textObject != null)
        {
            CL12_textObject.gameObject.SetActive(true);
        }
    }



    // -------------------- UTILS --------------------

    private IEnumerator BlinkSprite(SpriteRenderer renderer)
    {
        while (true)
        {
            renderer.enabled = !renderer.enabled;
            yield return new WaitForSeconds(blinkSpeed);
        }
    }

    private IEnumerator BlinkGameObject(GameObject go)
    {
        while (true)
        {
            go.SetActive(!go.activeSelf);
            yield return new WaitForSeconds(blinkSpeed);
        }
    }

    private IEnumerator MoveGhostWithOvershoot()
    {
        Vector3 startPos = ghost.transform.position;
        Vector3 overshootPos = new Vector3(startPos.x, ghostTargetY + ghostOvershoot, startPos.z);
        Vector3 targetPos = new Vector3(startPos.x, ghostTargetY, startPos.z);

        float t = 0f;
        while (t < ghostMoveDuration)
        {
            t += Time.deltaTime;
            float normalized = t / ghostMoveDuration;
            ghost.transform.position = Vector3.Lerp(startPos, overshootPos, EaseOutCubic(normalized));
            yield return null;
        }

        ghost.transform.position = overshootPos;

        t = 0f;
        float settleDuration = 0.2f;
        while (t < settleDuration)
        {
            t += Time.deltaTime;
            float normalized = t / settleDuration;
            ghost.transform.position = Vector3.Lerp(overshootPos, targetPos, EaseOutCubic(normalized));
            yield return null;
        }

        ghost.transform.position = targetPos;
    }

    private float EaseOutCubic(float x)
    {
        return 1 - Mathf.Pow(1 - x, 3);
    }

    private IEnumerator FadeInSprite(SpriteRenderer renderer)
    {
        Color c = renderer.color;
        c.a = 0;
        renderer.color = c;

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0, 1, t / fadeInDuration);
            renderer.color = c;
            yield return null;
        }
        c.a = 1;
        renderer.color = c;
    }

    private IEnumerator FadeOutToBlack(SpriteRenderer renderer)
    {
        Color c = renderer.color;
        Color startColor = c;
        Color endColor = Color.black;
        endColor.a = 1;

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            renderer.color = Color.Lerp(startColor, endColor, t / fadeOutDuration);
            yield return null;
        }
        renderer.color = endColor;
        renderer.enabled = false;
    }

    private IEnumerator MoveGhostToNewXPosition(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = t / duration;
            ghost.transform.position = Vector3.Lerp(from, to, EaseInOutCubic(normalized));
            yield return null;
        }
        ghost.transform.position = to;
    }

    private float EaseInOutCubic(float x)
    {
        return x < 0.5f ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
    }

    public void ReturnGhostToOriginalPosition()
    {
        Vector3 from = ghost.transform.position;
        Vector3 to = new Vector3(0.875f, from.y, from.z);
        StartCoroutine(MoveGhostToNewXPosition(from, to, 1.0f));
    }

    private void CleanupAfterChallenge()
    {
        Debug.Log("🚦 Cleaning up challenge stage");

        if (book != null)
        {
            book.HideBook();
            Debug.Log("📕 Book moved offscreen and scaled to zero.");
        }

        // Hide book control buttons
        if (book.btn_next != null) book.btn_next.gameObject.SetActive(false);
        if (book.btn_close != null) book.btn_close.gameObject.SetActive(false);
        if (book.btn_useBook != null) book.btn_useBook.gameObject.SetActive(false);

        // Show next-level button with fade blink
        if (btn_nextLevel != null)
        {
            btn_nextLevel.gameObject.SetActive(true);
            StartCoroutine(BlinkUIButtonImage(btn_nextLevel));
        }
        else
        {
            Debug.LogWarning("⚠️ btn_nextLevel not found");
        }

        // Show word balloon
        if (wordBalloon != null)
        {
            wordBalloon.SetActive(true);
        }
        else
        {
            Debug.LogWarning("⚠️ wordBalloon not found in scene");
        }

        // Hide timer
        if (timerObject != null)
        {
            timerObject.SetActive(false);
        }

        // Hide civilian photos
        if (civilianPhotos != null)
        {
            civilianPhotos.SetActive(false);
        }

        // ✅ Hide all text objects CL1 through CL9 and wordBalloon1
        CL1_textObject?.gameObject.SetActive(false);
        CL2_textObject?.gameObject.SetActive(false);
        CL3_textObject?.gameObject.SetActive(false);
        CL4_textObject?.gameObject.SetActive(false);
        CL5_textObject?.gameObject.SetActive(false);
        CL6_textObject?.gameObject.SetActive(false);
        CL7_textObject?.gameObject.SetActive(false);
        CL8_textObject?.gameObject.SetActive(false);
        CL9_textObject?.gameObject.SetActive(false);

        if (wordBalloon1 != null)
        {
            wordBalloon1.SetActive(false);
        }
    }


    private IEnumerator BlinkUIButtonImage(Button button)
    {
        Image img = button.GetComponent<Image>();
        if (img == null)
        {
            Debug.LogWarning("⚠️ Button has no Image component to blink");
            yield break;
        }

        Color originalColor = img.color;
        Color darkColor = originalColor * 0.5f; // 50% darker
        darkColor.a = originalColor.a;          // keep original alpha

        while (true)
        {
            float t = 0f;
            while (t < blinkSpeed)
            {
                t += Time.deltaTime;
                img.color = Color.Lerp(originalColor, darkColor, t / blinkSpeed);
                yield return null;
            }

            t = 0f;
            while (t < blinkSpeed)
            {
                t += Time.deltaTime;
                img.color = Color.Lerp(darkColor, originalColor, t / blinkSpeed);
                yield return null;
            }
        }
    }

}
