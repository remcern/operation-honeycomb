using UnityEngine;
using UnityEngine.EventSystems;

namespace OperationHoneycomb.Characters
{
    /// <summary>Oyuncu bir yazı kutusuna yazıyorsa oyun tuşları (WASD, E, Q, B, N) yok sayılır.
    /// TMP_InputField ve UnityEngine.UI.InputField için çalışır (tip adıyla bulur).</summary>
    public static class UiTypingGuard
    {
        public static bool IsTyping
        {
            get
            {
                EventSystem es = EventSystem.current;
                if (es == null) return false;

                GameObject selected = es.currentSelectedGameObject;
                if (selected == null) return false;

                Component[] components = selected.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] != null && components[i].GetType().Name.Contains("InputField"))
                        return true;
                }
                return false;
            }
        }
    }
}