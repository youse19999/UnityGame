using System.Collections.Generic;
using UnityEngine;
using SimsMock.Interaction;

public sealed class InteractionPrototypeController : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] MonoBehaviour scannerBehaviour;
    [SerializeField] MonoBehaviour executorBehaviour;
    [SerializeField] float scanRange = 3f;
    IInteractionScanner scanner;
    IInteractionExecutor executor;
    IInteractable menuTarget;
    bool menuOpen;
    string notice;
    float noticeUntil;
    void Awake() { scanner = scannerBehaviour as IInteractionScanner; executor = executorBehaviour as IInteractionExecutor; }
    public void Configure(Transform actor, MonoBehaviour scannerComponent, MonoBehaviour executorComponent)
    { player = actor; scannerBehaviour = scannerComponent; executorBehaviour = executorComponent; scanner = scannerComponent as IInteractionScanner; executor = executorComponent as IInteractionExecutor; }
    void Update()
    {
        if (player == null || scanner == null || executor == null) return;
        if (executor.State == InteractionState.Moving || executor.State == InteractionState.Executing) { if (Input.GetKeyDown(KeyCode.Escape)) { executor.Cancel(); menuOpen = false; } return; }
        scanner.Scan(player, scanRange);
        if (Input.GetKeyDown(KeyCode.Escape)) menuOpen = false;
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (menuOpen) { menuOpen = false; return; }
            if (scanner.CurrentTarget == null) { ShowNotice("操作できる対象が近くにありません"); return; }
            menuTarget = scanner.CurrentTarget; menuOpen = true;
        }
    }
    static bool IsAlive(IInteractable target) { return target != null && (target as Object) != null; }
    void ShowNotice(string message) { notice = message; noticeUntil = Time.time + 2.5f; }
    void OnGUI()
    {
        GUI.Box(new Rect(12, 12, 340, 132), "Sims Mock Interaction Prototype");
        GUI.Label(new Rect(26, 42, 320, 24), "移動: WASD / 矢印キー     操作: E     キャンセル: Esc");
        string target = scanner != null && scanner.CurrentTarget != null ? scanner.CurrentTarget.DisplayName : "なし";
        GUI.Label(new Rect(26, 70, 310, 22), "フォーカス: " + target);
        if (executor != null) GUI.Label(new Rect(26, 96, 310, 30), "状態: " + executor.StatusMessage);
        if (menuOpen)
        {
            if (!IsAlive(menuTarget)) { menuOpen = false; return; }
            var context = new InteractionContext(player, menuTarget);
            var choices = menuTarget.GetAvailableInteractions(context);
            float width = 280, height = 48, total = choices.Count * height + 54;
            Rect panel = new Rect((Screen.width-width)*.5f, (Screen.height-total)*.5f, width, total);
            GUI.Box(panel, menuTarget.DisplayName + " の行動");
            for (int i = 0; i < choices.Count; i++)
            {
                var item = choices[i]; if (item == null) continue;
                Rect button = new Rect(panel.x + 12, panel.y + 30 + i * height, width - 24, 38);
                if (GUI.Button(button, item.DisplayName + "  ·  " + item.Duration.ToString("0.#") + "秒"))
                { menuOpen = false; executor.Request(menuTarget, item); }
            }
            if (choices.Count == 0) GUI.Label(new Rect(panel.x+16,panel.y+35,width-32,30), "行動がありません");
        }
        if (!string.IsNullOrEmpty(notice) && Time.time < noticeUntil) GUI.Box(new Rect(12, Screen.height-54, 380, 38), notice);
        GUI.Label(new Rect(Screen.width-220, 14, 200, 44), "家具に近づいて E キーで行動");
    }
}
