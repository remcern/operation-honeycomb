using UnityEngine;
using UnityEngine.InputSystem;

namespace OperationHoneycomb.Characters
{
    /// <summary>Oyuncunun Bizz'e emir verdiği giriş sınıfı. E: mandala git, Q: iptal.
    /// Yanlış tuş, spam, klavye yokluğu ve eksik atama oyunu bozmaz.</summary>
    public sealed class CompanionCommandInput : MonoBehaviour
    {
        [SerializeField] private CompanionController companion;
        [Tooltip("IInteractable uygulayan nesne (Lever).")]
        [SerializeField] private MonoBehaviour leverObject;
        [Tooltip("Emirler arası en kısa süre (sn). Spam basışı engeller.")]
        [SerializeField] private float cooldown = 0.6f;

        private IInteractable _lever;
        private float _nextAllowed;
        private string _notice = "";
        private float _noticeUntil;

        private void Awake()
        {
            _lever = leverObject as IInteractable;
            if (companion == null || _lever == null)
                Debug.LogWarning("CompanionCommandInput: companion ya da lever atanmamis, emirler devre disi.");
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;                      // klavye yok: sessizce çık
            if (UiTypingGuard.IsTyping) return;          // yazı kutusuna yazılırken E/Q yok sayılır
            if (companion == null || _lever == null) return;

            if (kb.eKey.wasPressedThisFrame) TryOrder();
            if (kb.qKey.wasPressedThisFrame) TryCancel();
            // Diğer tüm tuşlar bilerek yok sayılır
        }

        private void TryOrder()
        {
            if (Time.unscaledTime < _nextAllowed) return;     // çok hızlı basış
            _nextAllowed = Time.unscaledTime + cooldown;

            if (!_lever.CanInteract)
            {
                Notice("Mandal zaten indirildi.");
                return;
            }
            if (companion.HasOrder || !companion.IsAvailable)
            {
                Notice("Bizz su an mesgul.");
                return;
            }
            if (!companion.CanAccept(_lever.InteractionPoint))
            {
                Notice("Bizz mandala ulasamiyor.");
                return;
            }

            companion.GoToAndInteract(_lever);
        }

        private void TryCancel()
        {
            if (!companion.HasOrder)
            {
                Notice("Iptal edilecek emir yok.");
                return;
            }
            companion.Cancel();
            Notice("Emir iptal edildi.");
        }

        private void Notice(string text)
        {
            _notice = text;
            _noticeUntil = Time.unscaledTime + 6f;
        }

        private void OnGUI()
        {
            if (companion == null) return;

            // Sol üstte yalnızca tek ipucu kalır (Q yine çalışır, ekranda yazmaz)
            GUI.Label(new Rect(10, 10, 500, 25), "E: Bizz'i mandala gönder");

            if (!string.IsNullOrEmpty(_notice) && Time.unscaledTime < _noticeUntil)
            {
                var style = new GUIStyle(GUI.skin.label);
                style.fontSize = 22;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = new Color(1f, 0.85f, 0.3f);
                GUI.Label(new Rect(0, Screen.height - 120f, Screen.width, 40f), _notice, style);
            }
        }
    }
}