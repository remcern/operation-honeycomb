using UnityEngine;
using UnityEngine.AI;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in ana denetleyicisi. Sensör, agent ve beş durumu birbirine bağlar,
    /// oyuncudan gelen emirleri ICommandable arayüzüyle kabul eder.</summary>
    [RequireComponent(typeof(CompanionAgent))]
    [RequireComponent(typeof(CompanionSensor))]
    public sealed class CompanionController : MonoBehaviour, ICommandable
    {
        private CompanionContext _context;
        private CompanionStateMachine _machine;

        private IdleState _idle;
        private GoToTargetState _goToTarget;
        private AvoidObstacleState _avoid;
        private PullLeverState _pullLever;
        private DoneState _done;

        public string CurrentStateName
        {
            get { return _machine != null && _machine.Current != null ? _machine.Current.Name : "None"; }
        }

        public bool HasOrder { get { return _context.HasOrder; } }
        public bool OrderCompleted { get { return _context.OrderCompleted; } }
        public bool OrderFailed { get { return _context.OrderFailed; } }

        private void Awake()
        {
            var agent = GetComponent<CompanionAgent>();
            var sensor = GetComponent<CompanionSensor>();
            _context = new CompanionContext(transform, agent, sensor);
            _machine = new CompanionStateMachine();

            // Durumlar birbirini tanımaz; geçişler buradan fonksiyon olarak verilir.
            _idle = new IdleState(_context, () => _goToTarget);
            _goToTarget = new GoToTargetState(
                _context,
                onInteract: () => _pullLever,
                onArrived: () => _done,
                onFailed: () => _done,
                onObstacle: () => _avoid);
            _avoid = new AvoidObstacleState(_context, () => _goToTarget);
            _pullLever = new PullLeverState(_context, () => _done);
            _done = new DoneState(_context, () => _idle);
        }

        private void Start()
        {
            _machine.Start(_idle);
        }

        private void Update()
        {
            _machine.Tick();
        }

        // ---- ICommandable ----

        public bool CanAccept(Vector3 targetPosition)
        {
            if (_context.HasOrder) return false;
            NavMeshHit hit;
            return NavMesh.SamplePosition(targetPosition, out hit, 2f, NavMesh.AllAreas);
        }

        public void GoTo(Vector3 targetPosition)
        {
            if (!CanAccept(targetPosition)) return;
            _context.TargetPosition = targetPosition;
            _context.TargetInteractable = null;
            _context.OrderCompleted = false;
            _context.OrderFailed = false;
            _context.HasOrder = true;
        }

        /// <summary>Bir nesneye (mandal vb.) gidip onu kullanma emri.</summary>
        public void GoToAndInteract(IInteractable target)
        {
            if (target == null || !CanAccept(target.InteractionPoint)) return;
            _context.TargetPosition = target.InteractionPoint;
            _context.TargetInteractable = target;
            _context.OrderCompleted = false;
            _context.OrderFailed = false;
            _context.HasOrder = true;
        }

        public void Cancel()
        {
            if (!_context.HasOrder) return;
            _context.OrderFailed = true;
            _machine.ChangeTo(_done);
        }
    }
}