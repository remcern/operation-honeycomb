using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in durumuna göre balonları söyletir. Rüzgâr sahnesi: Bumbu uyarır ve siper olur,
    /// Bizz ilerler, alan geçilince teşekkür edilir.</summary>
    [RequireComponent(typeof(CompanionController))]
    public sealed class CompanionDialogue : MonoBehaviour
    {
        [Tooltip("Bizz'in konuşma balonu.")]
        [SerializeField] private SpeechBubble bizzBubble;
        [Tooltip("Bumbu'nun konuşma balonu.")]
        [SerializeField] private SpeechBubble bumbuBubble;

        [Header("Rüzgâr (isteğe bağlı)")]
        [SerializeField] private WindArea windArea;

        private CompanionController _controller;
        private ISpeaker _bizz;
        private ISpeaker _bumbu;
        private string _lastState = "";
        private string _previousState = "";
        private bool _bumbuWarned;

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
            }

            TalkAboutWind();

            string state = _controller.CurrentStateName;
            if (state == _lastState) return;

            _previousState = _lastState;
            _lastState = state;
            OnStateChanged(state);
        }

        // Bizz rüzgârda zorlanırken Bumbu seslenir (bir kez).
        private void TalkAboutWind()
        {
            if (windArea == null || _bumbu == null) return;

            bool struggling = windArea.TargetInside && !windArea.IsSheltered;
            if (!struggling)
            {
                if (!windArea.TargetInside) _bumbuWarned = false;
                return;
            }

            if (_bumbuWarned) return;
            _bumbuWarned = true;
            _bumbu.Say("Rüzgâr çok sert! Dur Bizz, önüne geçiyorum!", 3.5f);
        }

        // Bizz alanı geçince bir kez teşekkür eder.
        private void OnWindCrossed()
        {
            if (_bizz != null) _bizz.Say("Sağ ol Bumbu!", 3f);
            if (_bumbu != null) _bumbu.Say("Rica ederim, Bizz!", 3f);
        }

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
                    _bizz.Say("Çekiyorum!");
                    break;
                case "Done":
                    if (_controller.OrderCompleted)
                    {
                        _bizz.Say("Tamam, açtım!");
                        if (_bumbu != null) _bumbu.Say("Aferin Bizz!", 2.5f);
                    }
                    else _bizz.Say("Olmadı...");
                    break;
            }
        }
    }
}