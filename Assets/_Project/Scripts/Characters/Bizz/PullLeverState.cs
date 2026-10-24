using System;
using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz hedefteki nesneyi (mandal vb.) kullanırken içinde bulunduğu durum.</summary>
    public sealed class PullLeverState : ICompanionState
    {
        private readonly CompanionContext _context;
        private readonly Func<ICompanionState> _onDone;
        private readonly float _pullDuration;
        private float _elapsed;
        private bool _interacted;

        public string Name { get { return "PullLever"; } }

        public PullLeverState(CompanionContext context, Func<ICompanionState> onDone, float pullDuration = 1.0f)
        {
            _context = context;
            _onDone = onDone;
            _pullDuration = pullDuration;
        }

        public void Enter()
        {
            _elapsed = 0f;
            _interacted = false;
            _context.Agent.Stop();
        }

        public ICompanionState Tick()
        {
            _elapsed += Time.deltaTime;

            if (!_interacted && _elapsed >= _pullDuration)
            {
                _interacted = true;
                if (_context.TargetInteractable != null && _context.TargetInteractable.CanInteract)
                    _context.TargetInteractable.Interact();

                _context.OrderCompleted = true;
                return _onDone();
            }

            return null;
        }

        public void Exit() { }
    }
}