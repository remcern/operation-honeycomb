
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OperationHoneycomb.Characters;

public class OwlDialogueUI : MonoBehaviour
{
    [Header("Bilmece Arayuzu")]
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text owlResponseText;
    [SerializeField] private TMP_Text riddleQuestionText;

    [Header("LLM ve Bilmece")]
    [SerializeField] private OllamaService ollamaService;
    [SerializeField] private RiddleManager riddleManager;

    [Header("Baykus Konusma Balonu")]
    [SerializeField] private SpeechBubble owlSpeechBubble;

    [Header("Baykus Etkilesimi")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform owlSpot;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private float interactionDistance = 5f;

    [Header("Konusma Ayarlari")]
    [SerializeField] private float nextRiddleDelay = 3f;

    private RiddleData currentRiddle;
    private bool isProcessingAnswer;
    private bool hasCompletedRiddles;
    private bool wasNearOwl;

    private void Awake()
    {
        SetDialogueVisible(false);
    }

    private void Start()
    {
        if (answerInput == null ||
            submitButton == null ||
            ollamaService == null ||
            owlResponseText == null ||
            riddleQuestionText == null ||
            riddleManager == null ||
            owlSpeechBubble == null ||
            playerTransform == null ||
            owlSpot == null ||
            dialogueCanvasGroup == null)
        {
            Debug.LogError(
                "OwlDialogueUI: Inspector baglantilari eksik!"
            );

            enabled = false;
            return;
        }

        currentRiddle = riddleManager.GetRandomRiddle();

        if (currentRiddle == null)
        {
            Debug.LogError("OwlDialogueUI: Bilmece bulunamadi.");
            enabled = false;
            return;
        }

        answerInput.lineType = TMP_InputField.LineType.SingleLine;

        submitButton.onClick.AddListener(SubmitAnswer);

        riddleQuestionText.text = currentRiddle.Question;
        owlResponseText.text = "";

        Debug.Log("Bekci Baykus bilmece sistemi hazir.");
    }

    private void Update()
    {
        if (playerTransform == null ||
            owlSpot == null ||
            dialogueCanvasGroup == null)
        {
            return;
        }

        Vector3 playerPosition = playerTransform.position;
        Vector3 owlPosition = owlSpot.position;

        playerPosition.y = 0f;
        owlPosition.y = 0f;

        float distance = Vector3.Distance(
            playerPosition,
            owlPosition
        );

        bool isNearOwl = distance <= interactionDistance;

        SetDialogueVisible(isNearOwl);

        if (isNearOwl && !wasNearOwl)
        {
            if (hasCompletedRiddles)
            {
                ShowOwlSpeech(
                    "Tebrikler! Bilmecelerimi cozdunuz. " +
                    "Artik kapidan gecebilirsiniz!"
                );
            }
            else if (!isProcessingAnswer && currentRiddle != null)
            {
                ShowOwlSpeech(currentRiddle.Question);
            }
        }

        if (!isNearOwl && wasNearOwl)
        {
            HideOwlSpeech();
        }

        wasNearOwl = isNearOwl;
    }


    private void SetDialogueVisible(bool visible)
    {
        if (dialogueCanvasGroup == null)
            return;

        if (!visible && dialogueCanvasGroup.interactable)
        {
            if (answerInput != null)
            {
                answerInput.DeactivateInputField();
            }

            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current
                    .SetSelectedGameObject(null);
            }
        }

        dialogueCanvasGroup.alpha = visible ? 1f : 0f;
        dialogueCanvasGroup.interactable = visible;
        dialogueCanvasGroup.blocksRaycasts = visible;
    }


    private void ShowOwlSpeech(string message)
    {
        if (owlSpeechBubble == null ||
            string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (!IsPlayerNearOwl())
            return;

        owlSpeechBubble.Say(message, 3600f);
    }

    private void HideOwlSpeech()
    {
        if (owlSpeechBubble != null)
        {
            owlSpeechBubble.Say("", 0f);
        }
    }

    private bool IsPlayerNearOwl()
    {
        if (playerTransform == null || owlSpot == null)
            return false;

        Vector3 player = playerTransform.position;
        Vector3 owl = owlSpot.position;

        player.y = 0f;
        owl.y = 0f;

        return Vector3.Distance(player, owl)
               <= interactionDistance;
    }

    private void OnEnable()
    {
        if (answerInput != null)
        {
            answerInput.onSubmit.AddListener(OnAnswerSubmitted);
        }
    }

    private void OnDisable()
    {
        if (answerInput != null)
        {
            answerInput.onSubmit.RemoveListener(OnAnswerSubmitted);
        }

        if (owlSpeechBubble != null)
        {
            HideOwlSpeech();
        }
    }

    private void OnAnswerSubmitted(string value)
    {
        SubmitAnswer();
    }

    private void SubmitAnswer()
    {
        if (isProcessingAnswer ||
            hasCompletedRiddles ||
            !IsPlayerNearOwl() ||
            currentRiddle == null ||
            answerInput == null)
        {
            return;
        }

        string playerAnswer = answerInput.text.Trim();

        if (string.IsNullOrWhiteSpace(playerAnswer))
        {
            ShowOwlSpeech("Once bir cevap yazmalisin!");
            return;
        }

        StartCoroutine(ProcessAnswer(playerAnswer));
    }

    private IEnumerator ProcessAnswer(string playerAnswer)
    {
        isProcessingAnswer = true;

        answerInput.interactable = false;
        submitButton.interactable = false;

        Debug.Log("Oyuncunun cevabi: " + playerAnswer);

        ShowOwlSpeech("Hmm... Bir dusuneyim.");

        RiddleData nextRiddle = null;
        bool answerWasCorrect = false;
        bool evaluationSucceeded = false;

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
                        evaluationSucceeded = true;
                        answerWasCorrect = result.is_correct;

                        string cleanResponse =
                            RemoveEmojis(result.owl_response);

                        owlResponseText.text =
                            "Bekci Baykus: " + cleanResponse;

                        ShowOwlSpeech(cleanResponse);

                        if (answerWasCorrect &&
                            !riddleManager.AllRiddlesSolved)
                        {
                            nextRiddle =
                                riddleManager.GetRandomRiddle();
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            "Baykus yanit hatasi: " + error
                        );

                        string errorMessage =
                            string.IsNullOrWhiteSpace(error)
                                ? "Su anda cevap veremiyorum. Tekrar dene."
                                : error;

                        owlResponseText.text =
                            "Bekci Baykus: " + errorMessage;

                        ShowOwlSpeech(errorMessage);
                    }
                }
            )
        );

        if (evaluationSucceeded && answerWasCorrect)
        {
            answerInput.text = "";

            // Oyuncu once Ollama'nin tebrik mesajini gorur.
            yield return new WaitForSecondsRealtime(
                nextRiddleDelay
            );

            if (riddleManager.AllRiddlesSolved)
            {
                hasCompletedRiddles = true;

                riddleQuestionText.text =
                    "Tum bilmeceleri cozdun!";

                string finalMessage =
                    "Tebrikler! Iki bilmecemi de cozdunuz. " +
                    "Kapi artik acik!";

                owlResponseText.text =
                    "Bekci Baykus: " + finalMessage;

                ShowOwlSpeech(finalMessage);

                answerInput.text = "";
                answerInput.interactable = false;
                submitButton.interactable = false;

                Debug.Log(
                    "Tum bilmeceler tamamlandi. Kapi acilmali."
                );
            }
            else if (nextRiddle != null)
            {
                currentRiddle = nextRiddle;

                riddleQuestionText.text =
                    currentRiddle.Question;

                owlResponseText.text = "";

                ShowOwlSpeech(currentRiddle.Question);

                Debug.Log(
                    "Yeni bilmece secildi: " +
                    currentRiddle.RiddleId
                );
            }
            else
            {
                Debug.LogWarning(
                    "Sonraki bilmece secilemedi."
                );
            }
        }

        isProcessingAnswer = false;

        answerInput.interactable = !hasCompletedRiddles;
        submitButton.interactable = !hasCompletedRiddles;
    }

    private string RemoveEmojis(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        StringBuilder result = new StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            int codePoint;

            if (char.IsHighSurrogate(input[i]))
            {
                if (i + 1 < input.Length &&
                    char.IsLowSurrogate(input[i + 1]))
                {
                    codePoint = char.ConvertToUtf32(
                        input[i],
                        input[i + 1]
                    );

                    i++;
                }
                else
                {
                    continue;
                }
            }
            else if (char.IsLowSurrogate(input[i]))
            {
                continue;
            }
            else
            {
                codePoint = input[i];
            }

            bool isEmoji =
                (codePoint >= 0x1F000 &&
                 codePoint <= 0x1FAFF) ||
                (codePoint >= 0x2600 &&
                 codePoint <= 0x27BF) ||
                codePoint == 0xFE0F ||
                codePoint == 0x200D;

            if (!isEmoji)
            {
                result.Append(
                    char.ConvertFromUtf32(codePoint)
                );
            }
        }

        return result.ToString().Trim();
    }

    private void OnDestroy()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(
                SubmitAnswer
            );
        }
    }
}
