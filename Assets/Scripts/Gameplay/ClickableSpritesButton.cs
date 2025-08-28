using UnityEngine;

public class ClickableSpriteButton : MonoBehaviour
{
    public enum ButtonAction
    {
        MoveUIOut,
        MoveUIIn,
        ToggleMusic,
        ToggleSFX
    }

    public ButtonAction action;

    private GameManager gameManager;

    [System.Obsolete]
    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("ClickableSpriteButton: GameManager not found in scene!");
        }
    }

    private void OnMouseDown()
    {
        if (gameManager == null) return;

        switch (action)
        {
            case ButtonAction.MoveUIOut:
                gameManager.MoveUIOut();
                Debug.Log("Move sound control out");
                break;
            case ButtonAction.MoveUIIn:
                gameManager.MoveUIIn();
                Debug.Log("Move sound control in");
                break;
            case ButtonAction.ToggleMusic:
                Debug.Log("Toggle Music");
                gameManager.ToggleMusic();
                break;
            case ButtonAction.ToggleSFX:
                Debug.Log("Toggle SFX");
                gameManager.ToggleSFX();
                break;
        }
    }
}
