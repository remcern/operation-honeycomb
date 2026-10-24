using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in indirebildiği mandal. Bağlı kapıyı IDoor üzerinden açar.</summary>
    public sealed class Lever : MonoBehaviour, IInteractable
    {
        [Header("Bağlantı")]
        [Tooltip("IDoor uygulayan bir nesne (ör. Door) sürükle.")]
        [SerializeField] private MonoBehaviour doorObject;

        [Header("Etkileşim noktası")]
        [Tooltip("Bizz'in durması gereken nokta. Boşsa mandalın kendi konumu.")]
        [SerializeField] private Transform interactionPoint;

        [Header("Görünüm")]
        [SerializeField] private Transform handle;
        [SerializeField] private Vector3 pulledRotation = new Vector3(60f, 0f, 0f);

        private IDoor _door;

        public bool CanInteract { get; private set; } = true;

        public Vector3 InteractionPoint
        {
            get { return interactionPoint != null ? interactionPoint.position : transform.position; }
        }

        private void Awake()
        {
            _door = doorObject as IDoor;
            if (_door == null)
                Debug.LogError("Lever: doorObject IDoor uygulamiyor ya da atanmamis.", this);
        }

        public void Interact()
        {
            if (!CanInteract) return;
            CanInteract = false;

            if (handle != null)
                handle.localEulerAngles = pulledRotation;

            if (_door != null) _door.Open();
        }
    }
}