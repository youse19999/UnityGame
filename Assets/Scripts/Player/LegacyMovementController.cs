using System;
using UnityEngine;
using SimsMock.Interaction;

public sealed class LegacyMovementController : MonoBehaviour, IMovementController
{
    [SerializeField] float speed = 4f;
    bool movingToPoint;
    Vector3 destination;
    Action<bool> arrival;
    public bool IsMoving => movingToPoint;
    void Update()
    {
        if (movingToPoint)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            if ((transform.position - destination).sqrMagnitude < .0025f) { movingToPoint = false; var cb = arrival; arrival = null; cb?.Invoke(true); }
            return;
        }
        float x = Input.GetAxisRaw("Horizontal"), z = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(x, 0, z).normalized;
        transform.position += direction * speed * Time.deltaTime;
    }
    public void MoveTo(Vector3 point, Action<bool> onArrived) { destination = point; arrival = onArrived; movingToPoint = true; }
    public void Stop() { movingToPoint = false; arrival = null; }
}
