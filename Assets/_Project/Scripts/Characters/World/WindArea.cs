using System;
using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Yolda bir rüzgâr bölgesi. Bizz rüzgârda çok yavaşlar. Bumbu siper noktasında
    /// durursa rüzgâr kesilir ve Bizz ilerler. Bizz alanın karşı ucuna çıkınca engel aşılır.
    /// Alan her seferinde, ileri ya da geri geçişte, yeniden çalışır.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WindArea : MonoBehaviour
    {
        [Header("Rüzgâr")]
        [Range(0.05f, 1f)]
        [SerializeField] private float speedFactorInWind = 0.1f;

        [Header("Siper")]
        [Tooltip("Siper olacak karakter (Bumbu).")]
        [SerializeField] private Transform shield;
        [Tooltip("Panelin önündeki işaretli nokta.")]
        [SerializeField] private Transform shieldSpot;
        [SerializeField] private float spotRadius = 2f;

        [Header("Geçiş")]
        [Tooltip("İleri yönde Bizz'in gideceği nokta.")]
        [SerializeField] private Transform exitPoint;
        [Tooltip("Geri dönüşte Bizz'in gideceği nokta (alanın başlangıç ucunda).")]
        [SerializeField] private Transform returnExitPoint;
        [Tooltip("Alan geçildikten kaç saniye sonra tekrar kurulsun.")]
        [SerializeField] private float rearmDelay = 2f;

        private bool _crossed;
        private bool _crossedForward;
        private float _rearmAt = -1f;
        private bool _enteredFromFront;

        public bool IsSheltered { get; private set; }
        public bool TargetInside { get; private set; }
        public bool HasCrossed { get { return _crossed; } }

        /// <summary>Bizz'in şu an gitmesi gereken çıkış noktası (yön alana girişe göre).</summary>
        public Transform ExitPoint
        {
            get { return _enteredFromFront ? returnExitPoint : exitPoint; }
        }

        /// <summary>Engel aşılınca bir kez tetiklenir.</summary>
        public event Action Crossed;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void Update()
        {
            if (_crossed && !TargetInside && _rearmAt > 0f && Time.time >= _rearmAt)
            {
                _crossed = false;
                _rearmAt = -1f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<IWindAffected>() == null) return;
            _enteredFromFront = other.transform.position.z > transform.position.z;
        }

        private void OnTriggerStay(Collider other)
        {
            IWindAffected affected = other.GetComponentInParent<IWindAffected>();
            if (affected == null) return;

            if (_crossed)
            {
                affected.SetWindFactor(1f);
                return;
            }

            TargetInside = true;
            IsSheltered = ShieldInPlace();
            affected.SetWindFactor(IsSheltered ? 1f : speedFactorInWind);
        }

        private void OnTriggerExit(Collider other)
        {
            IWindAffected affected = other.GetComponentInParent<IWindAffected>();
            if (affected == null) return;

            TargetInside = false;
            affected.SetWindFactor(1f);

            bool exitedFront = other.transform.position.z > transform.position.z;
            bool crossedNow = _enteredFromFront ? !exitedFront : exitedFront;

            if (IsSheltered && crossedNow && !_crossed)
            {
                _crossed = true;
                _crossedForward = !_enteredFromFront;
                _rearmAt = Time.time + rearmDelay;
                if (Crossed != null) Crossed();
            }
            IsSheltered = false;
        }

        private bool ShieldInPlace()
        {
            if (shield == null || shieldSpot == null) return false;
            Vector3 a = shield.position;
            Vector3 b = shieldSpot.position;
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b) <= spotRadius;
        }
    }
}