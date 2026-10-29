using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in durumlarının ortak kullandığı bilgi çantası.
    /// Durumlar birbirini tanımaz, yalnızca bu bağlam üzerinden veri paylaşır.</summary>
    public sealed class CompanionContext
    {
        public Transform Self { get; private set; }
        public CompanionAgent Agent { get; private set; }
        public ICompanionSensor Sensor { get; private set; }

        /// <summary>Emir yokken takip edilecek hedef (Bumbu). Boşsa takip yapılmaz.</summary>
        public Transform FollowTarget { get; set; }

        /// <summary>Rüzgâr alanında takip yerine gidilecek nokta (yoksa null).</summary>
        public Transform OverrideTarget { get; set; }

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