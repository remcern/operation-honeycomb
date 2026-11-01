using System.Collections;
using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Nesneyi sürekli döndürür. StopGradually çağrılınca yavaşça durur.</summary>
    public sealed class Spinner : MonoBehaviour
    {
        [SerializeField] private Vector3 degreesPerSecond = new Vector3(0f, 0f, 360f);

        private float _speedFactor = 1f;
        private bool _stopped;

        private void Update()
        {
            if (_speedFactor <= 0f) return;
            transform.Rotate(degreesPerSecond * (_speedFactor * Time.deltaTime), Space.Self);
        }

        public void StopGradually(float seconds)
        {
            if (_stopped) return;
            _stopped = true;
            StartCoroutine(SlowDown(Mathf.Max(0.1f, seconds)));
        }

        private IEnumerator SlowDown(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                _speedFactor = 1f - Mathf.Clamp01(t / seconds);
                yield return null;
            }
            _speedFactor = 0f;
        }
    }
}