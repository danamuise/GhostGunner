using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BookPhotoMatcher : MonoBehaviour
{
    [Header("Book Reference")]
    public Book book; // Assign in Inspector

    [Header("UI Book Photo Buttons")]
    public Button[] bookPhotoButtons; // bookPhoto0–bookPhoto3

    [Header("Civilian Photo Animators")]
    public Animator[] civilianAnimators; // 3 animators, top one is current

    [Header("Book Photo Button Roots (Prefabs)")]
    public GameObject[] bookPhotoButtonRoots; // Same order as bookPhotoButtons

    private int[] civilianPhotoIDs = new int[] { 0, 14, 9 };
    private int currentCivilianIndex = 0;
    private int[] bookPhotoIDsOnCurrentPage = new int[4];

    [Header("Parent Animator for Slide Outs")]
    public Animator civilianPhotosParentAnimator;
    public ChallengeMode1 challengeManager;
    private int wrongPhotoClickCount = 0;

    public Sprite challengePUaward;
    void Start()
    {
        currentCivilianIndex = 0;
        book.onBookOpened.AddListener(OnBookOpenedByPlayer);

        // Hook up photo button listeners
        for (int i = 0; i < bookPhotoButtons.Length; i++)
        {
            int index = i;
            bookPhotoButtons[i].onClick.AddListener(() => OnPhotoButtonClicked(index));
        }
    }

    void OnBookOpenedByPlayer()
    {
        Debug.Log("📖 Book officially opened by player — enabling photo buttons");

        // Reset hint text and word balloon
        if (challengeManager != null)
        {
            challengeManager.HideWordBalloon1AndText();
        }

        //SetPhotoButtonsInteractable(true);
        //EnableBookPhotoButtons();
    }


    void OnPhotoButtonClicked(int index)
    {
        if (index < 0 || index >= bookPhotoIDsOnCurrentPage.Length)
        {
            Debug.LogWarning($"📸 bookPhoto{index} clicked, but ID array is not ready.");
            return;
        }

        if (currentCivilianIndex >= civilianPhotoIDs.Length)
        {
            Debug.LogWarning($"🚫 Invalid civilian index: {currentCivilianIndex} (max = {civilianPhotoIDs.Length - 1})");
            return;
        }

        int selectedID = bookPhotoIDsOnCurrentPage[index];
        int targetID = civilianPhotoIDs[currentCivilianIndex];

        if (selectedID == targetID)
        {
            Debug.Log($"photo{selectedID}: correct");
            ShowFeedback(index, true);
            SFXManager.Instance.Play("correctAnswer");
            StartCoroutine(HandleCorrectMatchSequence());
        }
        else
        {
            Debug.Log($"photo{selectedID}: incorrect");
            ShowFeedback(index, false);
            SFXManager.Instance.Play("incorrectAnswer");

            // ❌ Do NOT disable interactables immediately — allow marker to show
            StartCoroutine(HandleIncorrectMatchSequence());
        }

        Debug.Log($"🧠 Current Civilian Index: {currentCivilianIndex} | Target ID: {targetID} | Selected ID: {selectedID}");
    }


    IEnumerator HandleCorrectMatchSequence()
    {
        SetPhotoButtonsInteractable(false);
        yield return new WaitForSeconds(0.75f);

        HideAllBookPhotoMarkers();
        SetPhotoButtonsInteractable(false);
        book.CloseBook();
        DisableBookPhotoButtons();
        yield return new WaitForSeconds(0.5f);

        // trigger stamp animation
        if (currentCivilianIndex < civilianAnimators.Length && civilianAnimators[currentCivilianIndex] != null)
        {
            string stampParam = $"stamp{currentCivilianIndex}";
            civilianAnimators[currentCivilianIndex].SetBool(stampParam, true);
            Debug.Log($"📬 Triggered animation bool '{stampParam}' on civilian photo {currentCivilianIndex}");
        }

        yield return new WaitForSeconds(0.75f);

        if (civilianPhotosParentAnimator != null)
        {
            string slideOutParam = $"slideOut{currentCivilianIndex}";
            civilianPhotosParentAnimator.SetBool(slideOutParam, true);
            Debug.Log($"📤 Triggered animation bool '{slideOutParam}' on CivilianPhotos");
        }

        yield return new WaitForSeconds(1.2f);

        // ➡️ NEW: show CL9 or CL10 depending on which photo was matched
        if (challengeManager != null)
        {
            challengeManager.HideWordBalloon1AndText(); // always clear

            if (currentCivilianIndex == 0)
            {
                challengeManager.CL9_textObject.gameObject.SetActive(true);
                challengeManager.wordBalloon1.SetActive(true);
            }
            else if (currentCivilianIndex == 1)
            {
                challengeManager.CL10_textObject.gameObject.SetActive(true);
                challengeManager.wordBalloon1.SetActive(true);
            }
        }

        AdvanceToNextCivilian();
    }


    void AdvanceToNextCivilian()
    {
        currentCivilianIndex++;

        if (currentCivilianIndex >= civilianPhotoIDs.Length)
        {
            Debug.Log("✅ All civilians matched. Unlock power-up!");
            if (challengeManager != null)
            {
                challengeManager.Stage6sequence();
            }
            else
            {
                Debug.LogWarning("⚠️ challengeManager reference not assigned!");
            }


            // Move ghost to original position
            if (challengeManager != null)
            {
                challengeManager.ReturnGhostToOriginalPosition();
            }
            else
            {
                Debug.LogWarning("⚠️ challengeManager reference not assigned!");
            }

            // Hide btn_useBook
            if (book != null && book.btn_useBook != null)
            {
                book.btn_useBook.gameObject.SetActive(false);
            }


            /*
            // Move book off screen to its original hidden position
            if (book != null)
            {
                book.MoveBookOut(); // Assuming this moves the book back to its original off-screen spot
            }
            else
            {
                Debug.LogWarning("⚠️ Book reference not assigned!");
            }
            */
            return;
        }

        Debug.Log($"🔍 Now searching for photo ID: {civilianPhotoIDs[currentCivilianIndex]}");
    }

    private void finalStage()
    {
        //if player was successfull
        //-- after 0.5 seconds to allow ghost to return to position
        //show wordBaloon0
        //show CL_text11




        //if player failed. 

    }

    public void SetPagePhotoIDs(int id0, int id1, int id2, int id3)
    {
        bookPhotoIDsOnCurrentPage[0] = id0;
        bookPhotoIDsOnCurrentPage[1] = id1;
        bookPhotoIDsOnCurrentPage[2] = id2;
        bookPhotoIDsOnCurrentPage[3] = id3;
    }

    public void UpdateBookPhotosForPage(int currentPage)
    {
        int spreadIndex = (currentPage - 2) / 2;
        int baseID = spreadIndex * 4;

        SetPagePhotoIDs(baseID, baseID + 1, baseID + 2, baseID + 3);
        Debug.Log($"📖 Book page {currentPage} set photo IDs {baseID}–{baseID + 3}");
    }

    void ShowFeedback(int index, bool isCorrect)
    {
        if (index < 0 || index >= bookPhotoButtonRoots.Length) return;

        Transform root = bookPhotoButtonRoots[index].transform;
        Transform correctMark = root.Find("correct");
        Transform incorrectMark = root.Find("incorrect");

        if (correctMark != null) correctMark.gameObject.SetActive(isCorrect);
        if (incorrectMark != null) incorrectMark.gameObject.SetActive(!isCorrect);
    }

    public void SetPhotoButtonsInteractable(bool isOn)
    {
        Debug.Log($"🔧 Setting photo buttons interactable = {isOn}");
        foreach (var btn in bookPhotoButtons)
        {
            if (btn != null)
            {
                btn.interactable = isOn;
                var colors = btn.colors;
                colors.disabledColor = new Color(1f, 1f, 1f, 0f);
                btn.colors = colors;
            }
        }
    }

    void HideAllBookPhotoMarkers()
    {
        foreach (GameObject root in bookPhotoButtonRoots)
        {
            if (root == null) continue;

            Transform correct = root.transform.Find("correct");
            Transform incorrect = root.transform.Find("incorrect");

            if (correct != null) correct.gameObject.SetActive(false);
            if (incorrect != null) incorrect.gameObject.SetActive(false);
        }
    }

    void DisableBookPhotoButtons()
    {
        Debug.Log("✔️ Book photo buttons disabled: ");
        foreach (var btn in bookPhotoButtons)
        {
            if (btn != null)
            {
                btn.interactable = false;
                
                var cg = btn.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.interactable = false;
                    btn.gameObject.SetActive(false);
                    cg.blocksRaycasts = false;
                }
                else
                {
                    Debug.LogWarning($"⚠️ No CanvasGroup found on {btn.gameObject.name}");
                }
            }
        }
    }

    void EnableBookPhotoButtons()
    {
        Debug.Log("✔️ Book photo buttons enabled");
        foreach (var btn in bookPhotoButtons)
        {
            if (btn != null)
            {
                btn.interactable = true;

                var cg = btn.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    btn.gameObject.SetActive(true);
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
        }
    }

    IEnumerator ReenablePhotoButtonsAfterBookOpens()
    {
        Debug.Log("✔️ Book photo buttons renabled after book opens");
        // Wait until book has visually opened (adjust timing if needed)
        yield return new WaitForSeconds(1.0f);

        SetPhotoButtonsInteractable(true);
        EnableBookPhotoButtons();
    }

    public void activatePhotoButtons()
    {
        Debug.Log("✔️ Book photo buttons renabled after book opens");


        SetPhotoButtonsInteractable(true);
        EnableBookPhotoButtons();
    }
    IEnumerator HandleIncorrectMatchSequence()
    {
        // 1. Let the "incorrect" marker be visible briefly
        yield return new WaitForSeconds(0.75f);

        // 2. Show feedback message based on how many incorrects so far
        if (challengeManager != null)
        {
            int cycleIndex = wrongPhotoClickCount % 3;

            challengeManager.HideWordBalloon1AndText(); // Reset all first

            switch (cycleIndex)
            {
                case 0:
                    challengeManager.CL6_textObject.gameObject.SetActive(true);
                    break;
                case 1:
                    challengeManager.CL7_textObject.gameObject.SetActive(true);
                    break;
                case 2:
                    challengeManager.CL8_textObject.gameObject.SetActive(true);
                    break;
            }

            challengeManager.wordBalloon1.SetActive(true);
        }

        wrongPhotoClickCount++;

        // 3. Close the book
        HideAllBookPhotoMarkers();
        book.CloseBook();

        // 4. Disable all interaction on the book photo buttons
        SetPhotoButtonsInteractable(false);
        DisableBookPhotoButtons();

        // 5. Wait for book closing animation (if needed)
        yield return new WaitForSeconds(0.5f);

        // 6. Log
        Debug.Log("❌ Incorrect match. Book closed. Player must reopen it manually.");
    }


}
