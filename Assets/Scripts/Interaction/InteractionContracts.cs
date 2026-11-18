using System.Collections.Generic;
using UnityEngine;

namespace SimsMock.Interaction
{
    public enum InteractionState { Idle, Requested, Moving, Validating, Executing, Completed, Failed, Cancelled }
    public enum InteractionKind { Sit, Work }

    public readonly struct InteractionContext
    {
        public readonly Transform Actor;
        public readonly IInteractable Target;
        public InteractionContext(Transform actor, IInteractable target) { Actor = actor; Target = target; }
    }

    public interface IInteractable
    {
        string DisplayName { get; }
        Transform InteractionTransform { get; }
        IReadOnlyList<InteractionDefinition> GetAvailableInteractions(in InteractionContext context);
        bool CanExecute(InteractionDefinition definition, in InteractionContext context, out string reason);
        void Execute(InteractionDefinition definition, in InteractionContext context);
    }

    public interface IInteractionScanner { IInteractable CurrentTarget { get; } void Scan(Transform origin, float range); }
    public interface IMovementController { bool IsMoving { get; } void MoveTo(Vector3 destination, System.Action<bool> onArrived); void Stop(); }
    public interface IInteractionExecutor { InteractionState State { get; } string StatusMessage { get; } void Request(IInteractable target, InteractionDefinition definition); void Cancel(); }
}
