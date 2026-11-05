using UnityEngine;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in hareket yeteneği. Durumlar NavMesh'i değil, bu sözleşmeyi bilir.</summary>
    public interface ICompanionMover
    {
        bool HasArrived { get; }
        bool PathFailed { get; }
        bool MoveTo(Vector3 target);
        void Stop();
    }
}