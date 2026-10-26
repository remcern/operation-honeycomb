
using System;
using System.Collections.Generic;
using UnityEngine;

public class RiddleManager : MonoBehaviour
{
    [SerializeField] private int requiredCorrectAnswers = 2;

    private readonly HashSet<string> solvedRiddles =
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

    public void ResetProgress()
    {
        solvedRiddles.Clear();
    }
}
