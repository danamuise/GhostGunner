using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class OpenTmpLink : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private TMP_Text tmpText;     // Assign in Inspector (or auto-found)
    [SerializeField] private Camera uiCamera;      // Null for Screen Space - Overlay

    [Header("URLs")]
    [SerializeField] private string siteUrl = "https://www.danamuise.net";
    [SerializeField] private string linkedInUrl = "https://www.linkedin.com/in/danamuise/";

    private Dictionary<string, string> aliasMap;

    private void Awake()
    {
        if (!tmpText) tmpText = GetComponent<TMP_Text>();

        // Map nice aliases and common variants to canonical URLs.
        aliasMap = new Dictionary<string, string>
        {
            // Site aliases
            { "site", siteUrl },
            { "danamuise", siteUrl },
            { "danamuise.net", siteUrl },
            { "www.danamuise.net", siteUrl },

            // LinkedIn aliases
            { "linkedin", linkedInUrl },
            { "li", linkedInUrl },
            { "linkedin.com/in/danamuise/", linkedInUrl },
            { "www.linkedin.com/in/danamuise/", linkedInUrl }
        };
    }

    private void Reset()
    {
        if (!tmpText) tmpText = GetComponent<TMP_Text>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!tmpText) return;

        // For Screen Space - Overlay canvases, uiCamera must be null.
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmpText, eventData.position, uiCamera);
        if (linkIndex == -1) return;

        TMP_LinkInfo linkInfo = tmpText.textInfo.linkInfo[linkIndex];
        string linkId = linkInfo.GetLinkID().Trim();

        if (string.IsNullOrEmpty(linkId)) return;

        string url = ResolveUrl(linkId);
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
        }
    }

    private string ResolveUrl(string linkId)
    {
        string key = linkId.ToLowerInvariant();

        // 1) Alias mapping (recommended: use <link="site"> and <link="linkedin"> in text)
        if (aliasMap.TryGetValue(key, out var mapped))
            return mapped;

        // 2) If author put a full URL, just use it.
        if (key.StartsWith("http://") || key.StartsWith("https://"))
            return linkId;

        // 3) If it looks like a domain/path, prefix https:// for mobile ATS compliance.
        return "https://" + linkId.TrimStart('/');
    }
}
