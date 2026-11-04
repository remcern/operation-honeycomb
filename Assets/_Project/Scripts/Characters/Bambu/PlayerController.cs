using UnityEngine;
using UnityEngine.InputSystem;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bumbu'nun WASD ile hareketi. Hareket kameranın baktığı yöne göredir.
    /// CharacterController sayesinde çit ve duvarların içinden geçemez.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Hareket")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float turnSpeed = 10f;
        [SerializeField] private float gravity = -20f;

        [Header("Kamera (yoksa Main Camera kullanılır)")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController _controller;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            Vector2 input = ReadInput();
            Vector3 move = CameraRelative(input) * moveSpeed;

            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;
            move.y = _verticalVelocity;

            _controller.Move(move * Time.deltaTime);

            Vector3 flat = new Vector3(move.x, 0f, move.z);
            if (flat.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(flat);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
            }
        }

        private static Vector2 ReadInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            Vector2 v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            return forward * input.y + right * input.x;
        }
    }
}