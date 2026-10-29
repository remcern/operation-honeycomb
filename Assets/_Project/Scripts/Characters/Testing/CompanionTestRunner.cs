using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in davranışını tekrarlanabilir biçimde test eder.
    /// T tuşu: rastgele başlangıç noktalarından N deneme yapar, başarı oranını
    /// ve ortalama süreyi ekrana, Console'a ve CSV dosyasına yazar.</summary>
    public sealed class CompanionTestRunner : MonoBehaviour
    {
        [Header("Bağlantılar")]
        [SerializeField] private CompanionController companion;
        [Tooltip("IInteractable ve IResettable uygulayan nesne (Lever).")]
        [SerializeField] private MonoBehaviour leverObject;
        [Tooltip("IDoor ve IResettable uygulayan nesne (FenceGate).")]
        [SerializeField] private MonoBehaviour doorObject;

        [Header("Test ayarları")]
        [SerializeField] private int trialCount = 30;
        [Tooltip("Aynı seed ile aynı başlangıç noktaları üretilir (tekrarlanabilirlik).")]
        [SerializeField] private int seed = 12345;
        [SerializeField] private float trialTimeout = 40f;
        [SerializeField] private float timeScale = 2f;
        [SerializeField] private Vector2 areaMin = new Vector2(-8f, 0f);
        [SerializeField] private Vector2 areaMax = new Vector2(8f, 10f);
        [SerializeField] private float minDistanceFromLever = 3f;

        private sealed class TrialResult
        {
            public int Index;
            public Vector3 Start;
            public bool Success;
            public float Duration;
            public string Reason;
        }

        private readonly List<TrialResult> _results = new List<TrialResult>();
        private IInteractable _lever;
        private IResettable _leverReset;
        private IDoor _door;
        private IResettable _doorReset;
        private NavMeshAgent _agent;

        private bool _running;
        private int _currentTrial;
        private float _previousTimeScale = 1f;
        private string _summary = "T tuşu: testi başlat";

        private void Awake()
        {
            _lever = leverObject as IInteractable;
            _leverReset = leverObject as IResettable;
            _door = doorObject as IDoor;
            _doorReset = doorObject as IResettable;
            if (companion != null) _agent = companion.GetComponent<NavMeshAgent>();

            if (companion == null || _agent == null || _lever == null || _leverReset == null
                || _door == null || _doorReset == null)
                Debug.LogError("CompanionTestRunner: baglantilar eksik ya da yanlis.", this);
        }

        private void OnDestroy()
        {
            if (_running) Time.timeScale = _previousTimeScale;
        }

        private void Update()
        {
            if (_running || Keyboard.current == null) return;
            if (Keyboard.current.tKey.wasPressedThisFrame) StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            if (companion == null || _agent == null || _lever == null || _leverReset == null
                || _door == null || _doorReset == null)
                yield break;

            _running = true;
            _results.Clear();
            _previousTimeScale = Time.timeScale;
            Time.timeScale = timeScale;
            var rng = new System.Random(seed);

            for (int i = 1; i <= trialCount; i++)
            {
                _currentTrial = i;
                yield return StartCoroutine(RunTrial(i, rng));
            }

            Time.timeScale = _previousTimeScale;
            _summary = BuildSummary();
            Debug.Log(_summary);
            SaveCsv();
            _running = false;
        }

        private IEnumerator RunTrial(int index, System.Random rng)
        {
            // Önceki denemeden kalan emir bitsin, Bizz Idle'a dönsün
            float waited = 0f;
            while (companion.CurrentStateName != "Idle" && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            _leverReset.ResetState();
            _doorReset.ResetState();

            Vector3 start = PickStart(rng);
            _agent.Warp(start);
            yield return null;
            yield return null;

            float t0 = Time.time;
            companion.GoToAndInteract(_lever);

            if (!companion.HasOrder)
            {
                _results.Add(new TrialResult { Index = index, Start = start, Success = false, Duration = 0f, Reason = "Emir kabul edilmedi" });
                yield break;
            }

            string reason = "";
            bool success = false;
            while (true)
            {
                if (_door.IsOpen) { success = true; break; }

                if (!companion.HasOrder)
                {
                    reason = companion.OrderFailed ? "Hedefe ulasilamadi" : "Emir bitti ama kapi acilmadi";
                    break;
                }

                if (Time.time - t0 > trialTimeout)
                {
                    reason = "Zaman asimi";
                    companion.Cancel();
                    break;
                }

                yield return null;
            }

            _results.Add(new TrialResult
            {
                Index = index,
                Start = start,
                Success = success,
                Duration = Time.time - t0,
                Reason = success ? "" : reason
            });
        }

        // Mandaldan yeterince uzak, NavMesh üzerinde rastgele bir başlangıç noktası seçer.
        private Vector3 PickStart(System.Random rng)
        {
            Vector3 leverPos = leverObject.transform.position;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                float x = Mathf.Lerp(areaMin.x, areaMax.x, (float)rng.NextDouble());
                float z = Mathf.Lerp(areaMin.y, areaMax.y, (float)rng.NextDouble());

                float dx = x - leverPos.x;
                float dz = z - leverPos.z;
                if (Mathf.Sqrt(dx * dx + dz * dz) < minDistanceFromLever) continue;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(new Vector3(x, 0f, z), out hit, 2f, NavMesh.AllAreas))
                    return hit.position;
            }
            return companion.transform.position;
        }

        private string BuildSummary()
        {
            int total = _results.Count;
            int ok = 0;
            float sum = 0f, min = float.MaxValue, max = 0f;

            foreach (TrialResult r in _results)
            {
                if (!r.Success) continue;
                ok++;
                sum += r.Duration;
                if (r.Duration < min) min = r.Duration;
                if (r.Duration > max) max = r.Duration;
            }

            if (ok == 0)
                return "TEST SONUCU: " + total + " denemede 0 basari (%0).";

            float rate = 100f * ok / total;
            return "TEST SONUCU: " + ok + "/" + total + " basari (%" + rate.ToString("0.0", CultureInfo.InvariantCulture)
                + "), ortalama sure " + (sum / ok).ToString("0.00", CultureInfo.InvariantCulture)
                + " sn, en kisa " + min.ToString("0.00", CultureInfo.InvariantCulture)
                + " sn, en uzun " + max.ToString("0.00", CultureInfo.InvariantCulture) + " sn.";
        }

        private void SaveCsv()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("trial,start_x,start_z,success,duration_s,reason");
                foreach (TrialResult r in _results)
                {
                    sb.Append(r.Index).Append(',')
                      .Append(r.Start.x.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                      .Append(r.Start.z.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                      .Append(r.Success ? "1" : "0").Append(',')
                      .Append(r.Duration.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                      .AppendLine(r.Reason);
                }

                string path = Path.Combine(Application.persistentDataPath, "companion_test_results.csv");
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
                Debug.Log("Test sonuclari kaydedildi: " + path);
                _summary += "\nCSV: " + path;
            }
            catch (IOException ex)
            {
                Debug.LogWarning("CSV yazilamadi: " + ex.Message);
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 60, 900, 25), _running
                ? "TEST CALISIYOR: deneme " + _currentTrial + "/" + trialCount
                : "T: Bizz testini baslat (" + trialCount + " deneme)");

            if (!_running)
                GUI.Label(new Rect(10, 82, 1200, 60), _summary);
        }
    }
}