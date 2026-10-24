using UnityEngine;
using UnityEngine.InputSystem;

namespace OperationHoneycomb.Characters
{
    /// <summary>Oyuncunun Bizz'e emir verdiği giriş sınıfı.
    /// E tuşu: Bizz mandala gider ve onu indirir. Q tuşu: emri iptal eder.</summary>
    public sealed class CompanionCommandInput : MonoBehaviour
    {
        [SerializeField] private CompanionController companion;
        [Tooltip("IInteractable uygulayan nesne (Lever).")]
        [SerializeField] private MonoBehaviour leverObject;

        private IInteractable _lever;

        private void Awake()
        {
            _lever = leverObject as IInteractable;
            if (companion == null || _lever == null)
                Debug.LogError("CompanionCommandInput: companion ya da lever atanmamis.", this);
        }

        private void Update()
        {
            if (Keyboard.current == null || _lever == null || companion == null) return;

            if (Keyboard.current.eKey.wasPressedThisFrame)
                companion.GoToAndInteract(_lever);

            if (Keyboard.current.qKey.wasPressedThisFrame)
                companion.Cancel();
        }

        private void OnGUI()
        {
            if (companion == null) return;
            GUI.Label(new Rect(10, 10, 500, 25), "E: Bizz mandala git   Q: iptal");
            GUI.Label(new Rect(10, 32, 500, 25), "Bizz durumu: " + companion.CurrentStateName);
        }
    }
}