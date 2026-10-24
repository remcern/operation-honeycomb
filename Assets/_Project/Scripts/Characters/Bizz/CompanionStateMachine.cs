namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in durum makinesi. Aktif durumu tutar ve geçişleri yönetir.
    /// Durumların ne yaptığını bilmez, yalnızca ICompanionState sözleşmesini kullanır.</summary>
    public sealed class CompanionStateMachine
    {
        public ICompanionState Current { get; private set; }

        public void Start(ICompanionState initial)
        {
            Current = initial;
            Current.Enter();
        }

        /// <summary>Her karede çağrılır. Aktif durum yeni bir durum döndürürse geçiş yapar.</summary>
        public void Tick()
        {
            if (Current == null) return;

            ICompanionState next = Current.Tick();
            if (next != null && next != Current)
                ChangeTo(next);
        }

         public void ChangeTo(ICompanionState next)
        {
            Current.Exit();
            Current = next;
            Current.Enter();
        }
    }
}