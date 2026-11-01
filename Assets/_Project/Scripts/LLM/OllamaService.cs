
using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaService : MonoBehaviour
{
    private const string ApiUrl = "http://localhost:11434/api/generate";

    [Serializable]
    private class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
        public string format;
    }

    [Serializable]
    private class OllamaResponse
    {
        public string response;
    }

    [Serializable]
    public class RiddleResult
    {
        public bool is_correct;
        public string owl_response;
    }

    private bool IsValidRiddleJson(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
            return false;

        bool validCorrect = Regex.IsMatch(
            responseJson,
            "\"is_correct\"\\s*:\\s*(true|false)\\s*[,}]"
        );

        bool validResponse = Regex.IsMatch(
            responseJson,
            "\"owl_response\"\\s*:\\s*\"(?:\\\\.|[^\"\\\\])*\"\\s*[,}]"
        );


        return validCorrect && validResponse;
    }

    private void Start()
    {
        Debug.Log("OllamaService hazýr. Oyuncunun cevabý bekleniyor.");
    }


    public IEnumerator EvaluateRiddle(
        string riddleId,
        string riddle,
        string expectedAnswer,
        string playerAnswer,
        Action<RiddleResult, string> onComplete = null)
    {
        string prompt =
            "Sen Operation: Honeycomb oyununda bilmece cevaplarýný deðerlendiren bir yapay zekasýn.\n" +
            "Oyuncunun cevabýnýn beklenen cevapla ayný nesneyi ifade edip etmediðini belirle.\n" +
            "ÖNEMLÝ KURALLAR:\n" +
            "1. Tam kelime eþleþmesi zorunlu deðildir.\n" +
            "2. Beklenen nesnenin bir türünü ifade eden cevaplar doðrudur.\n" +
            "3. Oyuncunun cevabýna eklediði açýklayýcý sýfatlar cevabý yanlýþ yapmaz.\n" +
            "4. Ýlgisiz veya farklý nesneleri ifade eden cevaplar yanlýþtýr.\n" +
            "5. Önce anlamý deðerlendir, ardýndan kýsa, komik ve anlamlý bir Türkçe baykuþ yanýtý oluþtur.\n" +
            "6. Yalnýzca is_correct ve owl_response alanlarýný içeren JSON döndür.\n" +
            "7. is_correct mutlaka true veya false olmalýdýr.\n" +
            "8. owl_response kýsa, anlaþýlýr ve doðal Türkçe olmalýdýr.\n" +
            "9. Emoji, gereksiz semboller veya anlamsýz karakterler kullanma.Yalnýzca türkçe metin yaz.\n\n" +
            "Bilmece: " + riddle + "\n" +
            "Beklenen cevap: " + expectedAnswer + "\n" +
            "Oyuncunun cevabý: " + playerAnswer;

        OllamaRequest requestData = new OllamaRequest
        {
            model = "qwen2.5:7b",
            prompt = prompt,
            stream = false,
            format = "json"
        };

        string json = JsonUtility.ToJson(requestData);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 120;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error =
                    "Ollama sunucusuna baðlanýlamadý: " + request.error;

                Debug.LogError(error);
                onComplete?.Invoke(null, error);
                yield break;
            }

            RiddleResult result = null;
            string validationError = null;

            try
            {
                OllamaResponse ollamaResponse =
                    JsonUtility.FromJson<OllamaResponse>(
                        request.downloadHandler.text
                    );

                if (ollamaResponse == null ||
                    string.IsNullOrWhiteSpace(ollamaResponse.response))
                {
                    validationError = "Ollama boþ yanýt verdi.";
                }
                else
                {
                    string responseJson = ollamaResponse.response;


                    if (!IsValidRiddleJson(responseJson))
                    {
                        validationError =
                            "LLM yanýtýndaki JSON alanlarý geçersiz.";
                    }
                    else
                    {
                        result = JsonUtility.FromJson<RiddleResult>(
                            responseJson
                        );
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "JSON ayrýþtýrma hatasý: " + ex.Message
                );

                validationError = "Ollama yanýtý iþlenemedi.";
            }

            if (validationError != null)
            {
                Debug.LogError(validationError);
                onComplete?.Invoke(null, validationError);
                yield break;
            }

            if (result == null ||
                string.IsNullOrWhiteSpace(result.owl_response))
            {
                string error = "LLM geçersiz bilmece yanýtý verdi.";

                Debug.LogError(error);
                onComplete?.Invoke(null, error);
                yield break;
            }

            Debug.Log("Bilmece doðru mu? " + result.is_correct);
            Debug.Log("Bekçi Baykuþ: " + result.owl_response);

            if (result.is_correct)
            {
                RiddleManager riddleManager =
                    GetComponent<RiddleManager>();

                if (riddleManager != null)
                {
                    bool registered =
                        riddleManager.RegisterCorrectAnswer(riddleId);

                    if (registered)
                    {
                        Debug.Log(
                            "Bilmece baþarýyla kaydedildi: " + riddleId
                        );
                    }
                }
                else
                {
                    Debug.LogError(
                        "RiddleManager bileþeni bulunamadý!"
                    );
                }
            }

            onComplete?.Invoke(result, null);
        }
    }
}

