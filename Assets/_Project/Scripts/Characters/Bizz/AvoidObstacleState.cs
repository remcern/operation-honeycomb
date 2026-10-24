
using System;
using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz yakında engel görünce devreye giren kaçınma durumu.
    /// Engelin tersi yönünde NavMesh üzerinde bir nokta seçip oraya yönelir,
    /// kısa süre sonra hedefe dönüş için GoToTarget durumuna geçer.</summary>
    public sealed class AvoidObstacleState : ICompanionState
    {
        private readonly CompanionContext _context;
        private readonly Func<ICompanionState> _onResume;
        private readonly float _sideStep;
        private readonly float _duration;
        private float _elapsed;

        public string Name { get { return "AvoidObstacle"; } }

        public AvoidObstacleState(
            CompanionContext context,
            Func<ICompanionState> onResume,
            float sideStep = 1.5f,
            float duration = 1.0f)
        {
            _context = context;
            _onResume = onResume;
            _sideStep = sideStep;
            _duration = duration;
        }

        public void Enter()
        {
            _elapsed = 0f;

            // Engel sağdaysa (açı > 0) sola, soldaysa sağa kaç
            float side = _context.Sensor.ObstacleAngle > 0f ? -1f : 1f;
            Vector3 offset = _context.Self.right * side * _sideStep + _context.Self.forward * 0.5f;
            _context.Agent.MoveTo(_context.Self.position + offset);
        }

        public ICompanionState Tick()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _duration || _context.Agent.HasArrived)
                return _onResume();

            return null;
        }

        public void Exit() { }
    }
}