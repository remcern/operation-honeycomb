using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace OperationHoneycomb.Characters
{
    /// <summary>Final sahnesi. Baykuş kapısı (IDoor) açılınca başlar: kamera yükselir, Bumbu ve Bizz
    /// bala gider, sarılırlar, Bumbu balı yer, oyun biter. Baykuş koduna bağımlı değildir;
    /// yalnızca IDoor.IsOpen değerini izler. Diğer ekip üyesi UnityEvent'lerle bağlanabilir.</summary>
    public sealed class FinaleController : MonoBehaviour
    {
        [Header("Baykuş kapısı (IDoor uygulayan nesne: OwlDoor)")]
        [SerializeField] private MonoBehaviour doorObject;
        [Tooltip("Test için Shift+K kapıyı açar. Teslimde KAPALI olmalı.")]
        [SerializeField] private bool debugOpenKey = false;

        [Header("Karakterler")]
        [SerializeField] private PlayerController player;
        [SerializeField] private CharacterController bumbuBody;
        [SerializeField] private CompanionController bizz;
        [SerializeField] private SpeechBubble bumbuBubble;
        [SerializeField] private SpeechBubble bizzBubble;

        [Header("Kamera (final sırasında yükselir)")]
        [SerializeField] private FollowCamera followCamera;
        [SerializeField] private Vector3 finaleCameraOffset = new Vector3(0f, 12f, -7f);

        [Header("Noktalar")]
        [SerializeField] private Transform bumbuSpot;
        [SerializeField] private Transform bizzSpot;
        [SerializeField] private Transform honeyJar;

        [Header("Zamanlama (saniye)")]
        [SerializeField] private float doorWait = 1.5f;
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float gap = 0.5f;
        [SerializeField] private float lineDuration = 2.2f;

        [Header("Diğer ekip üyesi için olaylar")]
        [SerializeField] private UnityEvent onFinaleStarted;
        [SerializeField] private UnityEvent onFinaleEnded;

        private IDoor _door;
        private bool _started;
        private bool _ended;
        private bool _bumbuArrived;

        private void Awake()
        {
            _door = doorObject as IDoor;
            if (_door == null)
                Debug.LogError("FinaleController: doorObject IDoor uygulamiyor ya da atanmamis.", this);
        }

        private void Update()
        {
            if (_door == null || _started) return;

            if (debugOpenKey && Keyboard.current != null && !UiTypingGuard.IsTyping)
            {
                bool shift = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
                if (shift && Keyboard.current.kKey.wasPressedThisFrame) _door.Open();
            }

            if (_door.IsOpen) StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            _started = true;
            if (onFinaleStarted != null) onFinaleStarted.Invoke();
            yield return new WaitForSeconds(doorWait);

            if (player != null) player.enabled = false;
            if (followCamera != null) followCamera.SetOffset(finaleCameraOffset);
            bizz.SetHold(false);
            bizz.SetOverrideTarget(bizzSpot);
            StartCoroutine(WalkBumbu(bumbuSpot.position));

            Say(bumbuBubble, "Kapı açıldı! Bal hemen şurada, Bizz!");
            yield return new WaitForSeconds(lineDuration + gap);
            Say(bizzBubble, "Kristalize altın bal! İnanamıyorum!");
            yield return new WaitForSeconds(lineDuration + gap);

            float timeout = 10f;
            while (timeout > 0f && !(_bumbuArrived && BizzArrived()))
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            // Sarılma
            FaceEachOther();
            Say(bumbuBubble, "Başardık Bizz! Gel, kocaman sarılalım!");
            yield return new WaitForSeconds(lineDuration + gap);
            Say(bizzBubble, "Sen en iyi dostsun Bumbu!");
            yield return StartCoroutine(HugRoutine(lineDuration + gap));

            // Balı yeme
            Say(bumbuBubble, "Hmm... Çok tatlı! Karnım doydu, uykum geldi...");
            yield return StartCoroutine(EatHoney(3f));
            Say(bizzBubble, "İyi uykular Bumbu, tatlı rüyalar!");
            yield return new WaitForSeconds(lineDuration + gap);

            _ended = true;
            if (onFinaleEnded != null) onFinaleEnded.Invoke();
        }

        private void Say(SpeechBubble bubble, string text)
        {
            if (bubble != null) bubble.Say(text, lineDuration);
        }

        private bool BizzArrived()
        {
            return Flat(bizz.transform.position, bizzSpot.position) < 1.5f;
        }

        private IEnumerator WalkBumbu(Vector3 target)
        {
            _bumbuArrived = false;
            float timeout = 12f;
            while (timeout > 0f)
            {
                Vector3 d = target - bumbuBody.transform.position;
                d.y = 0f;
                if (d.magnitude < 0.4f) break;

                Vector3 dir = d.normalized;
                bumbuBody.Move((dir * walkSpeed + Vector3.down * 2f) * Time.deltaTime);
                Quaternion look = Quaternion.LookRotation(dir);
                bumbuBody.transform.rotation = Quaternion.Slerp(bumbuBody.transform.rotation, look, 8f * Time.deltaTime);

                timeout -= Time.deltaTime;
                yield return null;
            }
            _bumbuArrived = true;
        }

        private void FaceEachOther()
        {
            Vector3 toBizz = bizz.transform.position - bumbuBody.transform.position;
            toBizz.y = 0f;
            if (toBizz.sqrMagnitude > 0.01f)
                bumbuBody.transform.rotation = Quaternion.LookRotation(toBizz);
        }

        // Bumbu zıplar, aralarında kalpler yükselir.
        private IEnumerator HugRoutine(float duration)
        {
            float t = 0f;
            float nextHeart = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                bumbuBody.Move((Vector3.up * Mathf.Cos(t * 9f) * 1.4f + Vector3.down * 1.5f) * Time.deltaTime);

                if (t >= nextHeart)
                {
                    nextHeart = t + 0.25f;
                    Vector3 mid = (bumbuBody.transform.position + bizz.transform.position) * 0.5f;
                    Vector3 start = mid + new Vector3(Random.Range(-0.5f, 0.5f), 1.8f, Random.Range(-0.3f, 0.3f));
                    StartCoroutine(HeartRoutine(start));
                }
                yield return null;
            }
        }

        private IEnumerator HeartRoutine(Vector3 start)
        {
            GameObject h = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Collider c = h.GetComponent<Collider>();
            if (c != null) Destroy(c);
            Renderer r = h.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(1f, 0.4f, 0.6f);

            float t = 0f;
            const float life = 1.6f;
            while (t < life)
            {
                t += Time.deltaTime;
                h.transform.position = start + Vector3.up * (t * 1.2f);
                h.transform.localScale = Vector3.one * (0.3f * (1f - t / life) + 0.05f);
                yield return null;
            }
            Destroy(h);
        }

        private IEnumerator EatHoney(float duration)
        {
            if (honeyJar == null) { yield return new WaitForSeconds(duration); yield break; }

            Vector3 startScale = honeyJar.localScale;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / duration) * 0.75f;
                honeyJar.localScale = startScale * k;
                yield return null;
            }
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void OnGUI()
        {
            if (!_ended) return;

            var title = new GUIStyle(GUI.skin.label);
            title.fontSize = 40;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.normal.textColor = new Color(1f, 0.85f, 0.2f);

            var sub = new GUIStyle(title);
            sub.fontSize = 22;
            sub.fontStyle = FontStyle.Normal;
            sub.normal.textColor = Color.white;

            GUI.Box(new Rect(0, Screen.height * 0.3f, Screen.width, 180f), GUIContent.none);
            GUI.Label(new Rect(0, Screen.height * 0.3f + 30f, Screen.width, 60f), "OPERATION: HONEYCOMB TAMAMLANDI", title);
            GUI.Label(new Rect(0, Screen.height * 0.3f + 100f, Screen.width, 50f),
                "Bumbu mutlu bir kış uykusuna yattı...", sub);
        }
    }
}