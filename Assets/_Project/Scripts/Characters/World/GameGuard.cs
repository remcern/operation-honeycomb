using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace OperationHoneycomb.Characters
{
    /// <summary>Oyunun çökmesini önleyen koruyucu. Açılışta eksik atamaları tarar, çalışırken
    /// Bizz ve Bumbu'yu güvenli bölgede tutar, yakalanmamış hataları ekranda bildirir.
    /// B/N tuşlarıyla koruma mekanizması kendi kendine sınanabilir (rapor için).</summary>
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
        [SerializeField] private float checkInterval = 0.3f;
        [SerializeField] private float fallLimitY = -5f;
        [SerializeField] private float navSnapDistance = 3f;
        [SerializeField] private float messageSeconds = 8f;

        [Header("Kendi kendini sınama (B: Bizz, N: Bumbu)")]
        [SerializeField] private bool enableSelfTest = true;

        private readonly List<string> _problems = new List<string>();
        private string _runtimeMessage = "";
        private float _runtimeMessageUntil;
        private float _nextCheck;
        private Vector3 _lastSafeBumbu;
        private NavMeshAgent _bizzNav;
        private CharacterController _bumbuBody;

        private void Awake()
        {
            Time.timeScale = 1f;
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
            if (!NavMesh.SamplePosition(Vector3.forward * 5f, out hit, 10f, NavMesh.AllAreas))
                _problems.Add("NavMesh yok (Ground icin Bake gerekli)");

            foreach (string p in _problems) Debug.LogWarning("GameGuard: " + p);
        }

        // ---- 2) Çalışma anı koruması ----

        private void Update()
        {
            if (enableSelfTest) HandleSelfTestKeys();

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

            Vector3 pos = bizz.transform.position;
            Vector3 ground = new Vector3(pos.x, 0f, pos.z);

            NavMeshHit near;
            bool nearMesh = NavMesh.SamplePosition(ground, out near, 0.6f, NavMesh.AllAreas);
            if (_bizzNav.isOnNavMesh && nearMesh) return;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(ground, out hit, navSnapDistance * 6f, NavMesh.AllAreas))
            {
                _bizzNav.Warp(hit.position);
                ResetBizzOrder();
                Report("Bizz yolunu kaybetti, geri alindi.");
            }
        }

        // Geri alınan Bizz'in takılı emri iptal edilir; E tuşu yeniden çalışır.
        private void ResetBizzOrder()
        {
            try
            {
                bizz.Cancel();
                bizz.SetOverrideTarget(null);
                bizz.SetHold(false);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("GameGuard: emir sifirlanamadi: " + ex.Message);
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

        // ---- Kendi kendini sınama ----

        private void HandleSelfTestKeys()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.bKey.wasPressedThisFrame) ThrowBizzOffMesh();
            if (kb.nKey.wasPressedThisFrame) ThrowBumbuIntoPit();
        }

        private void ThrowBizzOffMesh()
        {
            if (bizz == null || _bizzNav == null) return;
            _bizzNav.enabled = false;
            bizz.transform.position = new Vector3(0f, 1f, -15f);
            _bizzNav.enabled = true;
            Debug.Log("GameGuard SINAMA: Bizz zeminin disina atildi.");
        }

        private void ThrowBumbuIntoPit()
        {
            if (bumbu == null) return;
            if (_bumbuBody != null) _bumbuBody.enabled = false;
            bumbu.position = new Vector3(bumbu.position.x, -20f, bumbu.position.z);
            if (_bumbuBody != null) _bumbuBody.enabled = true;
            Debug.Log("GameGuard SINAMA: Bumbu cukura atildi.");
        }

        // ---- 3) Genel yakalayıcı ----

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception) return;
            Report("Bir sorun olustu, oyun devam ediyor.");
        }

        private void Report(string message)
        {
            _runtimeMessage = message;
            _runtimeMessageUntil = Time.unscaledTime + messageSeconds;
            Debug.Log("GameGuard: " + message);
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
                var big = new GUIStyle(GUI.skin.label);
                big.fontSize = 24;
                big.fontStyle = FontStyle.Bold;
                big.alignment = TextAnchor.MiddleCenter;
                big.normal.textColor = new Color(1f, 0.8f, 0.2f);
                GUI.Label(new Rect(0, Screen.height - 170f, Screen.width, 40f), _runtimeMessage, big);
            }
        }
    }
}