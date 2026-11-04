using UnityEngine;
using UnityEngine.AI;

namespace OperationHoneycomb.Characters
{
    /// <summary>NavMeshAgent'ı saran hareket sınıfı. Yol bulmayı Unity NavMesh yapar;
    /// bu sınıf yalnızca "şuraya git", "dur" ve "vardın mı?" sorularını yanıtlar.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CompanionAgent : MonoBehaviour, IWindAffected
    {
        [Header("Hareket")]
        [SerializeField] private float stoppingDistance = 0.4f;
        [SerializeField] private float sampleRadius = 2f;

        [Header("Uçuş görünümü (yol bulma yine NavMesh ile)")]
        [SerializeField] private float flyingHeight = 1.2f;

        private NavMeshAgent _agent;
        private float _baseSpeed;
        private float _windFactor = 1f;

        public bool HasPath { get { return _agent.hasPath; } }
        public bool IsPathPending { get { return _agent.pathPending; } }
        public bool PathFailed { get { return _agent.pathStatus == NavMeshPathStatus.PathInvalid; } }
        public float RemainingDistance { get { return _agent.remainingDistance; } }
        public float WindFactor { get { return _windFactor; } }

        public bool HasArrived
        {
            get
            {
                if (_agent.pathPending) return false;
                if (_agent.remainingDistance > _agent.stoppingDistance) return false;
                return !_agent.hasPath || _agent.velocity.sqrMagnitude < 0.01f;
            }
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.stoppingDistance = stoppingDistance;
            _agent.baseOffset = flyingHeight;
            _baseSpeed = _agent.speed;
        }

        public void SetWindFactor(float speedFactor)
        {
            _windFactor = Mathf.Clamp(speedFactor, 0.05f, 1f);
            _agent.speed = _baseSpeed * _windFactor;
        }

        /// <summary>Hedefe en yakın NavMesh noktasını bulup oraya yürümeye başlar.
        /// NavMesh üzerinde bir nokta bulunamazsa false döner.</summary>
        public bool MoveTo(Vector3 target)
        {
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(target, out hit, sampleRadius, NavMesh.AllAreas))
                return false;

            _agent.isStopped = false;
            return _agent.SetDestination(hit.position);
        }

        public void Stop()
        {
            if (!_agent.isOnNavMesh) return;
            _agent.isStopped = true;
            _agent.ResetPath();
        }
    }
}