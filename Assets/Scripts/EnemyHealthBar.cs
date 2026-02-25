using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates and manages a world-space health bar above an enemy.
/// Uses the same visual style as the player HP bar (red fill Image).
/// Automatically attaches to EnemyController and updates in real-time.
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
    public bool hideAtFullHealth = true;
    [Tooltip("Delay before hiding after taking no damage")]
    public float hideDelay = 3f;

    private EnemyController enemyController;
    private Canvas healthCanvas;
    private Image backgroundImage;
    private Image fillImage;
    private GameObject healthBarRoot;
    private float lastDamageTime = -999f;
    private bool isSetup = false;

    void Start()
    {
        enemyController = GetComponent<EnemyController>();
        if (enemyController == null)
        {
            Debug.LogWarning($"[EnemyHealthBar] No EnemyController on {gameObject.name}");
            enabled = false;
            return;
        }

        CreateHealthBar();

        enemyController.OnEnemyDamaged += OnDamaged;
        isSetup = true;
    }

    void OnDestroy()
    {
        if (enemyController != null)
            enemyController.OnEnemyDamaged -= OnDamaged;

        if (healthBarRoot != null)
            Destroy(healthBarRoot);
    }

    private void OnDamaged(EnemyController controller, float damage)
    {
        lastDamageTime = Time.time;
        UpdateFill();
    }

    void LateUpdate()
    {
        if (!isSetup || healthBarRoot == null) return;

        // Position above enemy
        Vector3 worldPos = transform.position + Vector3.up * heightOffset;
        healthBarRoot.transform.position = worldPos;

        // Billboard: always face the camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            healthBarRoot.transform.rotation = Quaternion.LookRotation(
                healthBarRoot.transform.position - cam.transform.position
            );
        }

        // Visibility logic
        if (hideAtFullHealth && enemyController != null)
        {
            bool shouldShow = enemyController.GetHealthPercentage() < 1f;
            if (!shouldShow && hideDelay > 0 && Time.time - lastDamageTime < hideDelay)
                shouldShow = true;

            healthBarRoot.SetActive(shouldShow);
        }
    }

    private void UpdateFill()
    {
        if (fillImage == null || enemyController == null) return;

        float pct = enemyController.GetHealthPercentage();
        fillImage.fillAmount = pct;

        fillImage.color = pct < 0.3f ? lowHealthColor : healthBarColor;
    }

    private void CreateHealthBar()
    {
        healthBarRoot = new GameObject("EnemyHealthBar");
        healthBarRoot.transform.position = transform.position + Vector3.up * heightOffset;

        healthCanvas = healthBarRoot.AddComponent<Canvas>();
        healthCanvas.renderMode = RenderMode.WorldSpace;
        healthCanvas.sortingOrder = 10;

        RectTransform canvasRect = healthBarRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(barWidth * 1000f, barHeight * 1000f);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(healthBarRoot.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = backgroundBarColor;

        // Fill bar
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(healthBarRoot.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = healthBarColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 1f;

        // Border
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(healthBarRoot.transform, false);
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

        if (hideAtFullHealth)
            healthBarRoot.SetActive(false);
    }
}
