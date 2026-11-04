using System;
using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Emir bittiğinde (başarılı ya da başarısız) girilen durum.
    /// Emri temizler, kısa süre bekler ve Idle'a döner.</summary>
    public sealed class DoneState : ICompanionState
    {
        private readonly CompanionContext _context;
        private readonly Func<ICompanionState> _onIdle;
        private readonly float _waitTime;
        private float _elapsed;

        public string Name { get { return "Done"; } }

        public DoneState(CompanionContext context, Func<ICompanionState> onIdle, float waitTime = 0.5f)
        {
            _context = context;
            _onIdle = onIdle;
            _waitTime = waitTime;
        }

        public void Enter()
        {
            _elapsed = 0f;
            _context.Agent.Stop();
            _context.ClearOrder();
        }

        public ICompanionState Tick()
        {
            _elapsed += Time.deltaTime;
            return _elapsed >= _waitTime ? _onIdle() : null;
        }

        public void Exit() { }
    }
}