using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Açılabilen kapı. IDoor sayesinde kapıyı açan taraf (mandal, LLM)
    /// kapının nasıl çalıştığını bilmez.</summary>
    public sealed class Door : MonoBehaviour, IDoor
    {
        [Header("Açılma hareketi")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);
        [SerializeField] private float openSpeed = 2f;

        [Header("Engel (kapalıyken NavMesh'i keser)")]
        [SerializeField] private UnityEngine.AI.NavMeshObstacle navObstacle;

        private Vector3 _closedPosition;
        private Vector3 _openPosition;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            _closedPosition = transform.position;
            _openPosition = _closedPosition + openOffset;
            if (navObstacle != null) navObstacle.enabled = true;
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (navObstacle != null) navObstacle.enabled = false;
        }

        private void Update()
        {
            Vector3 target = IsOpen ? _openPosition : _closedPosition;
            transform.position = Vector3.MoveTowards(transform.position, target, openSpeed * Time.deltaTime);
        }
    }
}