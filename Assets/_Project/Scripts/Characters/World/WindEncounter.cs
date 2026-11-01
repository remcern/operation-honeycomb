using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace OperationHoneycomb.Characters
{
    /// <summary>Rüzgâr sahnesi. Bizz sınır çizgisini, Bumbu siper noktasında durmadıkça geçemez
    /// (fiziksel kapı). Konuşmalar yalnızca gerçek duruma göre söylenir, durum değişirse iptal olur.</summary>
    public sealed class WindEncounter : MonoBehaviour
    {
        private enum Phase { Idle, Struggling, Blocked, Passing, Thanks }

        [Header("Karakterler")]
        [SerializeField] private CompanionController bizz;
        [SerializeField] private Transform bumbu;
        [SerializeField] private SpeechBubble bizzBubble;
        [SerializeField] private SpeechBubble bumbuBubble;

        [Header("Noktalar")]
        [SerializeField] private Transform shieldSpot;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private Transform returnExitPoint;
        [SerializeField] private float frontEdgeZ = 19f;
        [SerializeField] private float backEdgeZ = 25f;
        [SerializeField] private float blockOffset = 2.5f;

        [Header("Rüzgâr hissi")]
        [Range(0.05f, 1f)]
        [SerializeField] private float windSpeedFactor = 0.15f;

        [Header("Mesafeler")]
        [SerializeField] private float spotRadius = 2f;
        [SerializeField] private float crossDistance = 1.5f;
        [SerializeField] private float rejoinDistance = 3.5f;
        [SerializeField] private float rearmMargin = 3f;

        [Header("Zamanlama (saniye)")]
        [SerializeField] private float gap = 0.4f;
        [SerializeField] private float lineDuration = 2.2f;

        [Header("Hata ayıklama")]
        [SerializeField] private bool showDebug = true;

        private Phase _phase = Phase.Idle;
        private bool _forward = true;
        private bool _thanksDone;
        private bool _introDone;
        private bool _hintVisible;
        private bool _needsRearm;
        private int _token;
        private float _limitZ;      // Bizz'in geçebileceği en uzak Z (ileri) ya da en yakın Z (geri)
        private bool _limitActive;
        private CompanionAgent _agent;
        private NavMeshAgent _nav;

        private Transform CurrentExit { get { return _forward ? exitPoint : returnExitPoint; } }

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
            if (bizz == null || bumbu == null || shieldSpot == null
                || exitPoint == null || returnExitPoint == null) return;

            float z = bizz.transform.position.z;

            if (_needsRearm && (z < frontEdgeZ - rearmMargin || z > backEdgeZ + rearmMargin))
            {
                _needsRearm = false;
                _phase = Phase.Idle;
            }

            switch (_phase)
            {
                case Phase.Idle:
                    if (_needsRearm) break;
                    if (z >= frontEdgeZ && z <= backEdgeZ)
                    {
                        _forward = z < (frontEdgeZ + backEdgeZ) * 0.5f;
                        BeginStruggle();
                    }
                    break;

                case Phase.Struggling:
                    if (ReachedBlock(z)) StartCoroutine(BlockedRoutine(++_token));
                    break;

                case Phase.Passing:
                    if (!BumbuAtSpot()) StartCoroutine(WindReturnsRoutine(++_token));
                    else if (Flat(bizz.transform.position, CurrentExit.position) <= crossDistance)
                        StartCoroutine(FinishRoutine(++_token));
                    break;
            }
        }

        // Fiziksel kapı: Bizz sınırı geçerse geri alınır.
        private void LateUpdate()
        {
            if (!_limitActive || bizz == null || _nav == null) return;
            if (_phase != Phase.Struggling && _phase != Phase.Blocked) return;

            Vector3 p = bizz.transform.position;
            bool over = _forward ? p.z > _limitZ : p.z < _limitZ;
            if (over)
            {
                p.z = _limitZ;
                _nav.Warp(p);
            }
        }

        private bool ReachedBlock(float z)
        {
            return _forward ? z >= frontEdgeZ + blockOffset : z <= backEdgeZ - blockOffset;
        }

        private void SetLimit()
        {
            _limitZ = _forward ? frontEdgeZ + blockOffset : backEdgeZ - blockOffset;
            _limitActive = true;
        }

        private void BeginStruggle()
        {
            _phase = Phase.Struggling;
            _token++;
            SetLimit();
            if (_agent != null) _agent.SetWindFactor(windSpeedFactor);
            bizz.SetOverrideTarget(CurrentExit);
            if (!_introDone) Say(bizzBubble, "Vızzz! Rüzgâr beni geri itiyor!");
        }

        private IEnumerator BlockedRoutine(int token)
        {
            _phase = Phase.Blocked;
            SetLimit();
            bizz.SetOverrideTarget(null);
            bizz.SetHold(true);
            _hintVisible = true;

            Say(bizzBubble, _introDone
                ? "Rüzgâr çok sert! Bumbu, yine siper ol!"
                : "Çok sert, ilerleyemiyorum! Bumbu, yardım et!");
            yield return new WaitForSeconds(lineDuration + gap);
            if (token != _token) yield break;

            if (!BumbuAtSpot())
            {
                Say(bumbuBubble, "Geliyorum Bizz! Önüne geçip rüzgârı kesiyorum!");
                yield return new WaitForSeconds(lineDuration + gap);
                if (token != _token) yield break;
            }

            // Bumbu daireye gelmeden hiçbir şey olmaz
            while (!BumbuAtSpot())
            {
                if (token != _token) yield break;
                yield return null;
            }

            _hintVisible = false;
            _phase = Phase.Passing;
            _introDone = true;
            _limitActive = false;
            if (_agent != null) _agent.SetWindFactor(1f);

            Say(bumbuBubble, "İşte buradayım Bizz! Rüzgârı ben tutuyorum, geç!");
            yield return new WaitForSeconds(lineDuration * 0.6f);
            if (token != _token) yield break;
            if (!BumbuAtSpot()) yield break;   // bu sırada ayrıldıysa Update yeni akışı başlatır

            bizz.SetHold(false);
            bizz.SetOverrideTarget(CurrentExit);
            Say(bizzBubble, "Vay, rüzgâr kesildi! Geçiyorum!");
        }

        private IEnumerator WindReturnsRoutine(int token)
        {
            _phase = Phase.Blocked;
            // Bizz şu an bulunduğu yerde durur; daha ileri geçemez
            _limitZ = bizz.transform.position.z;
            _limitActive = true;
            if (_agent != null) _agent.SetWindFactor(windSpeedFactor);
            bizz.SetOverrideTarget(null);
            bizz.SetHold(true);
            _hintVisible = true;
            Say(bizzBubble, "Rüzgâr geri geldi! Bumbu, daireye dön!");

            while (!BumbuAtSpot())
            {
                if (token != _token) yield break;
                yield return null;
            }

            _hintVisible = false;
            _phase = Phase.Passing;
            _limitActive = false;
            if (_agent != null) _agent.SetWindFactor(1f);
            Say(bumbuBubble, "Buradayım Bizz, geç!");
            yield return new WaitForSeconds(lineDuration * 0.6f);
            if (token != _token) yield break;
            if (!BumbuAtSpot()) yield break;

            bizz.SetHold(false);
            bizz.SetOverrideTarget(CurrentExit);
        }

        private IEnumerator FinishRoutine(int token)
        {
            _phase = Phase.Thanks;
            _hintVisible = false;
            _limitActive = false;
            if (_agent != null) _agent.SetWindFactor(1f);
            bizz.SetOverrideTarget(null);
            bizz.SetHold(true);

            if (_forward && !_thanksDone)
            {
                _thanksDone = true;
                yield return new WaitForSeconds(gap);
                Say(bizzBubble, "Başardık! Sen harika bir dostsun Bumbu!");
                yield return new WaitForSeconds(lineDuration + gap);
                Say(bumbuBubble, "Rica ederim Bizz! Şimdi bala gidelim!");
                yield return new WaitForSeconds(lineDuration + gap);
            }

            float timeout = 20f;
            while (timeout > 0f && Flat(bumbu.position, bizz.transform.position) > rejoinDistance)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            bizz.SetHold(false);
            _needsRearm = true;
            _phase = Phase.Idle;
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

        private void OnGUI()
        {
            if (showDebug)
                GUI.Label(new Rect(10, 130, 600, 25),
                    "Ruzgar: " + _phase + " | Bumbu dairede: " + (bumbu != null && shieldSpot != null && BumbuAtSpot()));

            if (!_hintVisible) return;

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