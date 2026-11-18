using System.Collections;
using UnityEngine;
using SimsMock.Interaction;

public sealed class InteractionExecutor : MonoBehaviour, IInteractionExecutor
{
    [SerializeField] Transform actor;
    [SerializeField] MonoBehaviour movementBehaviour;
    IMovementController movement;
    Coroutine routine;
    int requestId;
    public InteractionState State { get; private set; } = InteractionState.Idle;
    public string StatusMessage { get; private set; } = "近くの家具に近づいて E キー";
    void Awake() { movement = movementBehaviour as IMovementController; }
    public void Configure(Transform actorTransform, MonoBehaviour mover) { actor = actorTransform; movementBehaviour = mover; movement = mover as IMovementController; }
    public void Request(IInteractable target, InteractionDefinition definition)
    {
        Cancel(); int id = ++requestId; routine = StartCoroutine(Run(target, definition, id));
    }
    IEnumerator Run(IInteractable target, InteractionDefinition definition, int id)
    {
        State = InteractionState.Requested; StatusMessage = "行動を準備中…";
        if (!Alive(target)) { Fail("対象が見つかりません"); yield break; }
        Vector3 targetPosition = target.InteractionTransform.position;
        Vector3 away = actor.position - targetPosition; away.y = 0; if (away.sqrMagnitude < .001f) away = Vector3.forward; away.Normalize();
        Vector3 destination = targetPosition + away * Mathf.Max(.65f, definition.RequiredDistance * .8f); destination.y = actor.position.y;
        if (Vector3.Distance(actor.position, targetPosition) > definition.RequiredDistance)
        {
            if (movement == null) { Fail("移動コントローラーがありません"); yield break; }
            bool arrived = false; bool success = false;
            State = InteractionState.Moving; StatusMessage = target.DisplayName + " まで移動中…";
            movement.MoveTo(destination, value => { success = value; arrived = true; });
            while (!arrived && id == requestId && Alive(target)) yield return null;
            if (id != requestId) yield break;
            if (!Alive(target)) { Fail("移動中に対象が破棄されました"); yield break; }
            if (!success) { Fail("移動できませんでした"); yield break; }
        }
        State = InteractionState.Validating; StatusMessage = "行動条件を再確認中…";
        if (!Alive(target)) { Fail("対象が見つかりません"); yield break; }
        var context = new InteractionContext(actor, target);
        if (!target.CanExecute(definition, context, out string reason)) { Fail(string.IsNullOrEmpty(reason) ? "実行できません" : reason); yield break; }
        if (Vector3.Distance(actor.position, target.InteractionTransform.position) > definition.RequiredDistance + .15f) { Fail("対象との距離が足りません"); yield break; }
        State = InteractionState.Executing; StatusMessage = definition.DisplayName + " 実行中…";
        yield return new WaitForSeconds(definition.Duration);
        if (id != requestId) yield break;
        if (!Alive(target)) { Fail("実行中に対象が破棄されました"); yield break; }
        context = new InteractionContext(actor, target); target.Execute(definition, context);
        State = InteractionState.Completed; StatusMessage = definition.DisplayName + " 完了"; routine = null;
    }
    static bool Alive(IInteractable target) { return target != null && (target as Object) != null; }
    void Fail(string message) { State = InteractionState.Failed; StatusMessage = message; routine = null; }
    public void Cancel()
    {
        requestId++;
        if (routine != null) { StopCoroutine(routine); routine = null; }
        if (movement != null) movement.Stop();
        if (State == InteractionState.Moving || State == InteractionState.Executing || State == InteractionState.Requested || State == InteractionState.Validating)
        { State = InteractionState.Cancelled; StatusMessage = "行動をキャンセルしました"; }
    }
}
