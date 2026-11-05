using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace OperationHoneycomb.Characters
{
    /// <summary>Final sahnesi. Baykuş kapısı (IDoor) açılınca başlar: kamera yükselir, Bumbu ve Bizz
    /// bala gider, birbirine dönüp sarılırlar (kalpler yükselir), Bumbu balı yer, oyun biter.
    /// Baykuş koduna bağımlı değildir; yalnızca IDoor.IsOpen değerini izler.</summary>
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

        [Header("Sarılma efekti")]
        [SerializeField] private float heartScale = 0.9f;
        [SerializeField] private Color heartColor = new Color(1f, 0.25f, 0.5f);

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

            // Bizz artık yönlendirilmesin; olduğu yerde dursun ve Bumbu'ya dönsün
            bizz.SetOverrideTarget(null);
            bizz.SetHold(true);

            // Sarılma
            Say(bumbuBubble, "Başardık Bizz! Gel, kocaman sarılalım!");
            yield return StartCoroutine(FaceEachOtherFor(lineDuration + gap));
            Say(bizzBubble, "Sen en iyi dostsun Bumbu!");
            yield return StartCoroutine(HugRoutine(lineDuration + gap + 1f));

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

        // İkisi de birbirine yumuşakça döner.
        private void FaceEachOtherStep()
        {
            Vector3 bumbuPos = bumbuBody.transform.position;
            Vector3 bizzPos = bizz.transform.position;

            Vector3 toBizz = bizzPos - bumbuPos;
            toBizz.y = 0f;
            if (toBizz.sqrMagnitude > 0.001f)
            {
                Quaternion q = Quaternion.LookRotation(toBizz);
                bumbuBody.transform.rotation = Quaternion.Slerp(bumbuBody.transform.rotation, q, 8f * Time.deltaTime);
            }

            Vector3 toBumbu = bumbuPos - bizzPos;
            toBumbu.y = 0f;
            if (toBumbu.sqrMagnitude > 0.001f)
            {
                Quaternion q = Quaternion.LookRotation(toBumbu);
                bizz.transform.rotation = Quaternion.Slerp(bizz.transform.rotation, q, 8f * Time.deltaTime);
            }
        }

        private IEnumerator FaceEachOtherFor(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                FaceEachOtherStep();
                yield return null;
            }
        }

        // Bumbu zıplar, ikisi birbirine dönük kalır, aralarında kalpler yükselir.
        private IEnumerator HugRoutine(float duration)
        {
            float t = 0f;
            float nextHeart = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                FaceEachOtherStep();
                bumbuBody.Move((Vector3.up * Mathf.Cos(t * 9f) * 1.4f + Vector3.down * 1.5f) * Time.deltaTime);

                if (t >= nextHeart)
                {
                    nextHeart = t + 0.3f;
                    Vector3 mid = (bumbuBody.transform.position + bizz.transform.position) * 0.5f;
                    Vector3 start = mid + new Vector3(Random.Range(-0.6f, 0.6f), 2.2f, Random.Range(-0.3f, 0.3f));
                    StartCoroutine(HeartRoutine(start));
                }
                yield return null;
            }
        }

        // Gerçek kalp şekli: iki küre ve 45 derece döndürülmüş bir küp.
        private IEnumerator HeartRoutine(Vector3 start)
        {
            GameObject root = new GameObject("Heart");
            root.transform.position = start;

            MakePart(root.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.5f, 0.5f, 0.2f), Quaternion.Euler(0f, 0f, 45f));
            MakePart(root.transform, PrimitiveType.Sphere, new Vector3(-0.177f, 0.177f, 0f), new Vector3(0.5f, 0.5f, 0.2f), Quaternion.identity);
            MakePart(root.transform, PrimitiveType.Sphere, new Vector3(0.177f, 0.177f, 0f), new Vector3(0.5f, 0.5f, 0.2f), Quaternion.identity);

            float t = 0f;
            const float life = 2.2f;
            while (t < life)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / life);
                root.transform.position = start + Vector3.up * (t * 1.3f);
                float pop = k < 0.15f ? k / 0.15f : 1f;           // hızlı belirme
                float fade = 1f - Mathf.Clamp01((k - 0.7f) / 0.3f); // sonda küçülme
                root.transform.localScale = Vector3.one * heartScale * pop * fade;

                // Kameraya doğru çevir (üstten bakınca da kalp şekli görünsün)
                if (Camera.main != null)
                    root.transform.rotation = Quaternion.LookRotation(root.transform.position - Camera.main.transform.position);
                yield return null;
            }
            Destroy(root);
        }

        private void MakePart(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 localScale, Quaternion localRot)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            Collider c = part.GetComponent<Collider>();
            if (c != null) Destroy(c);

            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localRotation = localRot;
            part.transform.localScale = localScale;

            Renderer r = part.GetComponent<Renderer>();
            if (r != null)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.material.color = heartColor;
            }
        }

        private IEnumerator EatHoney(float duration)
        {
            if (honeyJar == null) { yield return new WaitForSeconds(duration); yield break; }

            Vector3 startScale = honeyJar.localScale;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                FaceEachOtherStep();
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