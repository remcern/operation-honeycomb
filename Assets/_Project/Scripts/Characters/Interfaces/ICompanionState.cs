namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in durum makinesindeki her durum bu arayüzü uygular.
    /// Durum makinesi, durumun ne yaptığını bilmez; yalnızca yaşam döngüsünü yönetir.</summary>
    public interface ICompanionState
    {
        string Name { get; }

        /// <summary>Duruma girilirken bir kez çağrılır.</summary>
        void Enter();

        /// <summary>Durum aktifken her karede çağrılır.
        /// Geçilecek yeni durumu döndürür; aynı durumda kalacaksa null döner.</summary>
        ICompanionState Tick();

        /// <summary>Durumdan çıkılırken bir kez çağrılır.</summary>
        void Exit();
    }
}