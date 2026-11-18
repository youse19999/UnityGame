using UnityEngine;
using UnityEngine.UIElements;
using SimsMock.Interaction;

[RequireComponent(typeof(UIDocument))]
public sealed class InteractionPrototypeController : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Camera viewCamera;
    [SerializeField] MonoBehaviour scannerBehaviour;
    [SerializeField] MonoBehaviour executorBehaviour;
    [SerializeField] float rayDistance = 100f;
    IInteractionScanner scanner;
    IInteractionExecutor executor;
    IInteractable menuTarget;
    bool menuOpen;
    bool moveMenu;
    Vector3 moveDestination;
    UIDocument uiDocument;
    VisualElement uiRoot;
    Label selectedLabel;
    Label statusLabel;
    int lastScreenWidth;
    int lastScreenHeight;

    void Awake()
    {
        scanner = scannerBehaviour as IInteractionScanner;
        executor = executorBehaviour as IInteractionExecutor;
        if (viewCamera == null) viewCamera = Camera.main;
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null) uiRoot = uiDocument.rootVisualElement;
        BuildUI();
    }

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null) uiRoot = uiDocument.rootVisualElement;
        BuildUI();
    }

    public void Configure(Transform actor, Camera camera, MonoBehaviour scannerComponent, MonoBehaviour executorComponent)
    {
        player = actor;
        viewCamera = camera;
        scannerBehaviour = scannerComponent;
        executorBehaviour = executorComponent;
        scanner = scannerComponent as IInteractionScanner;
        executor = executorComponent as IInteractionExecutor;
        RefreshUI();
    }

    void Update()
    {
        if (player == null || scanner == null || executor == null) return;
        if (selectedLabel != null)
            selectedLabel.text = "選択中: " + (IsAlive(scanner.CurrentTarget) ? scanner.CurrentTarget.DisplayName : "なし");
        if (statusLabel != null)
            statusLabel.text = "状態: " + executor.StatusMessage + "   (Esc: キャンセル)";
        if (menuOpen && (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)) RefreshUI();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (executor.State == InteractionState.Moving || executor.State == InteractionState.Executing) executor.Cancel();
            if (menuOpen) { menuOpen = false; RefreshUI(); }
        }
        if (executor.State == InteractionState.Executing || !Input.GetMouseButtonDown(0) || IsPointerOverUI()) return;
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera == null) return;
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            menuOpen = false; scanner.Select(null); RefreshUI(); return;
        }
        IInteractable interactable = null;
        foreach (var component in hit.collider.GetComponentsInParent<MonoBehaviour>())
        {
            if (component is IInteractable candidate) { interactable = candidate; break; }
        }
        if (interactable != null)
        {
            if (executor.State == InteractionState.Moving) executor.Cancel();
            scanner.Select(interactable);
            menuTarget = interactable;
            moveMenu = false;
            menuOpen = true;
            RefreshUI();
            return;
        }
        var ground = hit.collider.GetComponentInParent<GroundMoveTarget>();
        if (ground != null)
        {
            scanner.Select(null);
            menuTarget = null;
            moveDestination = hit.point;
            moveMenu = true;
            menuOpen = true;
            RefreshUI();
            return;
        }
        menuOpen = false; scanner.Select(null); RefreshUI();
    }

    static bool IsAlive(IInteractable target) { return target != null && (target as Object) != null; }

    bool IsPointerOverUI()
    {
        var pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (new Rect(0, 0, 450, 150).Contains(pointer)) return true;
        if (!menuOpen) return false;
        float diameter = GetPieDiameter();
        return new Rect((Screen.width - diameter) * .5f, (Screen.height - diameter) * .5f, diameter, diameter).Contains(pointer);
    }

    void RefreshUI()
    {
        if (uiRoot != null) BuildUI();
    }

    void BuildUI()
    {
        if (uiRoot == null) return;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        uiRoot.Clear();
        uiRoot.style.position = Position.Absolute;
        uiRoot.style.left = 0; uiRoot.style.top = 0; uiRoot.style.right = 0; uiRoot.style.bottom = 0;
        uiRoot.pickingMode = PickingMode.Ignore;

        var hud = new VisualElement();
        hud.style.position = Position.Absolute;
        hud.style.left = 16; hud.style.top = 16; hud.style.width = 430;
        hud.style.paddingLeft = 14; hud.style.paddingRight = 14; hud.style.paddingTop = 12; hud.style.paddingBottom = 12;
        hud.style.backgroundColor = new Color(0.06f, 0.09f, 0.13f, 0.9f);
        hud.style.borderTopLeftRadius = 10; hud.style.borderTopRightRadius = 10;
        hud.style.borderBottomLeftRadius = 10; hud.style.borderBottomRightRadius = 10;
        hud.pickingMode = PickingMode.Position;
        hud.Add(MakeLabel("Sims Mock Interaction Prototype", 18, true));
        hud.Add(MakeLabel("家具を左クリックして行動 / 地面を左クリックして移動", 13, false));
        selectedLabel = MakeLabel("選択中: なし", 14, false); hud.Add(selectedLabel);
        statusLabel = MakeLabel("状態: 待機中", 14, false); hud.Add(statusLabel);
        uiRoot.Add(hud);

        if (!menuOpen) return;
        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0; overlay.style.top = 0; overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.justifyContent = Justify.Center;
        overlay.style.alignItems = Align.Center;
        overlay.pickingMode = PickingMode.Ignore;
        var menu = new VisualElement();
        float diameter = GetPieDiameter();
        menu.style.position = Position.Relative;
        menu.style.width = diameter; menu.style.height = diameter;
        menu.pickingMode = PickingMode.Ignore;

        var ring = new VisualElement();
        float ringDiameter = diameter * .78f;
        ring.style.position = Position.Absolute;
        ring.style.width = ringDiameter; ring.style.height = ringDiameter;
        ring.style.left = (diameter - ringDiameter) * .5f; ring.style.top = (diameter - ringDiameter) * .5f;
        ring.style.borderTopLeftRadius = ringDiameter * .5f; ring.style.borderTopRightRadius = ringDiameter * .5f;
        ring.style.borderBottomLeftRadius = ringDiameter * .5f; ring.style.borderBottomRightRadius = ringDiameter * .5f;
        ring.style.borderTopWidth = 1; ring.style.borderRightWidth = 1; ring.style.borderBottomWidth = 1; ring.style.borderLeftWidth = 1;
        ring.style.borderTopColor = new Color(.35f,.48f,.78f,.65f); ring.style.borderRightColor = new Color(.35f,.48f,.78f,.65f);
        ring.style.borderBottomColor = new Color(.35f,.48f,.78f,.65f); ring.style.borderLeftColor = new Color(.35f,.48f,.78f,.65f);
        ring.pickingMode = PickingMode.Ignore;
        menu.Add(ring);

        var center = new VisualElement();
        float centerSize = diameter * .28f;
        center.style.position = Position.Absolute;
        center.style.width = centerSize; center.style.height = centerSize;
        center.style.left = (diameter - centerSize) * .5f; center.style.top = (diameter - centerSize) * .5f;
        center.style.justifyContent = Justify.Center; center.style.alignItems = Align.Center;
        center.style.backgroundColor = new Color(.13f,.17f,.29f,.98f);
        center.style.borderTopLeftRadius = centerSize * .5f; center.style.borderTopRightRadius = centerSize * .5f;
        center.style.borderBottomLeftRadius = centerSize * .5f; center.style.borderBottomRightRadius = centerSize * .5f;
        center.style.borderTopWidth = 3; center.style.borderRightWidth = 3; center.style.borderBottomWidth = 3; center.style.borderLeftWidth = 3;
        center.style.borderTopColor = new Color(.42f,.58f,.95f); center.style.borderRightColor = new Color(.42f,.58f,.95f);
        center.style.borderBottomColor = new Color(.42f,.58f,.95f); center.style.borderLeftColor = new Color(.42f,.58f,.95f);
        center.pickingMode = PickingMode.Ignore;
        string centerTitle = moveMenu ? "移動先" : (IsAlive(menuTarget) ? menuTarget.DisplayName : "対象");
        center.Add(MakeLabel(moveMenu ? "⌖" : "●", 25, true));
        var centerName = MakeLabel(centerTitle, Mathf.Clamp(Mathf.RoundToInt(diameter * .033f), 12, 18), true);
        centerName.style.unityTextAlign = TextAnchor.MiddleCenter;
        centerName.style.whiteSpace = WhiteSpace.Normal;
        center.Add(centerName);
        center.Add(MakeLabel(moveMenu ? "地点を選択" : "行動を選択", 10, false));
        menu.Add(center);

        if (moveMenu)
        {
            AddPieButton(menu, "ここに移動", 1, 0, diameter, () =>
            { menuOpen = false; RefreshUI(); executor.RequestMove(moveDestination); });
        }
        else if (IsAlive(menuTarget))
        {
            var context = new InteractionContext(player, menuTarget);
            var choices = menuTarget.GetAvailableInteractions(context);
            for (int i = 0; i < choices.Count; i++)
            {
                var definition = choices[i];
                if (definition == null) continue;
                var selectedDefinition = definition;
                var selectedTarget = menuTarget;
                AddPieButton(menu, selectedDefinition.DisplayName, choices.Count, i, diameter, () =>
                {
                    menuOpen = false; RefreshUI();
                    executor.Request(selectedTarget, selectedDefinition);
                });
            }
            if (choices.Count == 0)
            {
                var empty = MakeLabel("行動なし", 13, false);
                empty.style.position = Position.Absolute; empty.style.left = 0; empty.style.right = 0;
                empty.style.top = diameter - 30; empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                menu.Add(empty);
            }
        }
        overlay.Add(menu);
        uiRoot.Add(overlay);
    }

    float GetPieDiameter()
    {
        return Mathf.Clamp(Mathf.Min(Screen.width - 32, Screen.height - 80), 320, 540);
    }

    void AddPieButton(VisualElement menu, string title, int count, int index, float diameter, System.Action onClicked)
    {
        float angle = -90f + index * (360f / Mathf.Max(1, count));
        float radians = angle * Mathf.Deg2Rad;
        float center = diameter * .5f;
        float orbit = diameter * .36f;
        float buttonWidth = Mathf.Min(diameter * .38f, 198f);
        const float buttonHeight = 38f;
        float x = center + Mathf.Cos(radians) * orbit - buttonWidth * .5f;
        float y = center + Mathf.Sin(radians) * orbit - buttonHeight * .5f;
        var button = MakeButton(title);
        button.style.position = Position.Absolute;
        button.style.left = Mathf.Clamp(x, 0, diameter - buttonWidth);
        button.style.top = Mathf.Clamp(y, 0, diameter - buttonHeight);
        button.style.width = buttonWidth;
        button.style.height = buttonHeight;
        button.style.paddingLeft = 16; button.style.paddingRight = 16;
        button.style.backgroundColor = new Color(.18f,.24f,.56f,.98f);
        button.style.borderTopColor = new Color(.53f,.62f,1f); button.style.borderRightColor = new Color(.53f,.62f,1f);
        button.style.borderBottomColor = new Color(.06f,.09f,.28f); button.style.borderLeftColor = new Color(.53f,.62f,1f);
        button.style.borderTopWidth = 1; button.style.borderLeftWidth = 1; button.style.borderRightWidth = 1; button.style.borderBottomWidth = 2;
        button.style.unityTextAlign = TextAnchor.MiddleCenter;
        button.clicked += onClicked;
        button.RegisterCallback<PointerEnterEvent>(_ =>
        {
            button.style.backgroundColor = new Color(.03f,.5f,.16f,1f);
            button.style.borderTopColor = new Color(.35f,1f,.43f);
            button.style.borderLeftColor = new Color(.35f,1f,.43f);
            button.style.borderRightColor = new Color(.35f,1f,.43f);
            button.style.color = new Color(.65f,1f,.68f);
        });
        button.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            button.style.backgroundColor = new Color(.18f,.24f,.56f,.98f);
            button.style.borderTopColor = new Color(.53f,.62f,1f);
            button.style.borderLeftColor = new Color(.53f,.62f,1f);
            button.style.borderRightColor = new Color(.53f,.62f,1f);
            button.style.color = Color.white;
        });
        menu.Add(button);
    }

    static Label MakeLabel(string text, int size, bool bold)
    {
        var label = new Label(text);
        label.style.color = Color.white; label.style.fontSize = size;
        label.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
        label.style.marginBottom = 5;
        return label;
    }

    static Button MakeButton(string text)
    {
        var button = new Button { text = text };
        button.style.height = 38; button.style.marginTop = 5; button.style.marginBottom = 5;
        button.style.paddingLeft = 10; button.style.paddingRight = 10;
        button.style.color = Color.white; button.style.fontSize = 15;
        button.style.backgroundColor = new Color(.12f,.29f,.4f);
        button.style.borderTopLeftRadius = 5; button.style.borderTopRightRadius = 5;
        button.style.borderBottomLeftRadius = 5; button.style.borderBottomRightRadius = 5;
        return button;
    }
}
