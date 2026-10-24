namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz emir beklerken içinde bulunduğu durum.</summary>
    public sealed class IdleState : ICompanionState
    {
        private readonly CompanionContext _context;
        private readonly System.Func<ICompanionState> _goToTarget;

        public string Name { get { return "Idle"; } }

        /// <param name="goToTarget">Emir gelince geçilecek durumu veren fonksiyon.
        /// Durumlar birbirini doğrudan tanımasın diye dışarıdan verilir.</param>
        public IdleState(CompanionContext context, System.Func<ICompanionState> goToTarget)
        {
            _context = context;
            _goToTarget = goToTarget;
        }

        public void Enter()
        {
            _context.Agent.Stop();
        }

        public ICompanionState Tick()
        {
            return _context.HasOrder ? _goToTarget() : null;
        }

        public void Exit() { }
    }
}