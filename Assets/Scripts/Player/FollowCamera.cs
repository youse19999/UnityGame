using UnityEngine;
public sealed class FollowCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0, 10, -8);
    public void Configure(Transform followTarget) { target = followTarget; }
    void LateUpdate() { if (target == null) return; transform.position = target.position + offset; transform.rotation = Quaternion.LookRotation(target.position + Vector3.up - transform.position, Vector3.up); }
}
