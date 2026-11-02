

using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    public class RiddleResult
    {
        public bool is_correct;
        public string owl_response;
    }

    private bool TryParseRiddleResult(
        string responseJson,
        out RiddleResult result)
    {
        result = null;

        try
        {
            JObject json = JObject.Parse(responseJson);

            JToken correctToken = json["is_correct"];
            JToken responseToken = json["owl_response"];

            if (correctToken == null ||
                correctToken.Type != JTokenType.Boolean)
            {
                return false;
            }

            if (responseToken == null ||
                responseToken.Type != JTokenType.String)
            {
                return false;
            }

            string owlResponse = responseToken.Value<string>();

            if (string.IsNullOrWhiteSpace(owlResponse))
            {
                return false;
            }

            result = new RiddleResult
            {
                is_correct = correctToken.Value<bool>(),
                owl_response = owlResponse
            };

            return true;
        }
        catch (JsonException ex)
        {
            Debug.LogWarning("Geçersiz JSON: " + ex.Message);
            return false;
        }
        catch (ArgumentException ex)
        {
            Debug.LogWarning("JSON veri hatası: " + ex.Message);
            return false;
        }
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
            "9. Emoji, gereksiz semboller veya anlamsız karakterler kullanma. Yalnızca Türkçe metin yaz.\n\n" +
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

        string requestJson = JsonConvert.SerializeObject(requestData);
        byte[] body = Encoding.UTF8.GetBytes(requestJson);

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
                JObject ollamaResponse =
                    JObject.Parse(request.downloadHandler.text);

                JToken responseToken = ollamaResponse["response"];

                if (responseToken == null ||
                    responseToken.Type != JTokenType.String ||
                    string.IsNullOrWhiteSpace(responseToken.Value<string>()))
                {
                    validationError = "Ollama boş veya geçersiz yanıt verdi.";
                }
                else if (!TryParseRiddleResult(
                    responseToken.Value<string>(),
                    out result))
                {
                    validationError =
                        "LLM yanıtındaki JSON alanları geçersiz.";
                }
            }
            catch (JsonException ex)
            {
                Debug.LogWarning("Ollama JSON hatası: " + ex.Message);
                validationError = "Ollama yanıtı işlenemedi.";
            }
            catch (ArgumentException ex)
            {
                Debug.LogWarning("Ollama veri hatası: " + ex.Message);
                validationError = "Ollama yanıtı işlenemedi.";
            }

            if (validationError != null)
            {
                Debug.LogError(validationError);
                onComplete?.Invoke(null, validationError);
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

