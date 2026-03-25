using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Turns the HelpArea Canvas into a slideshow presentation.
/// Slide images are configured via the Inspector array.
/// Prev/Next buttons navigate slides; a Start button on the last slide loads the game.
///
/// Supports two workflows:
///   1) Editor-placed UI: place children named SlideshowBG/SlideArea/NavBar/PrevBtn/NextBtn/
///      StartGameBtn/SlideCounter in the Canvas – the script finds and wires them.
///   2) Auto-create: if any element is missing the script creates it at runtime.
///
/// Attach to the Canvas GameObject alongside VRControllerUIClick.
/// </summary>
public class HelpSlideshow : MonoBehaviour
{
    [Header("Slides (add/remove in Inspector)")]
    [Tooltip("Slide images in presentation order. Drag sprites here.")]
    public Sprite[] slides = new Sprite[0];

    [Header("Scene Loading")]
    [Tooltip("Scene to load when Start Game is clicked on the last slide")]
    public string gameSceneName = "SimpleArea";
    public int gameBuildIndex = 2;
    public bool useBuildIndex = true;

    [Header("Panel Colors")]
    public Color panelBackground = new Color(0.02f, 0.02f, 0.06f, 0.95f);
    public Color navBarColor = new Color(0.06f, 0.06f, 0.10f, 0.92f);

    [Header("Button Colors")]
    public Color buttonColor = new Color(0.15f, 0.45f, 0.85f, 1f);
    public Color startButtonColor = new Color(0.12f, 0.72f, 0.30f, 1f);
    public Color disabledColor = new Color(0.25f, 0.25f, 0.30f, 0.55f);
    public Color textColor = Color.white;

    [Header("Font Sizes")]
    public float buttonFontSize = 30f;
    public float counterFontSize = 24f;

    [Header("Optional – drag from scene or leave empty to auto-find/create")]
    public Image slideImageRef;
    public Button prevButtonRef;
    public Button nextButtonRef;
    public Button startButtonRef;
    public TextMeshProUGUI counterTextRef;

    private Image slideImage;
    private Button prevButton;
    private Button nextButton;
    private Button startButton;
    private TextMeshProUGUI counterText;
    private int currentIndex;

    void Start()
    {
        ApplyBlackSkybox();
        CleanLegacyMenu();
        EnsureCanvasComponents();
        ResolveOrBuildUI();
        WireButtons();
        ShowSlide(0);
        Debug.Log($"[HelpSlideshow] Ready. slides={slides?.Length ?? 0}, prev={prevButton != null}, next={nextButton != null}, start={startButton != null}, slideImg={slideImage != null}");
    }

    // ------------------------------------------------------------------ skybox

    private void ApplyBlackSkybox()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.2f);

        foreach (Camera cam in FindObjectsOfType<Camera>())
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }
    }

    // ------------------------------------------------------------------ cleanup

    private void CleanLegacyMenu()
    {
        Transform menu = transform.Find("Menu");
        if (menu != null) Destroy(menu.gameObject);

        StartMenuActions sma = GetComponent<StartMenuActions>();
        if (sma != null) Destroy(sma);
    }

    private void EnsureCanvasComponents()
    {
        if (GetComponent<Canvas>() == null)
        {
            Canvas c = gameObject.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
        }
        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;
        }
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    // ------------------------------------------------------------------ find or build

    private void ResolveOrBuildUI()
    {
        Transform bg = FindChild(transform, "SlideshowBG");
        Transform slideArea = bg != null ? FindChild(bg, "SlideArea") : null;
        Transform navBar = bg != null ? FindChild(bg, "NavBar") : null;

        if (bg == null)
        {
            GameObject bgObj = MakeElement("SlideshowBG", transform);
            StretchFill(bgObj);
            bgObj.AddComponent<Image>().color = panelBackground;
            bg = bgObj.transform;
        }

        if (slideArea == null)
        {
            GameObject sa = MakeElement("SlideArea", bg);
            SetAnchors(sa, 0.03f, 0.15f, 0.97f, 0.97f);
            Image img = sa.AddComponent<Image>();
            img.preserveAspect = true;
            img.type = Image.Type.Simple;
            img.color = Color.white;
            slideArea = sa.transform;
        }

        if (navBar == null)
        {
            GameObject nb = MakeElement("NavBar", bg);
            SetAnchors(nb, 0.03f, 0.02f, 0.97f, 0.13f);
            nb.AddComponent<Image>().color = navBarColor;
            navBar = nb.transform;
        }

        slideImage = slideImageRef != null ? slideImageRef : slideArea.GetComponent<Image>();
        if (slideImage == null)
            slideImage = slideArea.gameObject.AddComponent<Image>();

        prevButton = prevButtonRef;
        if (prevButton == null)
        {
            Transform t = FindChild(navBar, "PrevBtn");
            if (t != null) prevButton = t.GetComponent<Button>();
        }
        if (prevButton == null)
        {
            float p = 0.015f;
            prevButton = MakeButton("PrevBtn", navBar, "\u25C0  Prev", p, p, 0.30f - p, 1f - p, buttonColor);
        }

        counterText = counterTextRef;
        if (counterText == null)
        {
            Transform t = FindChild(navBar, "SlideCounter");
            if (t != null) counterText = t.GetComponent<TextMeshProUGUI>();
        }
        if (counterText == null)
        {
            GameObject ctr = MakeElement("SlideCounter", navBar);
            SetAnchors(ctr, 0.30f, 0f, 0.70f, 1f);
            counterText = ctr.AddComponent<TextMeshProUGUI>();
            counterText.fontSize = counterFontSize;
            counterText.color = textColor;
            counterText.alignment = TextAlignmentOptions.Center;
        }

        nextButton = nextButtonRef;
        if (nextButton == null)
        {
            Transform t = FindChild(navBar, "NextBtn");
            if (t != null) nextButton = t.GetComponent<Button>();
        }
        if (nextButton == null)
        {
            float p = 0.015f;
            nextButton = MakeButton("NextBtn", navBar, "Next  \u25B6", 0.70f + p, p, 1f - p, 1f - p, buttonColor);
        }

        startButton = startButtonRef;
        if (startButton == null)
        {
            Transform t = FindChild(navBar, "StartGameBtn");
            if (t != null) startButton = t.GetComponent<Button>();
        }
        if (startButton == null)
        {
            float p = 0.015f;
            startButton = MakeButton("StartGameBtn", navBar, "Start  \u25B6", 0.70f + p, p, 1f - p, 1f - p, startButtonColor);
        }
        startButton.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ wire onClick

    private void WireButtons()
    {
        prevButton.onClick.RemoveAllListeners();
        prevButton.onClick.AddListener(PreviousSlide);

        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(NextSlide);

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(LoadGameScene);
    }

    // ------------------------------------------------------------------ navigation

    public void NextSlide()
    {
        Debug.Log("[HelpSlideshow] NextSlide clicked");
        if (slides != null && currentIndex < slides.Length - 1)
            ShowSlide(currentIndex + 1);
    }

    public void PreviousSlide()
    {
        Debug.Log("[HelpSlideshow] PreviousSlide clicked");
        if (currentIndex > 0)
            ShowSlide(currentIndex - 1);
    }

    public void ShowSlide(int index)
    {
        if (slides == null || slides.Length == 0)
        {
            UpdateCounter();
            return;
        }

        currentIndex = Mathf.Clamp(index, 0, slides.Length - 1);

        if (slides[currentIndex] != null)
        {
            slideImage.sprite = slides[currentIndex];
            slideImage.color = Color.white;
        }
        else
        {
            slideImage.sprite = null;
            slideImage.color = Color.clear;
        }

        bool isFirst = currentIndex == 0;
        bool isLast = currentIndex >= slides.Length - 1;

        if(currentIndex == 0)
        {
            prevButton.gameObject.SetActive(false);
        }
        else
        {
            prevButton.gameObject.SetActive(true);
        }

        prevButton.interactable = !isFirst;
        TintButton(prevButton, isFirst ? disabledColor : buttonColor);

        // visibility
        nextButton.gameObject.SetActive(!isLast);
        startButton.gameObject.SetActive(isLast);

        nextButton.interactable = !isLast;
        startButton.interactable = isLast;

        //Image nextImg = nextButton.GetComponent<Image>();
        //Image startImg = startButton.GetComponent<Image>();

        //if (nextImg != null) nextImg.raycastTarget = !isLast;
        //if (startImg != null) startImg.raycastTarget = isLast;

        UpdateCounter();
    }

    public void LoadGameScene()
    {
        Debug.Log("[HelpSlideshow] LoadGameScene clicked");
        if (useBuildIndex)
            SceneManager.LoadScene(gameBuildIndex);
        else
            SceneManager.LoadScene(gameSceneName);
    }

    // ------------------------------------------------------------------ counter

    private void UpdateCounter()
    {
        if (counterText == null) return;
        int total = slides != null ? slides.Length : 0;
        counterText.text = total > 0
            ? $"{currentIndex + 1} / {total}"
            : "No slides";
    }

    // ------------------------------------------------------------------ UI helpers

    private static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        Transform t = parent.Find(name);
        if (t != null) return t;
        for (int i = 0; i < parent.childCount; i++)
        {
            t = FindChild(parent.GetChild(i), name);
            if (t != null) return t;
        }
        return null;
    }

    private static void TintButton(Button btn, Color col)
    {
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = col;
    }

    private static GameObject MakeElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void StretchFill(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(GameObject obj, float xMin, float yMin, float xMax, float yMax)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private Button MakeButton(string name, Transform parent, string label,
        float xMin, float yMin, float xMax, float yMax, Color bgColor)
    {
        GameObject btnObj = MakeElement(name, parent);
        SetAnchors(btnObj, xMin, yMin, xMax, yMax);

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        btn.colors = cb;

        GameObject textObj = MakeElement("Label", btnObj.transform);
        StretchFill(textObj);
        TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = buttonFontSize;
        txt.color = textColor;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Bold;
        txt.raycastTarget = false;

        return btn;
    }
}
