using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Raycast tabanlı sensör. Bizz'in önüne yelpaze şeklinde ışınlar atar,
    /// en yakın engelin mesafesini ve açısını hesaplar.</summary>
    public sealed class CompanionSensor : MonoBehaviour, ICompanionSensor
    {
        [Header("Işın ayarları")]
        [SerializeField] private int rayCount = 7;
        [SerializeField] private float fieldOfView = 90f;
        [SerializeField] private float maxDistance = 4f;
        [SerializeField] private float originHeight = 0.5f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("Hata ayıklama")]
        [SerializeField] private bool drawGizmos = true;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        public bool IsObstacleAhead { get; private set; }
        public float ObstacleDistance { get; private set; } = float.PositiveInfinity;
        public float ObstacleAngle { get; private set; }

        public void Scan()
        {
            IsObstacleAhead = false;
            ObstacleDistance = float.PositiveInfinity;
            ObstacleAngle = 0f;

            Vector3 origin = transform.position + Vector3.up * originHeight;
            int count = Mathf.Max(1, rayCount);

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : (float)i / (count - 1);
                float angle = Mathf.Lerp(-fieldOfView * 0.5f, fieldOfView * 0.5f, t);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;

                float distance = NearestHitDistance(origin, direction);
                if (distance < ObstacleDistance)
                {
                    ObstacleDistance = distance;
                    ObstacleAngle = angle;
                    IsObstacleAhead = true;
                }
            }
        }

        public float TargetAngle(Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f) return 0f;
            return Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
        }

        // Kendi collider'ımızı yok sayarak en yakın isabetin mesafesini bulur.
        private float NearestHitDistance(Vector3 origin, Vector3 direction)
        {
            int hitCount = Physics.RaycastNonAlloc(
                origin, direction, _hits, maxDistance, obstacleMask, QueryTriggerInteraction.Ignore);

            float nearest = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                if (_hits[i].collider.transform.IsChildOf(transform)) continue;
                if (_hits[i].distance < nearest) nearest = _hits[i].distance;
            }
            return nearest;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            Vector3 origin = transform.position + Vector3.up * originHeight;
            int count = Mathf.Max(1, rayCount);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : (float)i / (count - 1);
                float angle = Mathf.Lerp(-fieldOfView * 0.5f, fieldOfView * 0.5f, t);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(origin, direction * maxDistance);
            }
        }
    }
}