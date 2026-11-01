using System.Collections;
using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in durum makinesine bağlı konuşmaları (mandal sahnesi).
    /// Rüzgâr sahnesi WindEncounter'da.</summary>
    [RequireComponent(typeof(CompanionController))]
    public sealed class CompanionDialogue : MonoBehaviour
    {
        [SerializeField] private SpeechBubble bizzBubble;
        [SerializeField] private SpeechBubble bumbuBubble;

        [Header("Zamanlama (saniye)")]
        [SerializeField] private float gap = 0.4f;
        [SerializeField] private float lineDuration = 2.2f;

        private CompanionController _controller;
        private ISpeaker _bizz;
        private ISpeaker _bumbu;
        private string _lastState = "";
        private string _previousState = "";

        private void Awake()
        {
            _controller = GetComponent<CompanionController>();
            _bizz = bizzBubble;
            _bumbu = bumbuBubble;
            if (_bizz == null)
                Debug.LogError("CompanionDialogue: bizzBubble atanmamis.", this);
        }

        private void Update()
        {
            string state = _controller.CurrentStateName;
            if (state == _lastState) return;

            _previousState = _lastState;
            _lastState = state;
            OnStateChanged(state);
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