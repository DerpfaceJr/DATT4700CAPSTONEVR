using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Creates and displays a hologram-style high score panel in the Start Menu scene.
/// Auto-creates the panel at runtime next to the existing Menu Canvas.
/// </summary>
public class HighScoreDisplay : MonoBehaviour
{
    [Header("Panel Positioning")]
    [Tooltip("Offset from the Menu Canvas position. Adjust to place next to start menu panel.")]
    public Vector3 panelOffset = new Vector3(1.5f, 0f, 0f);

    [Tooltip("If set, panel is placed relative to this transform. Otherwise auto-finds the Canvas.")]
    public Transform referenceTransform;

    [Header("Hologram Style")]
    public Color hologramColor = new Color(0f, 0.9f, 1f, 0.85f);
    public Color hologramBgColor = new Color(0f, 0.1f, 0.15f, 0.6f);
    public Color headerColor = new Color(0f, 1f, 0.85f, 1f);
    public Color scoreTextColor = new Color(0.7f, 1f, 0.95f, 0.95f);

    [Header("Panel Size")]
    public float panelWidth = 0.8f;
    public float panelHeight = 1.0f;

    [Header("Font Sizes")]
    public float headerFontSize = 42f;
    public float columnHeaderFontSize = 24f;
    public float entryFontSize = 26f;
    public float footerFontSize = 18f;

    private Canvas hologramCanvas;
    private HighScoreManager highScoreManager;

    void Start()
    {
        EnsureHighScoreManager();
        CreateHologramPanel();
    }

    private void EnsureHighScoreManager()
    {
        highScoreManager = HighScoreManager.Instance;
        if (highScoreManager == null)
        {
            highScoreManager = FindObjectOfType<HighScoreManager>();
        }
        if (highScoreManager == null)
        {
            GameObject hsmObj = new GameObject("HighScoreManager");
            highScoreManager = hsmObj.AddComponent<HighScoreManager>();
        }
    }

    private void CreateHologramPanel()
    {
        // Determine position
        Vector3 spawnPos = transform.position + panelOffset;
        Quaternion spawnRot = transform.rotation;

        if (referenceTransform != null)
        {
            spawnPos = referenceTransform.position + referenceTransform.TransformDirection(panelOffset);
            spawnRot = referenceTransform.rotation;
        }
        else
        {
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                spawnPos = existingCanvas.transform.position + existingCanvas.transform.TransformDirection(panelOffset);
                spawnRot = existingCanvas.transform.rotation;
            }
        }

        // Create world-space canvas
        GameObject canvasObj = new GameObject("HighScoreHologramCanvas");
        canvasObj.transform.position = spawnPos;
        canvasObj.transform.rotation = spawnRot;

        hologramCanvas = canvasObj.AddComponent<Canvas>();
        hologramCanvas.renderMode = RenderMode.WorldSpace;
        hologramCanvas.sortingOrder = 5;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(panelWidth * 1000f, panelHeight * 1000f);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // Background panel with hologram look
        GameObject bgPanel = CreateUIElement("HologramBG", canvasObj.transform);
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = hologramBgColor;

        // Border effect
        GameObject borderObj = CreateUIElement("Border", bgPanel.transform);
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(2, 2);
        borderRect.offsetMax = new Vector2(-2, -2);
        Outline borderOutline = borderObj.AddComponent<Outline>();
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(0, 0, 0, 0);
        borderOutline.effectColor = hologramColor;
        borderOutline.effectDistance = new Vector2(2, 2);

        // Header
        GameObject headerObj = CreateUIElement("Header", bgPanel.transform);
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 0.85f);
        headerRect.anchorMax = new Vector2(1, 1f);
        headerRect.offsetMin = new Vector2(10, 0);
        headerRect.offsetMax = new Vector2(-10, -10);

        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "HIGH SCORES";
        headerText.fontSize = headerFontSize;
        headerText.color = headerColor;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.fontStyle = FontStyles.Bold;

        // Divider line
        GameObject divider = CreateUIElement("Divider", bgPanel.transform);
        RectTransform divRect = divider.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.05f, 0.83f);
        divRect.anchorMax = new Vector2(0.95f, 0.835f);
        divRect.offsetMin = Vector2.zero;
        divRect.offsetMax = Vector2.zero;
        Image divImg = divider.AddComponent<Image>();
        divImg.color = hologramColor;

        // Column headers
        GameObject colHeaders = CreateUIElement("ColumnHeaders", bgPanel.transform);
        RectTransform colRect = colHeaders.GetComponent<RectTransform>();
        colRect.anchorMin = new Vector2(0, 0.74f);
        colRect.anchorMax = new Vector2(1, 0.82f);
        colRect.offsetMin = new Vector2(15, 0);
        colRect.offsetMax = new Vector2(-15, 0);

        TextMeshProUGUI colText = colHeaders.AddComponent<TextMeshProUGUI>();
        colText.text = "RANK    ROUNDS    SCORE     DATE";
        colText.fontSize = columnHeaderFontSize;
        colText.color = hologramColor;
        colText.alignment = TextAlignmentOptions.Center;
        colText.fontStyle = FontStyles.Bold;

        // Score entries
        List<HighScoreManager.HighScoreEntry> scores = highScoreManager.GetHighScores();
        float entryHeight = 0.12f;
        float startY = 0.72f;

        for (int i = 0; i < 5; i++)
        {
            float yMin = startY - (i + 1) * entryHeight;
            float yMax = startY - i * entryHeight;

            GameObject entryObj = CreateUIElement($"ScoreEntry_{i}", bgPanel.transform);
            RectTransform entryRect = entryObj.GetComponent<RectTransform>();
            entryRect.anchorMin = new Vector2(0, yMin);
            entryRect.anchorMax = new Vector2(1, yMax);
            entryRect.offsetMin = new Vector2(15, 2);
            entryRect.offsetMax = new Vector2(-15, -2);

            if (i % 2 == 0)
            {
                Image rowBg = entryObj.AddComponent<Image>();
                rowBg.color = new Color(0f, 0.2f, 0.25f, 0.2f);
            }

            GameObject textObj = CreateUIElement("Text", entryObj.transform);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI entryText = textObj.AddComponent<TextMeshProUGUI>();
            entryText.fontSize = entryFontSize;
            entryText.alignment = TextAlignmentOptions.Center;

            if (i < scores.Count)
            {
                var s = scores[i];
                string dateShort = s.date.Length > 10 ? s.date.Substring(0, 10) : s.date;
                entryText.text = $"#{i + 1}        {s.rounds}          {s.score:F0}       {dateShort}";
                entryText.color = (i == 0) ? new Color(1f, 0.85f, 0f, 1f) : scoreTextColor;
            }
            else
            {
                entryText.text = $"#{i + 1}        ---        ---          ---";
                entryText.color = new Color(0.4f, 0.6f, 0.6f, 0.5f);
            }
        }

        // Footer / decorative scan line
        GameObject footer = CreateUIElement("Footer", bgPanel.transform);
        RectTransform footerRect = footer.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0.05f, 0.05f);
        footerRect.anchorMax = new Vector2(0.95f, 0.1f);
        footerRect.offsetMin = Vector2.zero;
        footerRect.offsetMax = Vector2.zero;

        TextMeshProUGUI footerText = footer.AddComponent<TextMeshProUGUI>();
        footerText.text = "SURVIVE  //  FIGHT  //  CONQUER";
        footerText.fontSize = footerFontSize;
        footerText.color = new Color(hologramColor.r, hologramColor.g, hologramColor.b, 0.5f);
        footerText.alignment = TextAlignmentOptions.Center;
        footerText.fontStyle = FontStyles.Italic;

        // Scan line effect overlay
        GameObject scanLine = CreateUIElement("ScanLine", bgPanel.transform);
        scanLine.AddComponent<HologramScanLine>();
        RectTransform scanRect = scanLine.GetComponent<RectTransform>();
        scanRect.anchorMin = new Vector2(0, 0);
        scanRect.anchorMax = new Vector2(1, 0.02f);
        scanRect.offsetMin = Vector2.zero;
        scanRect.offsetMax = Vector2.zero;
        Image scanImg = scanLine.AddComponent<Image>();
        scanImg.color = new Color(hologramColor.r, hologramColor.g, hologramColor.b, 0.3f);

        Debug.Log("[HighScoreDisplay] Hologram high score panel created.");
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }
}

/// <summary>
/// Animates a scan line moving up the hologram panel for visual effect.
/// </summary>
public class HologramScanLine : MonoBehaviour
{
    private RectTransform rt;
    private float speed = 0.15f;
    private float currentY = 0f;

    void Start()
    {
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (rt == null) return;
        currentY += speed * Time.deltaTime;
        if (currentY > 1f) currentY = 0f;
        rt.anchorMin = new Vector2(0, currentY);
        rt.anchorMax = new Vector2(1, currentY + 0.02f);
    }
}
