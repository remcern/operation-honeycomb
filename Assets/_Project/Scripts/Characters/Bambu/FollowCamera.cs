using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Hedefi (Bumbu) arkadan ve yukarıdan takip eden basit kamera.</summary>
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -8f);
        [SerializeField] private float smoothing = 5f;
        [SerializeField] private float lookHeight = 1f;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, smoothing * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }
    }
}