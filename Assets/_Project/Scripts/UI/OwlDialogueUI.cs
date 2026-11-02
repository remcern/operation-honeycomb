

using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class OwlDialogueUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private OllamaService ollamaService;
    [SerializeField] private TMP_Text owlResponseText;
    [SerializeField] private TMP_Text riddleQuestionText;
    [SerializeField] private RiddleData currentRiddle;
    [SerializeField] private RiddleData secondRiddle;
    [SerializeField] private RiddleManager riddleManager;
    private bool isProcessingAnswer = false;
    private bool hasCompletedRiddles = false;

    private void Start()
    {
        if (answerInput == null ||
            submitButton == null ||
            ollamaService == null ||
            owlResponseText == null ||
            riddleQuestionText == null ||
            currentRiddle == null
            || riddleManager == null)
        {
            Debug.LogError(
                "OwlDialogueUI: Inspector bağlantıları eksik!"
            );

            enabled = false;
            return;
        }

        submitButton.onClick.AddListener(SubmitAnswer);

        riddleQuestionText.text = currentRiddle.Question;

        Debug.Log("Bekçi Baykuş arayüzü hazır.");
    }

    private void SubmitAnswer()
    {
        if (isProcessingAnswer)
        {
            Debug.LogWarning(
                "Önceki cevap hâlâ değerlendiriliyor."
            );
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

    private IEnumerator ProcessAnswer(string playerAnswer)
    {
        isProcessingAnswer = true;
        submitButton.interactable = false;

        Debug.Log("Oyuncunun cevabı: " + playerAnswer);

        owlResponseText.text = "Bekçi Baykuş düşünüyor...";

        yield return StartCoroutine(
            ollamaService.EvaluateRiddle(
            currentRiddle.RiddleId,
            currentRiddle.Question,
            currentRiddle.ExpectedAnswer,
                playerAnswer,
                (result, error) =>
                {

                    if (result != null)
                    {
                        string cleanResponse =
                            RemoveEmojis(result.owl_response);

                        owlResponseText.text =
                            "Bekçi Baykuş: " + cleanResponse;

                        if (result.is_correct &&
                            currentRiddle != secondRiddle &&
                            secondRiddle != null)
                        {
                            currentRiddle = secondRiddle;
                            riddleQuestionText.text = currentRiddle.Question;
                            Debug.Log(
                                "İkinci bilmeceye geçildi: " +
                                currentRiddle.RiddleId
                            );
                        }
                    }

                    else
                    {
                        Debug.LogWarning(
                            "Baykuş yanıt hatası: " + error
                        );

                        owlResponseText.text =
                            "Bekçi Baykuş şu anda cevap veremiyor.";
                    }
                }
            )
        );
        if (riddleManager.AllRiddlesSolved)
        {
            hasCompletedRiddles = true;

            riddleQuestionText.text = "Tüm bilmeceleri çözdün!";
            answerInput.text = "";
            answerInput.interactable = false;
            Debug.Log("Tüm bilmeceler tamamlandı. Cevap gönderme kapatıldı.");
        }

        isProcessingAnswer = false;
        submitButton.interactable = !hasCompletedRiddles;
    }

    private string RemoveEmojis(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        StringBuilder result = new StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            int codePoint;

            if (char.IsHighSurrogate(input[i]))
            {
                if (i + 1 < input.Length &&
                    char.IsLowSurrogate(input[i + 1]))
                {
                    codePoint = char.ConvertToUtf32(input[i], input[i + 1]);
                    i++;
                }
                else
                {
                    // Geçersiz Unicode karakterini atla.
                    continue;
                }
            }
            else if (char.IsLowSurrogate(input[i]))
            {
                // Eşleşmeyen düşük surrogate karakterini atla.
                continue;
            }
            else
            {
                codePoint = input[i];
            }

            bool isEmoji =
                (codePoint >= 0x1F000 && codePoint <= 0x1FAFF) ||
                (codePoint >= 0x2600 && codePoint <= 0x27BF) ||
                codePoint == 0xFE0F ||
                codePoint == 0x200D;

            if (!isEmoji)
            {
                result.Append(char.ConvertFromUtf32(codePoint));
            }
        }

        return result.ToString().Trim();
    }


    private void OnDestroy()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(SubmitAnswer);
        }
    }
}
