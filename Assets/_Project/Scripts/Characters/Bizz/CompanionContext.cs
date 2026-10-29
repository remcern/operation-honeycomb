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

        /// <summary>Oyuncunun verdiği emirdeki hedef nokta.</summary>
        public Vector3 TargetPosition { get; set; }

        /// <summary>Hedefte kullanılacak nesne (mandal vb.). Sadece bir noktaya gidiliyorsa null.</summary>
        public IInteractable TargetInteractable { get; set; }

        /// <summary>Bekleyen bir emir var mı?</summary>
        public bool HasOrder { get; set; }

        /// <summary>Son emir tamamlandı mı?</summary>
        public bool OrderCompleted { get; set; }

        /// <summary>Son emir başarısız oldu mu (hedefe ulaşılamadı vb.)?</summary>
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