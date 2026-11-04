
using UnityEngine;

public class RiddleCompletionEventTest : MonoBehaviour
{
    [SerializeField] private RiddleManager riddleManager;

    private int eventCount = 0;

    private void OnEnable()
    {
        if (riddleManager != null)
        {
            riddleManager.OnAllRiddlesSolved += HandleCompletion;
        }
    }

    private void OnDisable()
    {
        if (riddleManager != null)
        {
            riddleManager.OnAllRiddlesSolved -= HandleCompletion;
        }
    }

    private void HandleCompletion()
    {
        eventCount++;

        Debug.Log(
            "TEST BASARILI: OnAllRiddlesSolved tetiklendi. " +
            "Olay sayisi: " + eventCount
        );
    }
}
