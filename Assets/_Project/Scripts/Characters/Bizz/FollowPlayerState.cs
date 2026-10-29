using System;
using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Emir yokken Bizz, Bumbu'yu NavMesh üzerinde takip eder.
    /// OverrideTarget varsa oraya gider, HoldPosition açıksa bekler.</summary>
    public sealed class FollowPlayerState : ICompanionState
    {
        private readonly CompanionContext _context;
        private readonly Func<ICompanionState> _onOrder;
        private readonly float _startDistance;
        private readonly float _stopDistance;
        private readonly float _repathInterval;

        private bool _moving;
        private float _timer;

        public string Name { get { return "Follow"; } }

        public FollowPlayerState(
            CompanionContext context,
            Func<ICompanionState> onOrder,
            float startDistance = 4f,
            float stopDistance = 2.5f,
            float repathInterval = 0.3f)
        {
            _context = context;
            _onOrder = onOrder;
            _startDistance = startDistance;
            _stopDistance = stopDistance;
            _repathInterval = repathInterval;
        }

        public void Enter()
        {
            _moving = false;
            _timer = 0f;
            _context.Agent.Stop();
        }

        public ICompanionState Tick()
        {
            if (_context.HasOrder) return _onOrder();

            if (_context.HoldPosition && _context.OverrideTarget == null)
            {
                _moving = false;
                _context.Agent.Stop();
                return null;
            }

            Transform target = _context.OverrideTarget != null ? _context.OverrideTarget : _context.FollowTarget;
            if (target == null) return null;

            Vector3 delta = target.position - _context.Self.position;
            delta.y = 0f;
            float distance = delta.magnitude;

            bool forced = _context.OverrideTarget != null;
            if (forced && distance > 0.5f) _moving = true;

            if (_moving && !forced && distance <= _stopDistance)
            {
                _moving = false;
                _context.Agent.Stop();
                return null;
            }

            if (!_moving && distance > _startDistance)
                _moving = true;

            if (_moving)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _timer = _repathInterval;
                    _context.Agent.MoveTo(target.position);
                }
            }

            return null;
        }

        public void Exit()
        {
            _context.Agent.Stop();
        }
    }
}