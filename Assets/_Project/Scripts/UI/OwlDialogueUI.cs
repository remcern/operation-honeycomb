
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OwlDialogueUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private OllamaService ollamaService;
    private bool isProcessingAnswer = false;

    private void Start()
    {
        if (answerInput == null || submitButton == null || ollamaService == null)
        {
            Debug.LogError("OwlDialogueUI: Inspector baðlantýlarý eksik!");
            return;
        }

        submitButton.onClick.AddListener(SubmitAnswer);

        Debug.Log("Bekçi Baykuþ arayüzü hazýr.");
    }

    private void SubmitAnswer()
    {
        if (isProcessingAnswer)
        {
            Debug.LogWarning("Önceki cevap hâlâ deðerlendiriliyor.");
            return;
        }

        string playerAnswer = answerInput.text.Trim();

        if (string.IsNullOrWhiteSpace(playerAnswer))
        {
            Debug.LogWarning("Lütfen bir cevap yaz.");
            return;
        }

        StartCoroutine(ProcessAnswer(playerAnswer));
    }
    private System.Collections.IEnumerator ProcessAnswer(string playerAnswer)
    {
        isProcessingAnswer = true;
        submitButton.interactable = false;

        Debug.Log("Oyuncunun cevabý: " + playerAnswer);

        yield return StartCoroutine(ollamaService.EvaluateRiddle(
            "bilmece_1",
            "Dört ayaðý var ama yürüyemez. Nedir?",
            "masa",
            playerAnswer
        ));

        isProcessingAnswer = false;
        submitButton.interactable = true;
    }


    private void OnDestroy()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(SubmitAnswer);
        }
    }
}
