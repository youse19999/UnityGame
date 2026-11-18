using UnityEngine;
using SimsMock.Interaction;

public sealed class MockInteractionScanner : MonoBehaviour, IInteractionScanner
{
    [SerializeField] LayerMask layers = ~0;
    IInteractable current;
    MockInteractable focused;
    public IInteractable CurrentTarget => current;
    public void Scan(Transform origin, float range)
    {
        var hits = Physics.OverlapSphere(origin.position, range, layers, QueryTriggerInteraction.Ignore);
        MockInteractable best = null; float bestSq = float.MaxValue;
        foreach (var hit in hits)
        {
            var candidate = hit.GetComponentInParent<MockInteractable>(); if (candidate == null) continue;
            float sq = (candidate.InteractionTransform.position - origin.position).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = candidate; }
        }
        if (best == focused) { current = best; return; }
        if (focused != null) focused.SetFocused(false);
        focused = best; current = best;
        if (focused != null) focused.SetFocused(true);
    }
}
