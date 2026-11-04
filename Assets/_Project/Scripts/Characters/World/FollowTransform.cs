using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Hedef nesnenin hareketini izler (ölçeğini almaz).
    /// Kapı dekorunu kapıyla birlikte kaldırır.</summary>
    public sealed class FollowTransform : MonoBehaviour
    {
        [SerializeField] private Transform target;

        private Vector3 _offset;
        private bool _ready;

        private void Start()
        {
            if (target == null)
            {
                Debug.LogError("FollowTransform: target atanmamis.", this);
                return;
            }
            // Başlangıçtaki konum farkını hatırla; kapı hareket edince aynı farkla gelir
            _offset = transform.position - target.position;
            _ready = true;
        }

        private void LateUpdate()
        {
            if (_ready) transform.position = target.position + _offset;
        }
    }
}