using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in durumlarının ortak kullandığı bilgi çantası.</summary>
    public sealed class CompanionContext
    {
        public Transform Self { get; private set; }
        public CompanionAgent Agent { get; private set; }
        public ICompanionSensor Sensor { get; private set; }

        public Transform FollowTarget { get; set; }

        /// <summary>Rüzgâr alanında takip yerine gidilecek nokta (yoksa null).</summary>
        public Transform OverrideTarget { get; set; }

        /// <summary>True ise Bizz takip etmez, olduğu yerde bekler.</summary>
        public bool HoldPosition { get; set; }

        public Vector3 TargetPosition { get; set; }
        public IInteractable TargetInteractable { get; set; }
        public bool HasOrder { get; set; }
        public bool OrderCompleted { get; set; }
        public bool OrderFailed { get; set; }

        public CompanionContext(Transform self, CompanionAgent agent, ICompanionSensor sensor)
        {
            Self = self;
            Agent = agent;
            Sensor = sensor;
        }

        public void ClearOrder()
        {
            HasOrder = false;
            TargetInteractable = null;
        }
    }
}