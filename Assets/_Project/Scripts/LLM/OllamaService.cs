
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
        Debug.Log("OllamaService hazır. Oyuncunun cevabı bekleniyor.");
    }


    public IEnumerator EvaluateRiddle(
        string riddleId,
        string riddle,
        string expectedAnswer,
        string playerAnswer,
        Action<RiddleResult, string> onComplete = null)
    {
        string prompt =
            "Sen Operation: Honeycomb oyununda bilmece cevaplarını değerlendiren bir yapay zekasın.\n" +
            "Oyuncunun cevabının beklenen cevapla aynı nesneyi ifade edip etmediğini belirle.\n" +
            "ÖNEMLİ KURALLAR:\n" +
            "1. Tam kelime eşleşmesi zorunlu değildir.\n" +
            "2. Beklenen nesnenin bir türünü ifade eden cevaplar doğrudur.\n" +
            "3. Oyuncunun cevabına eklediği açıklayıcı sıfatlar cevabı yanlış yapmaz.\n" +
            "4. İlgisiz veya farklı nesneleri ifade eden cevaplar yanlıştır.\n" +
            "5. Önce anlamı değerlendir, ardından kısa, komik ve anlamlı bir Türkçe baykuş yanıtı oluştur.\n" +
            "6. Yalnızca is_correct ve owl_response alanlarını içeren JSON döndür.\n" +
            "7. is_correct mutlaka true veya false olmalıdır.\n" +
            "8. owl_response kısa, anlaşılır ve doğal Türkçe olmalıdır.\n" +
            "9. Emoji, gereksiz semboller veya anlamsız karakterler kullanma.Yalnızca türkçe metin yaz.\n\n" +
            "Bilmece: " + riddle + "\n" +
            "Beklenen cevap: " + expectedAnswer + "\n" +
            "Oyuncunun cevabı: " + playerAnswer;

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
                    "Ollama sunucusuna bağlanılamadı: " + request.error;

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
                    validationError = "Ollama boş yanıt verdi.";
                }
                else
                {
                    string responseJson = ollamaResponse.response;


                    if (!IsValidRiddleJson(responseJson))
                    {
                        validationError =
                            "LLM yanıtındaki JSON alanları geçersiz.";
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
                    "JSON ayrıştırma hatası: " + ex.Message
                );

                validationError = "Ollama yanıtı işlenemedi.";
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
                string error = "LLM geçersiz bilmece yanıtı verdi.";

                Debug.LogError(error);
                onComplete?.Invoke(null, error);
                yield break;
            }

            Debug.Log("Bilmece doğru mu? " + result.is_correct);
            Debug.Log("Bekçi Baykuş: " + result.owl_response);

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
                            "Bilmece başarıyla kaydedildi: " + riddleId
                        );
                    }
                }
                else
                {
                    Debug.LogError(
                        "RiddleManager bileşeni bulunamadı!"
                    );
                }
            }

            onComplete?.Invoke(result, null);
        }
    }
}

