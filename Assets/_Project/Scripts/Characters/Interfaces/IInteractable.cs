using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in yanına gidip kullanabileceği dünya nesneleri (mandal, buton vb.).
    /// Bizz nesnenin türünü bilmez, yalnızca bu arayüzü görür.</summary>
    public interface IInteractable
    {
        /// <summary>Bizz'in nesneyle etkileşime girmek için durması gereken konum.</summary>
        Vector3 InteractionPoint { get; }

        /// <summary>Nesne daha önce kullanıldıysa false döner.</summary>
        bool CanInteract { get; }

        void Interact();
    }
}