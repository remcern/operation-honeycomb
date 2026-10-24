using System;
using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz hedefe NavMesh ile yürürken içinde bulunduğu durum.</summary>
    public sealed class GoToTargetState : ICompanionState
    {
        private readonly CompanionContext _context;
        private readonly Func<ICompanionState> _onInteract;
        private readonly Func<ICompanionState> _onArrived;
        private readonly Func<ICompanionState> _onFailed;
        private readonly Func<ICompanionState> _onObstacle;

        private readonly float _obstacleDistance;
        private readonly float _timeout;
        private float _elapsed;

        public string Name { get { return "GoToTarget"; } }

        public GoToTargetState(
            CompanionContext context,
            Func<ICompanionState> onInteract,
            Func<ICompanionState> onArrived,
            Func<ICompanionState> onFailed,
            Func<ICompanionState> onObstacle,
            float obstacleDistance = 0.8f,
            float timeout = 25f)
        {
            _context = context;
            _onInteract = onInteract;
            _onArrived = onArrived;
            _onFailed = onFailed;
            _onObstacle = onObstacle;
            _obstacleDistance = obstacleDistance;
            _timeout = timeout;
        }

        public void Enter()
        {
            _elapsed = 0f;
            _context.OrderFailed = false;
            _context.OrderCompleted = false;

            if (!_context.Agent.MoveTo(_context.TargetPosition))
                _context.OrderFailed = true;
        }

        public ICompanionState Tick()
        {
            if (_context.OrderFailed) return _onFailed();

            _elapsed += Time.deltaTime;
            if (_elapsed > _timeout)
            {
                _context.OrderFailed = true;
                return _onFailed();
            }

            if (_context.Agent.PathFailed)
            {
                _context.OrderFailed = true;
                return _onFailed();
            }

            // Sensör: yakında engel varsa kaçınma durumuna geç
            _context.Sensor.Scan();
            if (_context.Sensor.IsObstacleAhead && _context.Sensor.ObstacleDistance < _obstacleDistance)
                return _onObstacle();

            if (_context.Agent.HasArrived)
            {
                if (_context.TargetInteractable != null && _context.TargetInteractable.CanInteract)
                    return _onInteract();

                _context.OrderCompleted = true;
                return _onArrived();
            }

            return null;
        }

        public void Exit() { }
    }
}