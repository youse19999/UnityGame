using UnityEngine;
using SimsMock.Interaction;

[CreateAssetMenu(menuName = "Sims Mock/Interaction Definition")]
public sealed class InteractionDefinition : ScriptableObject
{
    [SerializeField] string displayName = "Interact";
    [SerializeField, TextArea] string description;
    [SerializeField] InteractionKind kind;
    [SerializeField, Min(0.1f)] float duration = 2f;
    [SerializeField, Min(0.1f)] float requiredDistance = 1.25f;
    [SerializeField] int valueChange = 1;
    public string DisplayName => displayName;
    public string Description => description;
    public InteractionKind Kind => kind;
    public float Duration => duration;
    public float RequiredDistance => requiredDistance;
    public int ValueChange => valueChange;
    public void Configure(string title, string details, InteractionKind interactionKind, float seconds, float distance, int change)
    { displayName = title; description = details; kind = interactionKind; duration = seconds; requiredDistance = distance; valueChange = change; }
}
