using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in çevresini algılayan sensör. Beyin, algılamanın nasıl yapıldığını
    /// (Raycast vb.) bilmez, yalnızca bu arayüzden sonuç okur.</summary>
    public interface ICompanionSensor
    {
        /// <summary>Her karede ya da belirli aralıkla çağrılır, çevreyi yeniden tarar.</summary>
        void Scan();

        /// <summary>Hedef yönünde bir engel var mı?</summary>
        bool IsObstacleAhead { get; }

        /// <summary>En yakın engele olan mesafe (engel yoksa sonsuz).</summary>
        float ObstacleDistance { get; }

        /// <summary>Engelin Bizz'in önüne göre açısı (derece, -180 ile 180 arası).</summary>
        float ObstacleAngle { get; }

        /// <summary>Hedefin Bizz'in önüne göre açısı (derece).</summary>
        float TargetAngle(Vector3 targetPosition);
    }
}