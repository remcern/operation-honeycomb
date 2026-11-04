
using UnityEngine;
using OperationHoneycomb.Characters;

public class OwlDoorController : MonoBehaviour
{
    [SerializeField] private RiddleManager riddleManager;
    [SerializeField] private Door owlDoor;

    private void OnEnable()
    {
        if (riddleManager != null)
        {
            riddleManager.OnAllRiddlesSolved += OpenOwlDoor;
        }
    }

    private void Start()
    {
        if (riddleManager == null || owlDoor == null)
        {
            Debug.LogWarning(
                "OwlDoorController: Inspector baðlantýlarý eksik!"
            );
            return;
        }

        if (riddleManager.AllRiddlesSolved)
        {
            OpenOwlDoor();
        }
    }

    private void OnDisable()
    {
        if (riddleManager != null)
        {
            riddleManager.OnAllRiddlesSolved -= OpenOwlDoor;
        }
    }

    private void OpenOwlDoor()
    {
        if (owlDoor == null)
        {
            Debug.LogWarning("Baykuþ kapýsý bulunamadý.");
            return;
        }

        owlDoor.Open();
        Debug.Log("LLM bilmeceleri tamamlandý. Baykuþ kapýsý açýlýyor!");
    }
}
