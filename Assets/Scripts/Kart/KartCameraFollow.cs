using UnityEngine;

public class KartCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 6f, -8.5f);
    [SerializeField] private float followSmooth = 9f;
    [SerializeField] private float rotateSmooth = 10f;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        var desiredPos = target.TransformPoint(followOffset);
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSmooth * Time.deltaTime);

        var lookTarget = target.position + (target.forward * 4f) + Vector3.up * 1.3f;
        var desiredRot = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotateSmooth * Time.deltaTime);
    }
}
