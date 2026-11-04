using System;
using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Rüzgâr bölgesi. Bizz rüzgârda çok yavaşlar. Bumbu siper noktasında durursa
    /// rüzgâr kesilir. Engel, Bizz çıkış noktasına gerçekten varınca aşılmış sayılır.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WindArea : MonoBehaviour
    {
        [Header("Rüzgâr")]
        [Range(0.05f, 1f)]
        [SerializeField] private float speedFactorInWind = 0.1f;

        [Header("Siper")]
        [SerializeField] private Transform shield;
        [SerializeField] private Transform shieldSpot;
        [SerializeField] private float spotRadius = 2f;

        [Header("Geçiş")]
        [Tooltip("İleri yönde Bizz'in gideceği nokta.")]
        [SerializeField] private Transform exitPoint;
        [Tooltip("Geri dönüşte Bizz'in gideceği nokta.")]
        [SerializeField] private Transform returnExitPoint;
        [Tooltip("Bizz çıkış noktasına bu kadar yaklaşınca engel aşılmış sayılır.")]
        [SerializeField] private float crossDistance = 1.5f;
        [SerializeField] private float rearmDelay = 2f;

        private Transform _target;
        private bool _active;
        private bool _crossed;
        private bool _wasShielded;
        private bool _enteredFromFront;
        private float _rearmAt = -1f;

        public bool IsSheltered { get; private set; }
        public bool TargetInside { get; private set; }

        /// <summary>Bizz alana girdi ve henüz çıkış noktasına varmadı.</summary>
        public bool IsActive { get { return _active; } }
        public bool HasCrossed { get { return _crossed; } }

        public Transform ExitPoint
        {
            get { return _enteredFromFront ? returnExitPoint : exitPoint; }
        }

        /// <summary>Engel gerçekten aşılınca bir kez tetiklenir.</summary>
        public event Action Crossed;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void Update()
        {
            if (_active && _target != null && ExitPoint != null)
            {
                if (Flat(_target.position, ExitPoint.position) <= crossDistance)
                {
                    _active = false;
                    if (_wasShielded && !_crossed)
                    {
                        _crossed = true;
                        _rearmAt = Time.time + rearmDelay;
                        if (Crossed != null) Crossed();
                    }
                }
            }

            if (_crossed && !_active && !TargetInside && _rearmAt > 0f && Time.time >= _rearmAt)
            {
                _crossed = false;
                _rearmAt = -1f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            IWindAffected affected = other.GetComponentInParent<IWindAffected>();
            Component comp = affected as Component;
            if (comp == null || _crossed) return;

            if (!_active)
            {
                _target = comp.transform;
                _enteredFromFront = other.transform.position.z > transform.position.z;
                _wasShielded = false;
                _active = true;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            IWindAffected affected = other.GetComponentInParent<IWindAffected>();
            if (affected == null) return;

            if (_crossed || !_active)
            {
                affected.SetWindFactor(1f);
                return;
            }

            TargetInside = true;
            IsSheltered = ShieldInPlace();
            if (IsSheltered) _wasShielded = true;
            affected.SetWindFactor(IsSheltered ? 1f : speedFactorInWind);
        }

        private void OnTriggerExit(Collider other)
        {
            IWindAffected affected = other.GetComponentInParent<IWindAffected>();
            if (affected == null) return;

            TargetInside = false;
            IsSheltered = false;
            affected.SetWindFactor(1f);

            // Geldiği yönden geri çıktıysa sahne iptal; karşı taraftan çıktıysa hedefe gitmeye devam eder.
            bool exitedFront = other.transform.position.z > transform.position.z;
            if (exitedFront == _enteredFromFront)
                _active = false;
        }

        private bool ShieldInPlace()
        {
            if (shield == null || shieldSpot == null) return false;
            return Flat(shield.position, shieldSpot.position) <= spotRadius;
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}