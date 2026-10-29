using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in durum adı değişince uygun cümleyi söyletir. Durum makinesine dokunmaz,
    /// yalnızca CompanionController'ın CurrentStateName değerini izler.</summary>
    [RequireComponent(typeof(CompanionController))]
    public sealed class CompanionDialogue : MonoBehaviour
    {
        [Tooltip("Bizz'in konuşma balonu.")]
        [SerializeField] private SpeechBubble bizzBubble;
        [Tooltip("Bumbu'nun konuşma balonu.")]
        [SerializeField] private SpeechBubble bumbuBubble;

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