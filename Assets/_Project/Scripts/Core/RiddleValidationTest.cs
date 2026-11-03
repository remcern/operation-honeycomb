
using UnityEngine;

public class RiddleValidationTest : MonoBehaviour
{
    [SerializeField] private RiddleManager riddleManager;

    private void Start()
    {
        if (riddleManager == null)
        {
            Debug.LogError("Test: RiddleManager atanmamýþ!");
            return;
        }

        bool result = riddleManager.RegisterCorrectAnswer(
            "olmayan_bilmece"
        );

        if (result == false &&
            riddleManager.CorrectAnswerCount == 0)
        {
            Debug.Log("TEST BAÞARILI: Geçersiz bilmece reddedildi.");
        }
        else
        {
            Debug.LogError("TEST BAÞARISIZ: Geçersiz bilmece kabul edildi!");
        }
    }
}
