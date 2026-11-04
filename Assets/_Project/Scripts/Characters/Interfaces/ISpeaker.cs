namespace OperationHoneycomb.Characters
{
    /// <summary>Bir karakterin ekrana kısa bir söz söylemesini sağlayan sözleşme.</summary>
    public interface ISpeaker
    {
        void Say(string text, float duration = 2.5f);
    }
}
