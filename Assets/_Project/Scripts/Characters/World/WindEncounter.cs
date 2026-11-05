using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace OperationHoneycomb.Characters
{
    /// <summary>Rüzgâr sahnesi (tek yönlü, tek seferlik). Bizz rüzgârda yavaşlar ve sınır çizgisinde durur;
    /// Bumbu sarı siper dairesine girince rüzgâr kesilir ve Bizz geçer. Çıkışta teşekkür edilir ve fan durur.
    /// Konuşma ve ipucu, anlık duruma göre karar verilir; süre beklemeye bağlı değildir.</summary>
    public sealed class WindEncounter : MonoBehaviour
    {
        private enum Phase { Idle, Struggling, Blocked, Passing, Thanks, Done }

        [Header("Karakterler")]
        [SerializeField] private CompanionController bizz;
        [SerializeField] private Transform bumbu;
        [SerializeField] private SpeechBubble bizzBubble;
        [SerializeField] private SpeechBubble bumbuBubble;

        [Header("Noktalar")]
        [SerializeField] private Transform shieldSpot;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private float frontEdgeZ = 19f;
        [SerializeField] private float blockOffset = 2.5f;

        [Header("Fan (engel aşılınca durur)")]
        [SerializeField] private Spinner fanSpinner;
        [SerializeField] private float fanStopSeconds = 3f;

        [Header("Rüzgâr hissi")]
        [Range(0.05f, 1f)]
        [SerializeField] private float windSpeedFactor = 0.15f;

        [Header("Mesafeler")]
        [SerializeField] private float spotRadius = 2f;
        [SerializeField] private float crossDistance = 1.5f;
        [SerializeField] private float rejoinDistance = 3.5f;
        [Tooltip("İpucu yalnızca Bumbu siper noktasına bu kadar yakınsa gösterilir.")]
        [SerializeField] private float hintRange = 12f;

        [Header("Zamanlama (saniye)")]
        [SerializeField] private float gap = 0.4f;
        [SerializeField] private float lineDuration = 2.2f;
        [Tooltip("Bumbu daireden bu kadar süre uzak kalırsa rüzgâr geri gelir.")]
        [SerializeField] private float graceSeconds = 1f;

        [Header("Hata ayıklama")]
        [SerializeField] private bool showDebug = false;

        private Phase _phase = Phase.Idle;
        private int _token;
        private float _limitZ;
        private bool _limitActive;
        private float _awayTimer;
        private bool _passedBefore;
        private CompanionAgent _agent;
        private NavMeshAgent _nav;

        private void Awake()
        {
            if (bizz != null)
            {
                _agent = bizz.GetComponent<CompanionAgent>();
                _nav = bizz.GetComponent<NavMeshAgent>();
            }
        }

        private void Update()
        {
            if (_phase == Phase.Done) return;
            if (bizz == null || bumbu == null || shieldSpot == null || exitPoint == null) return;

            float z = bizz.transform.position.z;
            bool bumbuAtSpot = BumbuAtSpot();

            switch (_phase)
            {
                case Phase.Idle:
                    if (z >= frontEdgeZ && z < exitPoint.position.z)
                    {
                        if (bumbuAtSpot) StartPassing(false);   // Bumbu zaten siperde: hiç durma
                        else BeginStruggle();
                    }
                    break;

                case Phase.Struggling:
                    if (bumbuAtSpot) StartPassing(false);
                    else if (z >= frontEdgeZ + blockOffset) EnterBlocked(false);
                    break;

                case Phase.Blocked:
                    if (bumbuAtSpot) StartPassing(true);
                    break;

                case Phase.Passing:
                    if (Flat(bizz.transform.position, exitPoint.position) <= crossDistance)
                    {
                        StartFinish();
                        break;
                    }
                    if (bumbuAtSpot)
                    {
                        _awayTimer = 0f;
                    }
                    else
                    {
                        _awayTimer += Time.deltaTime;
                        if (_awayTimer >= graceSeconds) EnterBlocked(true);   // rüzgâr geri geldi
                    }
                    break;
            }
        }

        // Fiziksel sınır: Bizz, rüzgârda sınır çizgisini geçemez.
        private void LateUpdate()
        {
            if (!_limitActive || bizz == null || _nav == null) return;
            if (_phase != Phase.Struggling && _phase != Phase.Blocked) return;

            Vector3 p = bizz.transform.position;
            if (p.z > _limitZ)
            {
                p.z = _limitZ;
                _nav.Warp(p);
            }
        }

        private void BeginStruggle()
        {
            _phase = Phase.Struggling;
            _token++;
            _limitZ = frontEdgeZ + blockOffset;
            _limitActive = true;
            if (_agent != null) _agent.SetWindFactor(windSpeedFactor);
            bizz.SetOverrideTarget(exitPoint);
            Say(bizzBubble, "Vızzz! Rüzgâr beni geri itiyor!");
        }

        private void EnterBlocked(bool windReturned)
        {
            _phase = Phase.Blocked;
            _token++;
            _limitZ = windReturned ? bizz.transform.position.z : frontEdgeZ + blockOffset;
            _limitActive = true;
            if (_agent != null) _agent.SetWindFactor(windSpeedFactor);
            bizz.SetOverrideTarget(null);
            bizz.SetHold(true);
            StartCoroutine(BlockedSpeech(_token, windReturned));
        }

        // Yalnızca konuşur; akışı beklemez. Durum değişirse (token) sessizce biter.
        private IEnumerator BlockedSpeech(int token, bool windReturned)
        {
            Say(bizzBubble, windReturned
                ? "Rüzgâr geri geldi! Bumbu, daireye dön!"
                : "Çok sert, ilerleyemiyorum! Bumbu, yardım et!");
            yield return new WaitForSeconds(lineDuration + gap);
            if (token != _token || BumbuAtSpot()) yield break;

            Say(bumbuBubble, windReturned
                ? "Dur Bizz, hemen daireye dönüyorum!"
                : "Geliyorum Bizz! Önüne geçip rüzgârı kesiyorum!");
        }

        private void StartPassing(bool wasBlocked)
        {
            _phase = Phase.Passing;
            _token++;
            _awayTimer = 0f;
            _limitActive = false;
            if (_agent != null) _agent.SetWindFactor(1f);
            bizz.SetHold(false);
            bizz.SetOverrideTarget(exitPoint);

            if (_passedBefore)
            {
                Say(bumbuBubble, "Buradayım Bizz, geç!");
            }
            else if (wasBlocked)
            {
                Say(bumbuBubble, "İşte buradayım Bizz! Rüzgârı ben tutuyorum, geç!");
                StartCoroutine(SayLater(bizzBubble, "Vay, rüzgâr kesildi! Geçiyorum!", 1.3f, _token));
            }
            else
            {
                Say(bizzBubble, "Bumbu siper olmuş, rüzgâr kesildi! Geçiyorum!");
            }
            _passedBefore = true;
        }

        private IEnumerator SayLater(SpeechBubble bubble, string text, float delay, int token)
        {
            yield return new WaitForSeconds(delay);
            if (token != _token) yield break;
            Say(bubble, text);
        }

        private void StartFinish()
        {
            _phase = Phase.Thanks;
            _token++;
            _limitActive = false;
            if (_agent != null) _agent.SetWindFactor(1f);
            bizz.SetOverrideTarget(null);
            bizz.SetHold(true);
            StartCoroutine(FinishRoutine());
        }

        private IEnumerator FinishRoutine()
        {
            yield return new WaitForSeconds(gap);
            Say(bizzBubble, "Başardık! Sen harika bir dostsun Bumbu!");
            yield return new WaitForSeconds(lineDuration + gap);

            Say(bumbuBubble, "Rica ederim Bizz! Bak, fan yavaşlıyor!");
            if (fanSpinner != null) fanSpinner.StopGradually(fanStopSeconds);
            yield return new WaitForSeconds(lineDuration + gap);

            Say(bumbuBubble, "Fan durdu, artık rüzgâr yok! Şimdi bala gidelim!");
            yield return new WaitForSeconds(lineDuration + gap);

            // Bumbu yanına gelince takip yeniden başlar (en fazla 20 sn)
            float timeout = 20f;
            while (timeout > 0f && Flat(bumbu.position, bizz.transform.position) > rejoinDistance)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            bizz.SetHold(false);
            _phase = Phase.Done;   // kalıcı: bir daha tetiklenmez, ipucu çıkmaz
        }

        private bool BumbuAtSpot()
        {
            return Flat(bumbu.position, shieldSpot.position) <= spotRadius;
        }

        private static void Say(SpeechBubble bubble, string text)
        {
            if (bubble != null) bubble.Say(text, 2.2f);
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // İpucu: yalnızca Bizz engelde, Bumbu daire dışında ve siper noktasına yakınken görünür.
        private bool HintVisible()
        {
            if (_phase != Phase.Blocked) return false;
            if (bumbu == null || shieldSpot == null) return false;
            float d = Flat(bumbu.position, shieldSpot.position);
            return d > spotRadius && d <= hintRange;
        }

        private void OnGUI()
        {
            if (showDebug)
                GUI.Label(new Rect(10, 130, 600, 25), "Ruzgar: " + _phase);

            if (!HintVisible()) return;

            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 22;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = new Color(1f, 0.9f, 0.3f);
            GUI.Label(new Rect(0, Screen.height - 70f, Screen.width, 40f),
                "İpucu: Bumbu ile sarı daireye git, Bizz'e siper ol!", style);
        }
    }
}