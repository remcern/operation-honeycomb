using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace OperationHoneycomb.Characters
{
    /// <summary>Oyunun çökmesini önleyen koruyucu. Açılışta eksik atamaları tarar, çalışırken
    /// Bizz ve Bumbu'yu güvenli bölgede tutar, yakalanmamış hataları ekranda bildirir.</summary>
    public sealed class GameGuard : MonoBehaviour
    {
        [Header("Kontrol edilecek nesneler")]
        [SerializeField] private CompanionController bizz;
        [SerializeField] private Transform bumbu;
        [SerializeField] private MonoBehaviour lever;     // IInteractable
        [SerializeField] private MonoBehaviour fenceGate; // IDoor
        [SerializeField] private MonoBehaviour owlDoor;   // IDoor
        [SerializeField] private Camera mainCamera;

        [Header("Güvenlik")]
        [SerializeField] private float checkInterval = 1f;
        [SerializeField] private float fallLimitY = -5f;
        [SerializeField] private float navSnapDistance = 3f;

        private readonly List<string> _problems = new List<string>();
        private string _runtimeMessage = "";
        private float _runtimeMessageUntil;
        private float _nextCheck;
        private Vector3 _lastSafeBumbu;
        private NavMeshAgent _bizzNav;
        private CharacterController _bumbuBody;
        private int _errorCount;

        private void Awake()
        {
            Time.timeScale = 1f;   // test sonrası takılı kalmasın
            ValidateScene();
            Application.logMessageReceived += OnLog;

            if (bizz != null) _bizzNav = bizz.GetComponent<NavMeshAgent>();
            if (bumbu != null)
            {
                _bumbuBody = bumbu.GetComponent<CharacterController>();
                _lastSafeBumbu = bumbu.position;
            }
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }

        // ---- 1) Açılış kontrolü ----

        private void ValidateScene()
        {
            _problems.Clear();
            if (bizz == null) _problems.Add("Bizz (CompanionController) bulunamadi");
            if (bumbu == null) _problems.Add("Bumbu bulunamadi");
            if (!(lever is IInteractable)) _problems.Add("Mandal (IInteractable) atanmamis");
            if (!(fenceGate is IDoor)) _problems.Add("Cit kapisi (IDoor) atanmamis");
            if (!(owlDoor is IDoor)) _problems.Add("Baykus kapisi (IDoor) atanmamis");
            if (mainCamera == null && Camera.main == null) _problems.Add("Kamera bulunamadi");

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(Vector3.zero + Vector3.forward * 5f, out hit, 10f, NavMesh.AllAreas))
                _problems.Add("NavMesh yok (Ground icin Bake gerekli)");

            foreach (string p in _problems) Debug.LogWarning("GameGuard: " + p);
        }

        // ---- 2) Çalışma anı koruması ----

        private void Update()
        {
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + checkInterval;

            try
            {
                KeepBizzOnNavMesh();
                KeepBumbuSafe();
            }
            catch (System.Exception ex)
            {
                Report("Koruyucu hata yakaladi: " + ex.Message);
            }
        }

        private void KeepBizzOnNavMesh()
        {
            if (bizz == null || _bizzNav == null || !_bizzNav.enabled) return;
            if (_bizzNav.isOnNavMesh) return;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(bizz.transform.position, out hit, navSnapDistance * 3f, NavMesh.AllAreas))
            {
                _bizzNav.Warp(hit.position);
                Report("Bizz yolunu kaybetti, geri alindi.");
            }
        }

        private void KeepBumbuSafe()
        {
            if (bumbu == null) return;

            if (bumbu.position.y < fallLimitY || float.IsNaN(bumbu.position.y))
            {
                if (_bumbuBody != null) _bumbuBody.enabled = false;
                bumbu.position = _lastSafeBumbu + Vector3.up * 0.5f;
                if (_bumbuBody != null) _bumbuBody.enabled = true;
                Report("Bumbu dustu, guvenli noktaya alindi.");
            }
            else if (_bumbuBody != null && _bumbuBody.isGrounded)
            {
                _lastSafeBumbu = bumbu.position;
            }
        }

        // ---- 3) Genel yakalayıcı ----

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception) return;
            _errorCount++;
            Report("Bir sorun olustu, oyun devam ediyor.");
        }

        private void Report(string message)
        {
            _runtimeMessage = message;
            _runtimeMessageUntil = Time.unscaledTime + 4f;
        }

        // ---- Ekran ----

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 18;
            style.fontStyle = FontStyle.Bold;

            float y = Screen.height - 30f;

            if (_problems.Count > 0)
            {
                style.normal.textColor = new Color(1f, 0.4f, 0.3f);
                for (int i = _problems.Count - 1; i >= 0; i--)
                {
                    y -= 24f;
                    GUI.Label(new Rect(10, y, 900, 24), "UYARI: " + _problems[i], style);
                }
            }

            if (!string.IsNullOrEmpty(_runtimeMessage) && Time.unscaledTime < _runtimeMessageUntil)
            {
                style.normal.textColor = new Color(1f, 0.8f, 0.2f);
                GUI.Label(new Rect(10, 10 + 160f, 900, 28), _runtimeMessage, style);
            }
        }
    }
}
