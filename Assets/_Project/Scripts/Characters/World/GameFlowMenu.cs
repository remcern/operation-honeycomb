using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OperationHoneycomb.Characters
{
    /// <summary>Açılışta "Oyuna başla" ekranını, oyun bitince "Macera tamamlandı" ekranını gösterir.
    /// Tekrar oyna, ana menü ve oyundan çık düğmeleri içerir. Başka sınıfları değiştirmez;
    /// final bitince ShowEndScreen() çağrılır (FinaleController.onFinaleEnded olayına bağlanır).</summary>
    public sealed class GameFlowMenu : MonoBehaviour
    {
        private enum Mode { Playing, Start, End }

        [Header("Ekranlar")]
        [SerializeField] private bool showStartMenu = true;
        [Tooltip("Final bitince bitiş ekranı bu kadar sonra gelir (final yazısı okunsun).")]
        [SerializeField] private float endScreenDelay = 2.5f;

        [Header("Metinler")]
        [SerializeField] private string gameTitle = "OPERATION: HONEYCOMB";
        [SerializeField] private string startSubtitle = "Bumbu ve Bizz'in bal macerası";
        [SerializeField] private string endTitle = "MACERA TAMAMLANDI";
        [SerializeField] private string endLine1 = "Bal bulundu, dostluk kazandı.";
        [SerializeField] private string endLine2 = "Bumbu mutlu bir kış uykusuna yattı...";

        // Sahne yeniden yüklenince hatırlanır: "Tekrar oyna" başlangıç ekranını atlar.
        private static bool s_skipStartMenu;

        private Mode _mode = Mode.Playing;
        private Texture2D _overlay, _button, _buttonHover;
        private GUIStyle _titleStyle, _textStyle, _buttonStyle;
        private bool _stylesReady;

        private void Awake()
        {
            _overlay = MakeTexture(new Color(0.18f, 0.05f, 0.14f, 0.96f));
            _button = MakeTexture(new Color(0.82f, 0.27f, 0.33f, 1f));
            _buttonHover = MakeTexture(new Color(0.93f, 0.36f, 0.42f, 1f));
        }

        private void Start()
        {
            if (!showStartMenu || s_skipStartMenu)
            {
                s_skipStartMenu = false;
                _mode = Mode.Playing;
                return;
            }
            _mode = Mode.Start;
            Time.timeScale = 0f;   // oyun, başla denene kadar bekler
        }

        private void Update()
        {
            if (_mode != Mode.Start) return;
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.enterKey.wasPressedThisFrame) StartGame();
        }

        private void OnDestroy()
        {
            if (_overlay != null) Destroy(_overlay);
            if (_button != null) Destroy(_button);
            if (_buttonHover != null) Destroy(_buttonHover);
        }

        // ---- Dışarıdan çağrılır (FinaleController.onFinaleEnded) ----
        public void ShowEndScreen()
        {
            if (_mode == Mode.End) return;
            StartCoroutine(ShowEndRoutine());
        }

        private IEnumerator ShowEndRoutine()
        {
            yield return new WaitForSecondsRealtime(endScreenDelay);
            _mode = Mode.End;
            Time.timeScale = 0f;
        }

        // ---- Düğme işlevleri ----
        private void StartGame()
        {
            _mode = Mode.Playing;
            Time.timeScale = 1f;
        }

        private void Restart(bool skipStartMenu)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex < 0)
            {
                Debug.LogError("GameFlowMenu: sahne Build Profiles listesinde degil, yeniden baslatilamaz.");
                return;
            }
            s_skipStartMenu = skipStartMenu;
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene.buildIndex);
        }

        private void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---- Çizim ----
        private void OnGUI()
        {
            if (_mode == Mode.Playing) return;

            GUI.depth = -1000;   // her şeyin üstünde
            BuildStyles();

            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlay);

            float s = Mathf.Clamp(Screen.height / 900f, 0.6f, 2f);
            float cx = Screen.width * 0.5f;
            float y = Screen.height * 0.22f;

            if (_mode == Mode.Start)
            {
                Label(gameTitle, _titleStyle, cx, ref y, 90f * s);
                Label(startSubtitle, _textStyle, cx, ref y, 60f * s);
                y += 40f * s;
                if (Button("OYUNA BAŞLA", cx, ref y, s)) StartGame();
                if (Button("OYUNDAN ÇIK", cx, ref y, s)) QuitGame();
                Label("Enter: başla", _textStyle, cx, ref y, 50f * s);
            }
            else
            {
                Label(endTitle, _titleStyle, cx, ref y, 90f * s);
                Label(endLine1, _textStyle, cx, ref y, 45f * s);
                Label(endLine2, _textStyle, cx, ref y, 60f * s);
                y += 30f * s;
                if (Button("TEKRAR OYNA", cx, ref y, s)) Restart(true);
                if (Button("ANA MENÜ", cx, ref y, s)) Restart(false);
                if (Button("OYUNDAN ÇIK", cx, ref y, s)) QuitGame();
            }
        }

        private void BuildStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.alignment = TextAnchor.MiddleCenter;
            _titleStyle.normal.textColor = Color.white;

            _textStyle = new GUIStyle(GUI.skin.label);
            _textStyle.fontStyle = FontStyle.Bold;
            _textStyle.alignment = TextAnchor.MiddleCenter;
            _textStyle.normal.textColor = Color.white;

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.alignment = TextAnchor.MiddleCenter;
            _buttonStyle.normal.background = _button;
            _buttonStyle.hover.background = _buttonHover;
            _buttonStyle.active.background = _buttonHover;
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor = Color.white;
            _buttonStyle.active.textColor = Color.white;
        }

        private void Label(string text, GUIStyle style, float cx, ref float y, float height)
        {
            float s = Mathf.Clamp(Screen.height / 900f, 0.6f, 2f);
            style.fontSize = Mathf.RoundToInt((style == _titleStyle ? 56f : 24f) * s);
            GUI.Label(new Rect(0, y, Screen.width, height), text, style);
            y += height;
        }

        private bool Button(string text, float cx, ref float y, float s)
        {
            _buttonStyle.fontSize = Mathf.RoundToInt(22f * s);
            float w = 320f * s;
            float h = 56f * s;
            bool clicked = GUI.Button(new Rect(cx - w * 0.5f, y, w, h), text, _buttonStyle);
            y += h + 14f * s;
            return clicked;
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, color);
            t.Apply();
            return t;
        }
    }
}