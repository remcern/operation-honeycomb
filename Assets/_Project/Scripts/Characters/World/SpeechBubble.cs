using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Karakterin üstünde kısa süreli konuşma balonu gösterir (OnGUI, dünya konumundan).</summary>
    public sealed class SpeechBubble : MonoBehaviour, ISpeaker
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private float maxWidth = 220f;
        [SerializeField] private int fontSize = 16;

        private string _text = "";
        private float _hideAt;
        private GUIStyle _style;
        private Camera _camera;

        public void Say(string text, float duration = 2.5f)
        {
            _text = text;
            _hideAt = Time.unscaledTime + duration;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_text) || Time.unscaledTime > _hideAt) return;

            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Vector3 screen = _camera.WorldToScreenPoint(transform.position + offset);
            if (screen.z < 0f) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box);
                _style.wordWrap = true;
                _style.alignment = TextAnchor.MiddleCenter;
                _style.normal.textColor = Color.white;
            }
            _style.fontSize = fontSize;

            GUIContent content = new GUIContent(_text);
            float height = _style.CalcHeight(content, maxWidth);
            Rect rect = new Rect(screen.x - maxWidth * 0.5f, Screen.height - screen.y - height, maxWidth, height);
            GUI.Box(rect, content, _style);
        }
    }
}