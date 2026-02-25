using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Creates a tutorial/practice area in the Start Menu scene.
/// Spawns a dummy enemy with unlimited health for combat practice.
/// Place on a GameObject in the Start Menu scene.
/// </summary>
public class TutorialArea : MonoBehaviour
{
    [Header("Tutorial Area Position")]
    [Tooltip("World position for the tutorial area center")]
    public Vector3 tutorialCenter = new Vector3(3f, 0f, 0f);

    [Tooltip("If set, tutorial area is relative to this transform")]
    public Transform referencePoint;

    [Header("Dummy Enemy")]
    [Tooltip("Enemy prefab to instantiate as dummy. If null, creates a procedural dummy.")]
    public GameObject enemyPrefab;

    [Tooltip("Position offset for the dummy enemy relative to tutorial center")]
    public Vector3 dummyEnemyOffset = new Vector3(0f, 0f, 2f);

    [Header("UI")]
    [Tooltip("Show tutorial instructions panel")]
    public bool showInstructions = true;

    [Header("Font Sizes")]
    public float titleFontSize = 36f;
    public float instructionsFontSize = 20f;
    public float dummyLabelFontSize = 28f;

    private GameObject dummyEnemy;
    private GameObject tutorialPanel;
    private EnemyController dummyController;

    void Start()
    {
        Vector3 center = tutorialCenter;
        if (referencePoint != null)
            center = referencePoint.position + referencePoint.TransformDirection(tutorialCenter);

        SpawnDummyEnemy(center);

        if (showInstructions)
            CreateInstructionPanel(center);
    }

    void Update()
    {
        // Respawn dummy if destroyed somehow
        if (dummyEnemy == null)
        {
            Vector3 center = tutorialCenter;
            if (referencePoint != null)
                center = referencePoint.position + referencePoint.TransformDirection(tutorialCenter);
            SpawnDummyEnemy(center);
        }
    }

    private void SpawnDummyEnemy(Vector3 center)
    {
        Vector3 spawnPos = center + dummyEnemyOffset;

        if (enemyPrefab != null)
        {
            dummyEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            dummyEnemy.name = "TutorialDummy";
        }
        else
        {
            dummyEnemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummyEnemy.name = "TutorialDummy";
            dummyEnemy.transform.position = spawnPos;
            dummyEnemy.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

            Renderer rend = dummyEnemy.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.8f, 0.2f, 0.2f, 1f);
                rend.material = mat;
            }

            Rigidbody rb = dummyEnemy.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        dummyEnemy.tag = "Enemy";

        // Ensure EnemyController for combat interaction
        dummyController = dummyEnemy.GetComponent<EnemyController>();
        if (dummyController == null)
            dummyController = dummyEnemy.AddComponent<EnemyController>();
        dummyController.maxHealth = 100f;
        dummyController.currentHealth = 100f;
        dummyController.isInvulnerable = true;

        // Add combat system so it reacts to hits
        CombatSystem cs = dummyEnemy.GetComponent<CombatSystem>();
        if (cs == null)
            cs = dummyEnemy.AddComponent<CombatSystem>();
        cs.instakillEnabled = false;

        // Add health bar for visual feedback
        EnemyHealthBar hpBar = dummyEnemy.GetComponent<EnemyHealthBar>();
        if (hpBar == null)
            hpBar = dummyEnemy.AddComponent<EnemyHealthBar>();
        hpBar.hideAtFullHealth = false;

        // Disable seek-and-attack so dummy stands still
        EnemySeekAndAttack seek = dummyEnemy.GetComponent<EnemySeekAndAttack>();
        if (seek != null)
            seek.enabled = false;

        // Disable NavMeshAgent if present
        UnityEngine.AI.NavMeshAgent agent = dummyEnemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
            agent.enabled = false;

        // Floating label
        CreateDummyLabel(dummyEnemy.transform);

        Debug.Log($"[TutorialArea] Spawned dummy enemy at {spawnPos}");
    }

    private void CreateInstructionPanel(Vector3 center)
    {
        Vector3 panelPos = center + new Vector3(0f, 2f, 1.5f);

        GameObject canvasObj = new GameObject("TutorialInstructionCanvas");
        canvasObj.transform.position = panelPos;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(600, 400);
        canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.15f, 0.75f);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(bgObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.75f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, -10);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "TUTORIAL AREA";
        titleText.fontSize = titleFontSize;
        titleText.color = new Color(0f, 1f, 0.8f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;

        // Instructions text
        GameObject instrObj = new GameObject("Instructions");
        instrObj.transform.SetParent(bgObj.transform, false);
        RectTransform instrRect = instrObj.AddComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0, 0.05f);
        instrRect.anchorMax = new Vector2(1, 0.72f);
        instrRect.offsetMin = new Vector2(15, 5);
        instrRect.offsetMax = new Vector2(-15, -5);
        TextMeshProUGUI instrText = instrObj.AddComponent<TextMeshProUGUI>();
        instrText.text =
            "Practice your combat skills!\n\n" +
            "<color=#FFD700>GRAB:</color> Grip button to pick up objects\n" +
            "<color=#FFD700>THROW:</color> Release grip while swinging\n" +
            "<color=#FFD700>MELEE:</color> Grab weapon and swing at enemy\n\n" +
            "The dummy enemy has <color=#FF4444>unlimited health</color>\n" +
            "so practice as much as you want!";
        instrText.fontSize = instructionsFontSize;
        instrText.color = new Color(0.85f, 0.9f, 1f, 0.95f);
        instrText.alignment = TextAlignmentOptions.Left;

        tutorialPanel = canvasObj;
        Debug.Log("[TutorialArea] Created tutorial instruction panel.");
    }

    private void CreateDummyLabel(Transform parent)
    {
        GameObject canvasObj = new GameObject("DummyLabel");
        canvasObj.transform.SetParent(parent, false);
        canvasObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 11;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(300, 60);
        canvasRect.localScale = new Vector3(0.005f, 0.005f, 0.005f);

        GameObject textObj = new GameObject("LabelText");
        textObj.transform.SetParent(canvasObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = textObj.AddComponent<TextMeshProUGUI>();
        label.text = "TRAINING DUMMY";
        label.fontSize = dummyLabelFontSize;
        label.color = new Color(1f, 0.8f, 0f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;

        canvasObj.AddComponent<FaceCameraBillboard>();
    }
}

/// <summary>
/// Makes a UI element always face the main camera (billboard effect).
/// </summary>
public class FaceCameraBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - cam.transform.position
            );
        }
    }
}
