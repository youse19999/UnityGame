using UnityEngine;
using SimsMock.Interaction;

public sealed class MockInteractionScanner : MonoBehaviour, IInteractionScanner
{
    IInteractable current;
    MockInteractable focused;
    public IInteractable CurrentTarget => current;
    public void Select(IInteractable target)
    {
        var next = target as MockInteractable;
        if (focused == next) { current = target; return; }
        if (focused != null) focused.SetFocused(false);
        focused = next; current = target;
        if (focused != null) focused.SetFocused(true);
    }
}
