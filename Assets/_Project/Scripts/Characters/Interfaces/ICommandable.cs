using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Oyuncudan emir alabilen karakterler (örn. Bizz) bu arayüzü uygular.
    /// Emri veren sınıf, karakterin iç yapısını bilmeden yalnızca bu arayüzü kullanır.</summary>
    public interface ICommandable
    {
        /// <summary>Verilen hedefe gitme emrini kabul eder. Emir kabul edildiyse true döner.</summary>
        bool CanAccept(Vector3 targetPosition);

        void GoTo(Vector3 targetPosition);

        /// <summary>Devam eden emri iptal eder.</summary>
        void Cancel();
    }
}