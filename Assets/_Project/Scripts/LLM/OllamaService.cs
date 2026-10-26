
using System;
using System.Collections;
using System.Text;
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

    private void Start()
    {
        StartCoroutine(EvaluateRiddle(
            "bilmece_1",
            "Dört ayaðý var ama yürüyemez. Nedir?",
            "masa",
            "yemek masasý"
        ));
        StartCoroutine(EvaluateRiddle(
        "bilmece_2",
        "Kanatlarý vardýr ama kuþ deðildir. Uçar ama canlý deðildir. Nedir?",
        "uçak",
        "yolcu uçaðý"
        ));
    }


    public IEnumerator EvaluateRiddle(
        string riddleId,
        string riddle,
        string expectedAnswer,
        string playerAnswer)
    {
        string prompt =
     "Sen Operation: Honeycomb oyununda bilmece cevaplarýný deðerlendiren bir yapay zekasýn.\n" +
     "Oyuncunun cevabýnýn beklenen cevapla ayný nesneyi ifade edip etmediðini belirle.\n" +
     "ÖNEMLÝ KURALLAR:\n" +
     "1. Tam kelime eþleþmesi zorunlu deðildir.\n" +
     "2. Beklenen nesnenin bir türünü ifade eden cevaplar doðrudur.\n" +
     "3. Oyuncunun cevabýna eklediði açýklayýcý sýfatlar cevabý yanlýþ yapmaz.\n" +
     "4. Ýlgisiz veya farklý nesneleri ifade eden cevaplar yanlýþtýr.\n" +
     "5. Önce anlamý deðerlendir, ardýndan kýsa bir Türkçe baykuþ yanýtý oluþtur.\n" +
     "6. Yalnýzca is_correct ve owl_response alanlarýný içeren JSON döndür.\n\n" +
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
                Debug.LogError("Ollama baðlantý hatasý: " + request.error);
                yield break;
            }

            OllamaResponse ollamaResponse;
            RiddleResult result;

            try
            {
                ollamaResponse = JsonUtility.FromJson<OllamaResponse>(
                    request.downloadHandler.text
                );

                if (ollamaResponse == null ||
                    string.IsNullOrWhiteSpace(ollamaResponse.response))
                {
                    Debug.LogError("Ollama boþ yanýt verdi.");
                    yield break;
                }

                result = JsonUtility.FromJson<RiddleResult>(
                    ollamaResponse.response
                );
            }
            catch (Exception ex)
            {
                Debug.LogError("JSON ayrýþtýrma hatasý: " + ex.Message);
                yield break;
            }

            if (result == null ||
                string.IsNullOrWhiteSpace(result.owl_response))
            {
                Debug.LogError("LLM geçersiz bilmece yanýtý verdi.");
                yield break;
            }

            Debug.Log("Bilmece doðru mu? " + result.is_correct);
            Debug.Log("Bekçi Baykuþ: " + result.owl_response);

            if (result.is_correct)
            {
                RiddleManager riddleManager = GetComponent<RiddleManager>();

                if (riddleManager != null)
                {
                    bool registered =
                        riddleManager.RegisterCorrectAnswer(riddleId);

                    if (registered)
                    {
                        Debug.Log("Bilmece baþarýyla kaydedildi: " + riddleId);
                    }
                }
                else
                {
                    Debug.LogError("RiddleManager bileþeni bulunamadý!");
                }
            }

        }
    }
}
