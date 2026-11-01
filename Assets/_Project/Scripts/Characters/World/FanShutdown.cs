using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz rüzgâr çıkışına varınca pervaneyi yavaşça durdurur ve Bumbu bir söz söyler.
    /// WindEncounter'a dokunmaz; yalnızca Bizz'in konumunu izler. Tek seferliktir.</summary>
    public sealed class FanShutdown : MonoBehaviour
    {
        [SerializeField] private Transform bizz;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private Spinner fanSpinner;
        [SerializeField] private SpeechBubble bumbuBubble;
        [SerializeField] private float triggerDistance = 1.5f;
        [Tooltip("Teşekkür konuşmaları bitsin diye bekleme (sn).")]
        [SerializeField] private float delay = 5f;
        [SerializeField] private float stopSeconds = 3f;

        private bool _triggered;
        private float _timer;
        private bool _counting;

        private void Update()
        {
            if (_triggered || bizz == null || exitPoint == null || fanSpinner == null) return;

            if (!_counting)
            {
                Vector3 a = bizz.position;
                Vector3 b = exitPoint.position;
                a.y = 0f;
                b.y = 0f;
                if (Vector3.Distance(a, b) <= triggerDistance) _counting = true;
                return;
            }

            _timer += Time.deltaTime;
            if (_timer >= delay)
            {
                _triggered = true;
                fanSpinner.StopGradually(stopSeconds);
                if (bumbuBubble != null) bumbuBubble.Say("Bak, fan durdu! Artık rüzgâr yok!", 2.5f);
            }
        }
    }
}