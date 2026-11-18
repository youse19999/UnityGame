using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, 0f);
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float scrollSpeed = 2f;
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 20f;

    private void LateUpdate()
    {
        if (target == null) return;

        float scroll = Input.mouseScrollDelta.y;
        offset.y = Mathf.Clamp(offset.y - scroll * scrollSpeed, minHeight, maxHeight);

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}