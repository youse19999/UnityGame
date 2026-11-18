using System;
using UnityEngine;
using SimsMock.Interaction;

public sealed class ClickMovementController : MonoBehaviour, IMovementController
{
    [SerializeField] float speed = 4f;
    bool movingToPoint;
    Vector3 destination;
    Action<bool> arrival;
    public bool IsMoving => movingToPoint;
    void Update()
    {
        if (!movingToPoint) return;
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        if ((transform.position - destination).sqrMagnitude < .0025f)
        { movingToPoint = false; var callback = arrival; arrival = null; callback?.Invoke(true); }
    }
    public void MoveTo(Vector3 point, Action<bool> onArrived) { destination = point; arrival = onArrived; movingToPoint = true; }
    public void Stop() { movingToPoint = false; arrival = null; }
}
