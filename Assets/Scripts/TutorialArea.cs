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
    public float instructionsFontSize = 17f;
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

        // Touch damage without attack animation: explicit policy (not "missing EnemySeekAndAttack")
        EnemyAttackDamageWindow dmgWindow = dummyEnemy.GetComponent<EnemyAttackDamageWindow>();
        if (dmgWindow == null)
            dmgWindow = dummyEnemy.AddComponent<EnemyAttackDamageWindow>();
        dmgWindow.contactMode = EnemyContactDamageMode.AnyContact;
        dmgWindow.useNormalizedTimeFallback = false;
        if (dmgWindow.attackDamageCollider != null)
            dmgWindow.attackDamageCollider.enabled = false;

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
        canvasRect.sizeDelta = new Vector2(650, 560);
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
        titleText.text = "PRACTICE — HOW TO PLAY";
        titleText.fontSize = titleFontSize;
        titleText.color = new Color(0f, 1f, 0.8f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;

        // Instructions text
        GameObject instrObj = new GameObject("Instructions");
        instrObj.transform.SetParent(bgObj.transform, false);
        RectTransform instrRect = instrObj.AddComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0, 0.04f);
        instrRect.anchorMax = new Vector2(1, 0.74f);
        instrRect.offsetMin = new Vector2(15, 5);
        instrRect.offsetMax = new Vector2(-15, -5);
        TextMeshProUGUI instrText = instrObj.AddComponent<TextMeshProUGUI>();
        instrText.text =
            "<color=#88DDFF>New to VR (Meta Quest)?</color>\n" +
            "Clear your play space. Wear the headset comfortably. Use <color=#FFD700>grip</color> (squeeze the side of the controller) to pick up most objects—like holding something in your real hand.\n\n" +
            "<color=#FFD700>MOVE:</color> <b>Left thumbstick</b> — walk forward/back and strafe. Direction follows where you look.\n" +
            "<color=#FFD700>TURN:</color> <b>Right thumbstick</b> left/right — snap-turn your view (when enabled).\n\n" +
            "<color=#FFD700>GRAB:</color> Hold <b>grip</b> near an object to pick it up.\n" +
            "<color=#FFD700>THROW:</color> Swing and <b>release grip</b> to throw; weak tosses may not count as attacks.\n" +
            "<color=#FFD700>MELEE:</color> Hold a weapon and swing so the <b>blade</b> hits enemies.\n" +
            "<color=#FFD700>THROWABLES:</color> Building blocks hurt enemies when thrown with enough speed—not when slowly rolling.\n\n" +
            "<color=#88DDFF>In the full game</color>\n" +
            "• <b>Rounds</b> — defeat every enemy to finish a round; difficulty scales up.\n" +
            "• <b>Your health</b> — enemies can hurt you on contact or with attacks; you get short <b>invulnerability</b> after taking damage.\n" +
            "• <b>Weapon durability</b> — while holding a melee weapon, a HUD icon shows remaining hits; weapons can <b>break</b> and later respawn.\n" +
            "• <b>Healing apples</b> — green apples heal <b>you</b> when they hit an enemy (they do not damage the enemy).\n" +
            "• Dropped weapons can show on the <b>minimap</b> so you can find them again.\n\n" +
            "<color=#44FF88>This dummy has unlimited health</color> — use it to practice with no pressure.";
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
