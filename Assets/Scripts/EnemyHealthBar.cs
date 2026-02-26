using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a world-space health bar above an enemy.
/// If a child "HealthBarCanvas" exists in the prefab, it is unparented at runtime
/// so the enemy's own scale (e.g. 0.18) does not shrink the canvas.
/// Falls back to runtime creation if no prefab canvas is found.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    [Tooltip("Height offset above enemy for the health bar")]
    public float heightOffset = 2.2f;

    [Tooltip("Width of the health bar in world units")]
    public float barWidth = 0.8f;

    [Tooltip("Height of the health bar in world units")]
    public float barHeight = 0.1f;

    [Header("Colors")]
    public Color backgroundBarColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color healthBarColor = new Color(0.9f, 0.1f, 0.1f, 1f);
    public Color lowHealthColor = new Color(1f, 0.3f, 0f, 1f);

    [Header("Behavior")]
    [Tooltip("Hide bar when enemy is at full health")]
    public bool hideAtFullHealth = false;
    [Tooltip("Delay before hiding after taking no damage")]
    public float hideDelay = 3f;

    private EnemyController enemyController;
    private RectTransform fillRect;  // Use rect anchors instead of fillAmount for reliable updates
    private Image fillImage;
    private GameObject healthBarRoot;
    private float lastDamageTime = -999f;
    private bool isSetup = false;

    void Start()
    {
        // Use GetComponentInParent so health bar works when on parent/child of EnemyController
        enemyController = GetComponentInParent<EnemyController>();
        if (enemyController == null)
        {
            Debug.LogWarning($"[EnemyHealthBar] No EnemyController on {gameObject.name} or parents");
            enabled = false;
            return;
        }

        SetupHealthBar();
        enemyController.OnEnemyDamaged += OnDamaged;
        isSetup = true;
        UpdateFill();

        if (hideAtFullHealth && enemyController.GetHealthPercentage() >= 1f)
            healthBarRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (enemyController != null)
            enemyController.OnEnemyDamaged -= OnDamaged;

        if (healthBarRoot != null)
            Destroy(healthBarRoot);
    }

    private void SetupHealthBar()
    {
        Transform prefabCanvas = transform.Find("HealthBarCanvas");
        if (prefabCanvas != null)
        {
            healthBarRoot = prefabCanvas.gameObject;
            prefabCanvas.SetParent(null, true);
        }
        else
        {
            healthBarRoot = new GameObject($"HealthBar_{gameObject.name}");
        }

        healthBarRoot.transform.position = transform.position + Vector3.up * heightOffset;
        healthBarRoot.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        Canvas canvas = healthBarRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = healthBarRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        canvas.worldCamera = Camera.main; // Required for World Space canvas to render

        RectTransform canvasRect = healthBarRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(barWidth * 1000f, barHeight * 1000f);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        Transform fillT = healthBarRoot.transform.Find("Fill");
        Transform bgT = healthBarRoot.transform.Find("Background");

        if (fillT == null || bgT == null)
        {
            BuildBarChildren(healthBarRoot.transform);
        }
        else
        {
            fillImage = fillT.GetComponent<Image>();
            fillRect = fillT.GetComponent<RectTransform>();
            Image bgImg = bgT.GetComponent<Image>();
            if (bgImg != null) bgImg.color = backgroundBarColor;

            RectTransform bgRect = bgT.GetComponent<RectTransform>();
            if (bgRect != null)
            {
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                bgRect.localPosition = Vector3.zero;
                bgRect.localScale = Vector3.one;
            }

            if (fillRect != null)
            {
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(1f, 1f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.offsetMin = new Vector2(2, 2);
                fillRect.offsetMax = new Vector2(-2, -2);
                fillRect.localPosition = Vector3.zero;
                fillRect.localScale = Vector3.one;
            }

            Transform borderT = healthBarRoot.transform.Find("Border");
            if (borderT != null)
            {
                RectTransform borderRect = borderT.GetComponent<RectTransform>();
                if (borderRect != null)
                {
                    borderRect.anchorMin = Vector2.zero;
                    borderRect.anchorMax = Vector2.one;
                    borderRect.offsetMin = Vector2.zero;
                    borderRect.offsetMax = Vector2.zero;
                    borderRect.localPosition = Vector3.zero;
                    borderRect.localScale = Vector3.one;
                }
            }
        }

        if (fillImage != null)
        {
            fillImage.color = healthBarColor;
            fillImage.type = Image.Type.Simple;  // Use Simple + rect anchors for reliable bar
        }
        if (fillRect == null)
            fillRect = healthBarRoot.transform.Find("Fill")?.GetComponent<RectTransform>();
    }

    private void BuildBarChildren(Transform parent)
    {
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(parent, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = backgroundBarColor;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(parent, false);
        this.fillRect = fillObj.AddComponent<RectTransform>();
        this.fillRect.anchorMin = new Vector2(0f, 0f);
        this.fillRect.anchorMax = new Vector2(1f, 1f);
        this.fillRect.pivot = new Vector2(0f, 0.5f);
        this.fillRect.offsetMin = new Vector2(2, 2);
        this.fillRect.offsetMax = new Vector2(-2, -2);
        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = healthBarColor;
        fillImage.type = Image.Type.Simple;

        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(parent, false);
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(0, 0, 0, 0);
        Outline outline = borderObj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.7f);
        outline.effectDistance = new Vector2(1, 1);
    }

    private void OnDamaged(EnemyController controller, float damage)
    {
        lastDamageTime = Time.time;
        UpdateFill();
    }

    void LateUpdate()
    {
        if (!isSetup || healthBarRoot == null) return;
        // Refresh world camera in case it was null at Start (e.g. XR camera not ready)
        Canvas c = healthBarRoot.GetComponent<Canvas>();
        if (c != null && c.worldCamera == null)
            c.worldCamera = Camera.main;
        if (enemyController == null || !enemyController.IsAlive())
        {
            if (healthBarRoot != null)
                healthBarRoot.SetActive(false);
            return;
        }

        // Always sync fill with current health (handles melee, projectiles, any damage source)
        UpdateFill();

        healthBarRoot.transform.position = transform.position + Vector3.up * heightOffset;

        Camera cam = Camera.main;
        if (cam != null)
        {
            healthBarRoot.transform.rotation = Quaternion.LookRotation(
                healthBarRoot.transform.position - cam.transform.position
            );
        }

        bool shouldShow = true;
        if (hideAtFullHealth)
        {
            shouldShow = enemyController.GetHealthPercentage() < 1f;
            if (!shouldShow && hideDelay > 0 && Time.time - lastDamageTime < hideDelay)
                shouldShow = true;
        }
        healthBarRoot.SetActive(shouldShow);
    }

    private void UpdateFill()
    {
        if (enemyController == null) return;

        float pct = Mathf.Clamp01(enemyController.GetHealthPercentage());

        // Use rect anchors for reliable bar (fillAmount can fail with World Space canvas)
        if (fillRect != null)
        {
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(pct, 1f);
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
        }

        if (fillImage != null)
            fillImage.color = pct < 0.3f ? lowHealthColor : healthBarColor;
    }
}
