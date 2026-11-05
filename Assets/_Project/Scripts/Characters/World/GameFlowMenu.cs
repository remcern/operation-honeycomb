using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OperationHoneycomb.Characters
{
    /// <summary>Açılışta "Oyuna başla" ekranını, oyun bitince "Macera tamamlandı" ekranını gösterir.
    /// Bal temalı arayüz (altıgen petek deseni, altın renkler) kodla çizilir.
    /// Başka sınıfları değiştirmez; final bitince ShowEndScreen() çağrılır
    /// (FinaleController.onFinaleEnded olayına bağlanır).</summary>
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
        [SerializeField] private string controlsHint = "Bumbu: W A S D   |   Bizz'e emir: E   |   İptal: Q";
        [SerializeField] private string endTitle = "MACERA TAMAMLANDI";
        [SerializeField] private string endLine1 = "Bal bulundu, dostluk kazandı.";
        [SerializeField] private string endLine2 = "Bumbu mutlu bir kış uykusuna yattı...";

        [Header("Bal teması renkleri")]
        [SerializeField] private Color backgroundTop = new Color(0.12f, 0.06f, 0.02f, 1f);
        [SerializeField] private Color backgroundBottom = new Color(0.42f, 0.22f, 0.04f, 1f);
        [SerializeField] private Color honeyGold = new Color(0.98f, 0.74f, 0.12f, 1f);
        [SerializeField] private Color honeyAmber = new Color(0.86f, 0.50f, 0.04f, 1f);
        [SerializeField] private Color cream = new Color(1f, 0.93f, 0.68f, 1f);
        [SerializeField] private Color deepBrown = new Color(0.20f, 0.10f, 0.02f, 1f);
        [Range(0f, 0.6f)]
        [SerializeField] private float honeycombStrength = 0.28f;

        // Sahne yeniden yüklenince hatırlanır: "Tekrar oyna" başlangıç ekranını atlar.
        private static bool s_skipStartMenu;

        private Mode _mode = Mode.Playing;
        private Texture2D _gradient, _hex, _panel, _border, _btnNormal, _btnHover;
        private GUIStyle _titleStyle, _textStyle, _hintStyle, _buttonStyle;
        private bool _stylesReady;

        private void Awake()
        {
            BuildTextures();
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
            DestroyTexture(_gradient);
            DestroyTexture(_hex);
            DestroyTexture(_panel);
            DestroyTexture(_border);
            DestroyTexture(_btnNormal);
            DestroyTexture(_btnHover);
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

            float s = Mathf.Clamp(Screen.height / 900f, 0.6f, 2f);
            float w = Screen.width;
            float h = Screen.height;

            // Arka plan: bal gradyanı + petek deseni
            GUI.DrawTexture(new Rect(0, 0, w, h), _gradient, ScaleMode.StretchToFill);
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, w, h), _hex,
                new Rect(0, 0, w / (_hex.width * s), h / (_hex.height * s)));

            bool isStart = _mode == Mode.Start;
            float panelW = 720f * s;
            float panelH = (isStart ? 470f : 540f) * s;
            Rect panel = new Rect((w - panelW) * 0.5f, (h - panelH) * 0.5f, panelW, panelH);

            // Panel: altın çerçeve + koyu bal zemini
            float b = 4f * s;
            GUI.DrawTexture(new Rect(panel.x - b, panel.y - b, panel.width + 2f * b, panel.height + 2f * b), _border);
            GUI.DrawTexture(panel, _panel);

            float cx = panel.x + panel.width * 0.5f;
            float y = panel.y + 34f * s;

            if (isStart)
            {
                Title(gameTitle, panel, ref y, 44f, 84f, s);
                Divider(cx, ref y, panel.width * 0.55f, s);
                Text(startSubtitle, panel, ref y, 26f, 56f, s);
                y += 14f * s;
                if (HoneyButton("OYUNA BAŞLA", cx, ref y, s)) StartGame();
                if (HoneyButton("OYUNDAN ÇIK", cx, ref y, s)) QuitGame();
                y += 6f * s;
                Hint(controlsHint, panel, ref y, s);
                Hint("Enter: başla", panel, ref y, s);
            }
            else
            {
                Title(endTitle, panel, ref y, 50f, 90f, s);
                Divider(cx, ref y, panel.width * 0.55f, s);
                Text(endLine1, panel, ref y, 26f, 52f, s);
                Text(endLine2, panel, ref y, 26f, 56f, s);
                y += 10f * s;
                if (HoneyButton("TEKRAR OYNA", cx, ref y, s)) Restart(true);
                if (HoneyButton("ANA MENÜ", cx, ref y, s)) Restart(false);
                if (HoneyButton("OYUNDAN ÇIK", cx, ref y, s)) QuitGame();
            }
        }

        private void Title(string text, Rect panel, ref float y, float size, float height, float s)
        {
            _titleStyle.fontSize = Mathf.RoundToInt(size * s);
            Rect r = new Rect(panel.x, y, panel.width, height * s);

            // Gölge, sonra krem rengi başlık
            Color old = _titleStyle.normal.textColor;
            _titleStyle.normal.textColor = new Color(0.05f, 0.02f, 0f, 0.8f);
            GUI.Label(new Rect(r.x + 3f * s, r.y + 3f * s, r.width, r.height), text, _titleStyle);
            _titleStyle.normal.textColor = old;
            GUI.Label(r, text, _titleStyle);
            y += height * s;
        }

        private void Text(string text, Rect panel, ref float y, float size, float height, float s)
        {
            _textStyle.fontSize = Mathf.RoundToInt(size * s);
            GUI.Label(new Rect(panel.x, y, panel.width, height * s), text, _textStyle);
            y += height * s;
        }

        private void Hint(string text, Rect panel, ref float y, float s)
        {
            _hintStyle.fontSize = Mathf.RoundToInt(18f * s);
            GUI.Label(new Rect(panel.x, y, panel.width, 34f * s), text, _hintStyle);
            y += 32f * s;
        }

        private void Divider(float cx, ref float y, float width, float s)
        {
            GUI.DrawTexture(new Rect(cx - width * 0.5f, y, width, 3f * s), _border);
            y += 22f * s;
        }

        private bool HoneyButton(string text, float cx, ref float y, float s)
        {
            float bw = 360f * s;
            float bh = 58f * s;
            float edge = 3f * s;

            // Amber çerçeve, üstüne altın düğme
            GUI.DrawTexture(new Rect(cx - bw * 0.5f - edge, y - edge, bw + 2f * edge, bh + 2f * edge), _border);
            _buttonStyle.fontSize = Mathf.RoundToInt(24f * s);
            bool clicked = GUI.Button(new Rect(cx - bw * 0.5f, y, bw, bh), text, _buttonStyle);

            y += bh + 18f * s;
            return clicked;
        }

        // ---- Stil ve doku üretimi ----
        private void BuildStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.alignment = TextAnchor.MiddleCenter;
            _titleStyle.normal.textColor = cream;

            _textStyle = new GUIStyle(GUI.skin.label);
            _textStyle.fontStyle = FontStyle.Bold;
            _textStyle.alignment = TextAnchor.MiddleCenter;
            _textStyle.normal.textColor = new Color(1f, 0.85f, 0.45f, 1f);

            _hintStyle = new GUIStyle(GUI.skin.label);
            _hintStyle.alignment = TextAnchor.MiddleCenter;
            _hintStyle.normal.textColor = new Color(1f, 0.85f, 0.45f, 0.75f);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.alignment = TextAnchor.MiddleCenter;
            _buttonStyle.normal.background = _btnNormal;
            _buttonStyle.hover.background = _btnHover;
            _buttonStyle.active.background = _btnHover;
            _buttonStyle.focused.background = _btnNormal;
            _buttonStyle.normal.textColor = deepBrown;
            _buttonStyle.hover.textColor = deepBrown;
            _buttonStyle.active.textColor = deepBrown;
            _buttonStyle.focused.textColor = deepBrown;
        }

        private void BuildTextures()
        {
            // Dikey gradyan: üstte koyu kahve, altta amber
            _gradient = new Texture2D(1, 128, TextureFormat.RGBA32, false);
            _gradient.wrapMode = TextureWrapMode.Clamp;
            for (int i = 0; i < 128; i++)
            {
                float t = i / 127f;   // 0 = alt, 1 = üst (Unity dokusunda y yukarı artar)
                _gradient.SetPixel(0, i, Color.Lerp(backgroundBottom, backgroundTop, t));
            }
            _gradient.Apply();

            _hex = BuildHoneycomb();

            _panel = SolidTexture(new Color(0.16f, 0.08f, 0.02f, 0.94f));
            _border = SolidTexture(honeyGold);
            _btnNormal = SolidTexture(honeyGold);
            _btnHover = SolidTexture(Color.Lerp(honeyGold, cream, 0.55f));
        }

        // Tekrarlanabilir altıgen (petek) deseni: her piksel için en yakın iki merkezin
        // uzaklık farkı küçükse kenar çizgisi çizilir.
        private Texture2D BuildHoneycomb()
        {
            const int W = 96;
            const int H = 56;
            const float T = 2.4f;   // çizgi kalınlığı

            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color line = new Color(honeyAmber.r, honeyAmber.g, honeyAmber.b, honeycombStrength);
            Color clear = new Color(honeyAmber.r, honeyAmber.g, honeyAmber.b, 0f);

            for (int py = 0; py < H; py++)
            {
                for (int px = 0; px < W; px++)
                {
                    float best = float.MaxValue;
                    float second = float.MaxValue;

                    for (int i = -2; i <= 3; i++)
                    {
                        for (int j = -2; j <= 2; j++)
                        {
                            float cx = i * 48f;
                            float cy = (j + 0.5f * (i & 1)) * 56f;
                            float dx = px - cx;
                            float dy = py - cy;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);

                            if (d < best) { second = best; best = d; }
                            else if (d < second) { second = d; }
                        }
                    }

                    float edge = Mathf.Clamp01(1f - (second - best) / T);
                    tex.SetPixel(px, py, Color.Lerp(clear, line, edge));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D SolidTexture(Color color)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, color);
            t.Apply();
            return t;
        }

        private static void DestroyTexture(Texture2D t)
        {
            if (t != null) Destroy(t);
        }
    }
}