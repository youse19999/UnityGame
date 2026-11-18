using UnityEngine;
using SimsMock.Interaction;

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

    void Awake()
    {
        scanner = scannerBehaviour as IInteractionScanner;
        executor = executorBehaviour as IInteractionExecutor;
        if (viewCamera == null) viewCamera = Camera.main;
    }
    public void Configure(Transform actor, Camera camera, MonoBehaviour scannerComponent, MonoBehaviour executorComponent)
    {
        player = actor; viewCamera = camera; scannerBehaviour = scannerComponent; executorBehaviour = executorComponent;
        scanner = scannerComponent as IInteractionScanner; executor = executorComponent as IInteractionExecutor;
    }
    void Update()
    {
        if (player == null || scanner == null || executor == null) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (executor.State == InteractionState.Moving || executor.State == InteractionState.Executing) executor.Cancel();
            menuOpen = false;
        }
        if (executor.State == InteractionState.Executing || !Input.GetMouseButtonDown(0) || IsPointerOverMenu()) return;
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera == null) return;
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance)) { menuOpen = false; scanner.Select(null); return; }
        IInteractable interactable = null;
        foreach (var component in hit.collider.GetComponentsInParent<MonoBehaviour>())
        {
            if (component is IInteractable candidate) { interactable = candidate; break; }
        }
        if (interactable != null)
        {
            if (executor.State == InteractionState.Moving) executor.Cancel();
            scanner.Select(interactable); menuTarget = interactable; moveMenu = false; menuOpen = true; return;
        }
        var ground = hit.collider.GetComponentInParent<GroundMoveTarget>();
        if (ground != null)
        {
            scanner.Select(null); menuTarget = null; moveDestination = hit.point; moveMenu = true; menuOpen = true; return;
        }
        menuOpen = false; scanner.Select(null);
    }
    static bool IsAlive(IInteractable target) { return target != null && (target as Object) != null; }
    bool IsPointerOverMenu()
    {
        if (!menuOpen) return false;
        float height = (moveMenu ? 1 : (IsAlive(menuTarget) ? menuTarget.GetAvailableInteractions(new InteractionContext(player, menuTarget)).Count : 0)) * 48 + 54;
        var pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return new Rect((Screen.width - 300) * .5f, (Screen.height - height) * .5f, 300, height).Contains(pointer);
    }
    void OnGUI()
    {
        GUI.Box(new Rect(12, 12, 420, 132), "Sims Mock Interaction Prototype");
        GUI.Label(new Rect(26, 42, 400, 24), "家具を左クリックして行動を選択 / 地面を左クリックして移動");
        GUI.Label(new Rect(26, 70, 390, 22), "選択中: " + (scanner != null && IsAlive(scanner.CurrentTarget) ? scanner.CurrentTarget.DisplayName : "なし"));
        if (executor != null) GUI.Label(new Rect(26, 96, 390, 30), "状態: " + executor.StatusMessage + "   (Esc: キャンセル)");
        if (menuOpen)
        {
            if (moveMenu)
            {
                DrawMenu("地面", 1, () => { menuOpen = false; executor.RequestMove(moveDestination); }, "ここに移動");
            }
            else
            {
                if (!IsAlive(menuTarget)) { menuOpen = false; return; }
                var context = new InteractionContext(player, menuTarget);
                var choices = menuTarget.GetAvailableInteractions(context);
                float width = 300, height = choices.Count * 48 + 54;
                Rect panel = new Rect((Screen.width-width)*.5f, (Screen.height-height)*.5f, width, height);
                GUI.Box(panel, menuTarget.DisplayName + " の行動");
                for (int i = 0; i < choices.Count; i++)
                {
                    var item = choices[i]; if (item == null) continue;
                    Rect button = new Rect(panel.x + 12, panel.y + 30 + i * 48, width - 24, 38);
                    if (GUI.Button(button, item.DisplayName + "  ·  " + item.Duration.ToString("0.#") + "秒"))
                    { menuOpen = false; executor.Request(menuTarget, item); }
                }
                if (choices.Count == 0) GUI.Label(new Rect(panel.x+16,panel.y+35,width-32,30), "行動がありません");
            }
        }
    }
    void DrawMenu(string title, int itemCount, System.Action onSelect, string label)
    {
        float width = 300, height = itemCount * 48 + 54;
        Rect panel = new Rect((Screen.width-width)*.5f, (Screen.height-height)*.5f, width, height);
        GUI.Box(panel, title + " の操作");
        if (GUI.Button(new Rect(panel.x+12,panel.y+30,width-24,38),label)) onSelect();
    }
}
