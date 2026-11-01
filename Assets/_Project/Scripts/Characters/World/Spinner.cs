using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Nesneyi sürekli döndürür (rüzgâr fanı pervanesi için).</summary>
    public sealed class Spinner : MonoBehaviour
    {
        [SerializeField] private Vector3 degreesPerSecond = new Vector3(0f, 0f, 360f);

        private void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}