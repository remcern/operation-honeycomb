namespace OperationHoneycomb.Characters
{
    /// <summary>Rüzgârdan etkilenebilen nesneler bu arayüzü uygular.
    /// WindArea nesnenin somut türünü bilmez, yalnızca bu arayüzü kullanır.</summary>
    public interface IWindAffected
    {
        /// <summary>Hız çarpanı: 1 = etkilenmiyor, 0.15 = çok yavaş.</summary>
        void SetWindFactor(float speedFactor);
    }
}