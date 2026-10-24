namespace OperationHoneycomb.Characters
{
    /// <summary>Açılıp kapanabilen her kapı bu arayüzü uygular.
    /// Baykuş (LLM) tarafı kapıyı yalnızca bu arayüz üzerinden açar.</summary>
    public interface IDoor
    {
        bool IsOpen { get; }
        void Open();
    }
}