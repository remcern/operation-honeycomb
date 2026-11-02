
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewRiddle",
    menuName = "Operation Honeycomb/Riddle Data"
)]
public class RiddleData : ScriptableObject
{
    [SerializeField] private string riddleId;

    [TextArea(2, 4)]
    [SerializeField] private string question;

    [SerializeField] private string expectedAnswer;

    public string RiddleId => riddleId;
    public string Question => question;
    public string ExpectedAnswer => expectedAnswer;
}
