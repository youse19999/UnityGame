using System.Collections.Generic;
using UnityEngine;
using SimsMock.Interaction;

public sealed class MockInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] string displayName = "Object";
    [SerializeField] Transform interactionPoint;
    [SerializeField] List<InteractionDefinition> interactions = new List<InteractionDefinition>();
    [SerializeField] int progress;
    Renderer objectRenderer;
    Color baseColor;
    public string DisplayName => displayName;
    public Transform InteractionTransform => interactionPoint != null ? interactionPoint : transform;
    public int Progress => progress;
    public void Configure(string title, List<InteractionDefinition> definitions)
    { displayName = title; interactions = definitions; interactionPoint = transform.Find("Interaction Point"); }
    void Awake() { objectRenderer = GetComponentInChildren<Renderer>(); if (objectRenderer != null) baseColor = objectRenderer.material.color; }
    public IReadOnlyList<InteractionDefinition> GetAvailableInteractions(in InteractionContext context) => interactions;
    public bool CanExecute(InteractionDefinition definition, in InteractionContext context, out string reason)
    {
        if (definition == null) { reason = "行動定義がありません"; return false; }
        if (context.Actor == null) { reason = "実行者がいません"; return false; }
        if (!interactions.Contains(definition)) { reason = "この対象では実行できません"; return false; }
        reason = string.Empty; return true;
    }
    public void Execute(InteractionDefinition definition, in InteractionContext context)
    { progress += definition.ValueChange; if (objectRenderer != null) objectRenderer.material.color = definition.Kind == InteractionKind.Sit ? new Color(.25f,.75f,.9f) : new Color(.95f,.65f,.25f); }
    public void SetFocused(bool focused)
    { if (objectRenderer != null) objectRenderer.material.color = focused ? Color.Lerp(baseColor, Color.yellow, .55f) : baseColor; }
    void OnDrawGizmos()
    { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(InteractionTransform.position, .18f); }
}
