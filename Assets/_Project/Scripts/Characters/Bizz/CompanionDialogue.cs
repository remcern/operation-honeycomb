using System.Collections;
using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz ve Bumbu'nun konuşmaları. Durum makinesine dokunmaz.
    /// Rüzgâr sahnesi aşamalıdır ve konuşmalar yalnızca ilk geçişte oynar.
    /// Her geçişten sonra Bizz, Bumbu yanına gelene (ya da geri dönene) kadar bekler.</summary>
    [RequireComponent(typeof(CompanionController))]
    public sealed class CompanionDialogue : MonoBehaviour
    {
        private enum WindStage { Waiting, Warned, Sheltered }

        [SerializeField] private SpeechBubble bizzBubble;
        [SerializeField] private SpeechBubble bumbuBubble;

        [Header("Rüzgâr (isteğe bağlı)")]
        [SerializeField] private WindArea windArea;
        [Tooltip("Bumbu Bizz'e bu kadar yaklaşınca bekleme biter.")]
        [SerializeField] private float releaseDistance = 3.5f;

        [Header("Zamanlama (saniye)")]
        [Tooltip("Konuşmalar arasındaki boşluk.")]
        [SerializeField] private float gap = 1.5f;
        [SerializeField] private float lineDuration = 2.6f;

        private CompanionController _controller;
        private ISpeaker _bizz;
        private ISpeaker _bumbu;
        private string _lastState = "";
        private string _previousState = "";
        private WindStage _stage = WindStage.Waiting;
        private bool _windSceneDone;
        private bool _holding;

        private void Awake()
        {
            _controller = GetComponent<CompanionController>();
            _bizz = bizzBubble;
            _bumbu = bumbuBubble;
            if (_bizz == null)
                Debug.LogError("CompanionDialogue: bizzBubble atanmamis.", this);
        }

        private void OnEnable()
        {
            if (windArea != null) windArea.Crossed += OnWindCrossed;
        }

        private void OnDisable()
        {
            if (windArea != null) windArea.Crossed -= OnWindCrossed;
        }

        private void Update()
        {
            if (windArea != null)
            {
                bool useExit = windArea.TargetInside && !windArea.HasCrossed && windArea.ExitPoint != null;
                _controller.SetOverrideTarget(useExit ? windArea.ExitPoint : null);
                if (!_windSceneDone) UpdateWindScene();
                UpdateHold();
            }

            string state = _controller.CurrentStateName;
            if (state == _lastState) return;

            _previousState = _lastState;
            _lastState = state;
            OnStateChanged(state);
        }

        // ---- Bekleme: Bumbu gelene ya da geri dönene kadar Bizz yerinde durur ----

        private void UpdateHold()
        {
            if (!_holding || bumbuBubble == null) return;

            Vector3 a = bumbuBubble.transform.position;
            Vector3 b = transform.position;
            a.y = 0f;
            b.y = 0f;
            bool bumbuArrived = Vector3.Distance(a, b) <= releaseDistance;

            float areaBackZ = windArea.transform.position.z - windArea.transform.localScale.z * 0.5f;
            bool bumbuWentBack = bumbuBubble.transform.position.z < areaBackZ;

            if (bumbuArrived || bumbuWentBack)
            {
                _holding = false;
                _controller.SetHold(false);
            }
        }

        // ---- Rüzgâr sahnesi ----

        private void UpdateWindScene()
        {
            if (!windArea.TargetInside)
            {
                _stage = WindStage.Waiting;
                return;
            }

            if (_stage == WindStage.Waiting && !windArea.IsSheltered)
            {
                _stage = WindStage.Warned;
                StartCoroutine(WarnRoutine());
            }
            else if (_stage == WindStage.Warned && windArea.IsSheltered)
            {
                _stage = WindStage.Sheltered;
                StartCoroutine(ShelterRoutine());
            }
        }

        // 1) Bizz yardım ister, 2) 1,5 sn sonra Bumbu yola çıkar.
        private IEnumerator WarnRoutine()
        {
            if (_bizz != null) _bizz.Say("Çok rüzgârlı, geçemiyorum! Bumbu, yardım et!", lineDuration);
            yield return new WaitForSeconds(lineDuration + gap);

            if (_windSceneDone || _stage == WindStage.Waiting) yield break;
            if (_bumbu != null) _bumbu.Say("Geliyorum Bizz! Önüne geçip siper oluyorum!", lineDuration);
        }

        // 3) Bumbu siper olur, 4) 1,5 sn sonra Bizz rüzgârın kesildiğini söyler.
        private IEnumerator ShelterRoutine()
        {
            if (_bumbu != null) _bumbu.Say("İşte siper oldum Bizz! Şimdi geç!", lineDuration);
            yield return new WaitForSeconds(lineDuration + gap);

            if (_windSceneDone || _bizz == null) yield break;
            _bizz.Say("Rüzgâr kesildi, ilerliyorum! Sağ ol Bumbu!", lineDuration);
        }

        private void OnWindCrossed()
        {
            // Her geçişte Bizz yerinde bekler; konuşma yalnızca ilk geçişte.
            _holding = true;
            _controller.SetHold(true);

            if (_windSceneDone) return;
            _windSceneDone = true;
            StartCoroutine(ThanksRoutine());
        }

        // 5) Bizz geçince teşekkür eder, 6) 1,5 sn sonra Bumbu cevap verir.
        private IEnumerator ThanksRoutine()
        {
            yield return new WaitForSeconds(gap);
            if (_bizz != null) _bizz.Say("Başardık! Sen harikasın Bumbu!", lineDuration);
            yield return new WaitForSeconds(lineDuration + gap);
            if (_bumbu != null) _bumbu.Say("Rica ederim Bizz! Birlikte her engeli aşarız!", lineDuration);
        }

        // ---- Durum makinesi konuşmaları ----

        private void OnStateChanged(string state)
        {
            if (_bizz == null) return;

            switch (state)
            {
                case "GoToTarget":
                    if (_previousState == "AvoidObstacle") _bizz.Say("Yoluma devam!");
                    else _bizz.Say("Mandala gidiyorum!");
                    break;
                case "AvoidObstacle":
                    _bizz.Say("Pardon, yoldan çekil!");
                    break;
                case "PullLever":
                    _bizz.Say("Çekiyorum, bal kokusu geliyor!");
                    break;
                case "Done":
                    if (_controller.OrderCompleted) StartCoroutine(LeverDoneRoutine());
                    else _bizz.Say("Olmadı...");
                    break;
            }
        }

        private IEnumerator LeverDoneRoutine()
        {
            _bizz.Say("Tamam, açtım!", lineDuration);
            yield return new WaitForSeconds(lineDuration + gap);
            if (_bumbu != null) _bumbu.Say("Aferin Bizz!", lineDuration);
        }
    }
}