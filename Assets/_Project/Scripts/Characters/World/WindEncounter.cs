using System.Collections;
using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Rüzgâr sahnesi. Bizz bölgeye girince durur, Bumbu siper noktasına geçince
    /// ilerler, çıkışa varınca teşekkür edilir. Tüm mantık konum kontrolüyle çalışır.</summary>
    public sealed class WindEncounter : MonoBehaviour
    {
        private enum Phase { Idle, Blocked, Passing, Thanks, Done }

        [Header("Karakterler")]
        [SerializeField] private CompanionController bizz;
        [SerializeField] private Transform bumbu;
        [SerializeField] private SpeechBubble bizzBubble;
        [SerializeField] private SpeechBubble bumbuBubble;

        [Header("Noktalar")]
        [SerializeField] private Transform shieldSpot;
        [SerializeField] private Transform exitPoint;
        [Tooltip("Bizz bu Z değerine gelince rüzgâr başlar.")]
        [SerializeField] private float zoneStartZ = 19f;

        [Header("Mesafeler")]
        [SerializeField] private float spotRadius = 2f;
        [SerializeField] private float crossDistance = 1.5f;
        [SerializeField] private float rejoinDistance = 3.5f;

        [Header("Zamanlama (saniye)")]
        [SerializeField] private float gap = 0.4f;
        [SerializeField] private float lineDuration = 2.2f;

        private Phase _phase = Phase.Idle;
        private bool _introDone;

        private void Update()
        {
            if (bizz == null || bumbu == null || shieldSpot == null || exitPoint == null) return;

            switch (_phase)
            {
                case Phase.Idle:
                    float z = bizz.transform.position.z;
                    if (z >= zoneStartZ && z < exitPoint.position.z) EnterBlocked();
                    break;

                case Phase.Blocked:
                    if (_introDone && BumbuAtSpot()) Release();
                    break;

                case Phase.Passing:
                    if (!BumbuAtSpot())
                    {
                        _phase = Phase.Blocked;
                        bizz.SetOverrideTarget(null);
                        bizz.SetHold(true);
                        Say(bumbuBubble, "Dur Bizz, daireye geri dönüyorum!");
                    }
                    else if (Flat(bizz.transform.position, exitPoint.position) <= crossDistance)
                    {
                        StartCoroutine(ThanksRoutine());
                    }
                    break;
            }
        }

        private void EnterBlocked()
        {
            _phase = Phase.Blocked;
            _introDone = false;
            bizz.SetHold(true);
            StartCoroutine(IntroRoutine());
        }

        private IEnumerator IntroRoutine()
        {
            Say(bizzBubble, "Vızzz! Rüzgâr çok sert, ilerleyemiyorum! Bumbu, yardım et!");
            yield return new WaitForSeconds(lineDuration + gap);

            if (BumbuAtSpot()) Say(bumbuBubble, "Ben buradayım Bizz, hadi geç!");
            else Say(bumbuBubble, "Geliyorum Bizz! Önüne geçip rüzgârı kesiyorum!");
            yield return new WaitForSeconds(lineDuration + gap);

            _introDone = true;
        }

        private void Release()
        {
            _phase = Phase.Passing;
            bizz.SetHold(false);
            bizz.SetOverrideTarget(exitPoint);
            Say(bizzBubble, "Rüzgâr kesildi, geçiyorum!");
        }

        private IEnumerator ThanksRoutine()
        {
            _phase = Phase.Thanks;
            bizz.SetOverrideTarget(null);
            bizz.SetHold(true);

            yield return new WaitForSeconds(gap);
            Say(bizzBubble, "Başardık! Sen harika bir dostsun Bumbu!");
            yield return new WaitForSeconds(lineDuration + gap);
            Say(bumbuBubble, "Rica ederim Bizz! Şimdi bala gidelim!");
            yield return new WaitForSeconds(lineDuration + gap);

            // Bumbu yanına gelene kadar bekle (en fazla 20 sn), sonra takip yeniden başlar
            float timeout = 20f;
            while (timeout > 0f && Flat(bumbu.position, bizz.transform.position) > rejoinDistance)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            bizz.SetHold(false);
            _phase = Phase.Done;
        }

        private bool BumbuAtSpot()
        {
            return Flat(bumbu.position, shieldSpot.position) <= spotRadius;
        }

        private static void Say(SpeechBubble bubble, string text)
        {
            if (bubble != null) bubble.Say(text, 2.2f);
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void OnGUI()
        {
            if (_phase != Phase.Blocked || !_introDone) return;

            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 22;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = new Color(1f, 0.9f, 0.3f);
            GUI.Label(new Rect(0, Screen.height - 70f, Screen.width, 40f),
                "İpucu: Bumbu ile sarı daireye git, Bizz'e siper ol!", style);
        }
    }
}