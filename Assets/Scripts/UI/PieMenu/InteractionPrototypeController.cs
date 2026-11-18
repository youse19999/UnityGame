using UnityEngine;
using UnityEngine.UIElements;
using SimsMock.Interaction;

[RequireComponent(typeof(PanelRenderer))]
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
    VisualElement uiRoot;
    Label selectedLabel;
    Label statusLabel;

    void Awake()
    {
        scanner = scannerBehaviour as IInteractionScanner;
        executor = executorBehaviour as IInteractionExecutor;
        if (viewCamera == null) viewCamera = Camera.main;
        GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
    }

    void OnDestroy()
    {
        GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
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

    void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement)
    {
        uiRoot = rootElement;
        BuildUI();
    }

    void Update()
    {
        if (player == null || scanner == null || executor == null) return;
        if (selectedLabel != null)
            selectedLabel.text = "選択中: " + (IsAlive(scanner.CurrentTarget) ? scanner.CurrentTarget.DisplayName : "なし");
        if (statusLabel != null)
            statusLabel.text = "状態: " + executor.StatusMessage + "   (Esc: キャンセル)";

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
        int itemCount = moveMenu ? 1 : (IsAlive(menuTarget) ? menuTarget.GetAvailableInteractions(new InteractionContext(player, menuTarget)).Count : 0);
        float height = itemCount * 48 + 68;
        return new Rect((Screen.width - 320) * .5f, (Screen.height - height) * .5f, 320, height).Contains(pointer);
    }

    void RefreshUI()
    {
        if (uiRoot != null) BuildUI();
    }

    void BuildUI()
    {
        if (uiRoot == null) return;
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
        menu.style.width = 320; menu.style.paddingLeft = 14; menu.style.paddingRight = 14;
        menu.style.paddingTop = 12; menu.style.paddingBottom = 12;
        menu.style.backgroundColor = new Color(0.08f, 0.11f, 0.16f, 0.97f);
        menu.style.borderTopLeftRadius = 10; menu.style.borderTopRightRadius = 10;
        menu.style.borderBottomLeftRadius = 10; menu.style.borderBottomRightRadius = 10;
        menu.style.borderTopWidth = 1; menu.style.borderBottomWidth = 1;
        menu.style.borderLeftWidth = 1; menu.style.borderRightWidth = 1;
        menu.style.borderTopColor = new Color(.35f,.55f,.7f); menu.style.borderBottomColor = new Color(.35f,.55f,.7f);
        menu.style.borderLeftColor = new Color(.35f,.55f,.7f); menu.style.borderRightColor = new Color(.35f,.55f,.7f);
        menu.pickingMode = PickingMode.Position;
        menu.Add(MakeLabel(moveMenu ? "地面の操作" : (IsAlive(menuTarget) ? menuTarget.DisplayName + " の行動" : "操作"), 18, true));
        if (moveMenu)
        {
            var moveButton = MakeButton("ここに移動");
            moveButton.clicked += () => { menuOpen = false; RefreshUI(); executor.RequestMove(moveDestination); };
            menu.Add(moveButton);
        }
        else if (IsAlive(menuTarget))
        {
            var context = new InteractionContext(player, menuTarget);
            var choices = menuTarget.GetAvailableInteractions(context);
            foreach (var definition in choices)
            {
                if (definition == null) continue;
                var selectedDefinition = definition;
                var actionButton = MakeButton(selectedDefinition.DisplayName + "   ·   " + selectedDefinition.Duration.ToString("0.#") + "秒");
                actionButton.clicked += () =>
                {
                    var target = menuTarget;
                    menuOpen = false; RefreshUI();
                    executor.Request(target, selectedDefinition);
                };
                menu.Add(actionButton);
            }
            if (choices.Count == 0) menu.Add(MakeLabel("行動がありません", 14, false));
        }
        overlay.Add(menu);
        uiRoot.Add(overlay);
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
