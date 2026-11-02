
using System;
using System.Collections.Generic;
using UnityEngine;

public class RiddleManager : MonoBehaviour
{
    [SerializeField] private int requiredCorrectAnswers = 2;
    [SerializeField] private List<RiddleData> riddlePool = new List<RiddleData>();

    private readonly HashSet<string> solvedRiddles =
        new HashSet<string>();
    private readonly HashSet<string> selectedRiddles =
    new HashSet<string>();

    public int CorrectAnswerCount => solvedRiddles.Count;

    public bool AllRiddlesSolved =>
        CorrectAnswerCount >= requiredCorrectAnswers;

    public event Action OnAllRiddlesSolved;

    public bool RegisterCorrectAnswer(string riddleId)
    {
        if (string.IsNullOrWhiteSpace(riddleId))
        {
            Debug.LogWarning("Bilmece kimliði boþ olamaz.");
            return false;
        }

        if (AllRiddlesSolved)
        {
            return false;
        }
        bool riddleExists = riddlePool != null &&
        riddlePool.Exists(riddle =>
        riddle != null &&
        riddle.RiddleId == riddleId);

        if (!riddleExists)
        {
            Debug.LogWarning("Bilmece havuzunda bulunamadý: " + riddleId);
            return false;
        }
        if (!solvedRiddles.Add(riddleId))
        {
            Debug.LogWarning(
                "Bu bilmece zaten çözüldü: " + riddleId
            );
            return false;
        }

        Debug.Log(
            "Doðru bilmece sayýsý: " +
            CorrectAnswerCount + "/" +
            requiredCorrectAnswers
        );

        if (AllRiddlesSolved)
        {
            Debug.Log("Tüm bilmeceler çözüldü!");
            OnAllRiddlesSolved?.Invoke();
        }

        return true;
    }
    public RiddleData GetRandomRiddle()
    {
        if (riddlePool == null || riddlePool.Count == 0)
        {
            Debug.LogWarning("Bilmece havuzu boþ!");
            return null;
        }

        List<RiddleData> availableRiddles = new List<RiddleData>();

        foreach (RiddleData riddle in riddlePool)
        {
            if (riddle != null &&
                !string.IsNullOrWhiteSpace(riddle.RiddleId) &&
                !selectedRiddles.Contains(riddle.RiddleId) &&
                !solvedRiddles.Contains(riddle.RiddleId))
            {
                availableRiddles.Add(riddle);
            }
        }

        if (availableRiddles.Count == 0)
        {
            Debug.LogWarning("Seçilebilecek bilmece kalmadý!");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, availableRiddles.Count);

        RiddleData selectedRiddle = availableRiddles[randomIndex];

        selectedRiddles.Add(selectedRiddle.RiddleId);

        Debug.Log("Rastgele seçilen bilmece: " + selectedRiddle.RiddleId);

        return selectedRiddle;
    }
    public void ResetProgress()
    {
        solvedRiddles.Clear();
        selectedRiddles.Clear();

        Debug.Log("Bilmece ilerlemesi ve seçim geçmiþi sýfýrlandý.");
    }
}
