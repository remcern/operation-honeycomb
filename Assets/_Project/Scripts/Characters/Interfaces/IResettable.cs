namespace OperationHoneycomb.Characters
{
    /// <summary>Başlangıç durumuna döndürülebilen dünya nesneleri (kapı, mandal).
    /// Test modu bu arayüzle nesnelerin somut türünü bilmeden sıfırlama yapar.</summary>
    public interface IResettable
    {
        void ResetState();
    }
}