using UnityEngine;
using UnityEngine.AI;

namespace OperationHoneycomb.Characters
{
    /// <summary>Bizz'in ana denetleyicisi. Sensör, agent ve durumları birbirine bağlar,
    /// oyuncudan gelen emirleri ICommandable arayüzüyle kabul eder.</summary>
    [RequireComponent(typeof(CompanionAgent))]
    [RequireComponent(typeof(CompanionSensor))]
    public sealed class CompanionController : MonoBehaviour, ICommandable
    {
        [Header("Takip (bos birakilirsa Bizz yerinde bekler)")]
        [SerializeField] private Transform followTarget;

        private CompanionContext _context;
        private CompanionStateMachine _machine;
        private CompanionAgent _agentRef;

        private IdleState _idle;
        private FollowPlayerState _follow;
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

        public bool IsAvailable
        {
            get { return CurrentStateName == "Idle" || CurrentStateName == "Follow"; }
        }

        public float WindFactorNow
        {
            get { return _agentRef != null ? _agentRef.WindFactor : 1f; }
        }

        /// <summary>Rüzgâr alanında Bizz'in gideceği nokta (null verilirse normal takip).</summary>
        public void SetOverrideTarget(Transform t)
        {
            if (_context != null) _context.OverrideTarget = t;
        }

        /// <summary>True verilirse Bizz takibi bırakıp olduğu yerde bekler.</summary>
        public void SetHold(bool hold)
        {
            if (_context != null) _context.HoldPosition = hold;
        }

        private void Awake()
        {
            _agentRef = GetComponent<CompanionAgent>();
            CompanionSensor sensor = GetComponent<CompanionSensor>();
            _context = new CompanionContext(transform, _agentRef, sensor);
            _context.FollowTarget = followTarget;
            _machine = new CompanionStateMachine();

            _idle = new IdleState(_context, () => _goToTarget);
            _follow = new FollowPlayerState(_context, () => _goToTarget);
            _goToTarget = new GoToTargetState(
                _context,
                () => _pullLever,
                () => _done,
                () => _done,
                () => _avoid);
            _avoid = new AvoidObstacleState(_context, () => _goToTarget);
            _pullLever = new PullLeverState(_context, () => _done);
            _done = new DoneState(_context, () => RestState());
        }

        private ICompanionState RestState()
        {
            if (_context.FollowTarget != null) return _follow;
            return _idle;
        }

        private void Start()
        {
            _machine.Start(RestState());
        }

        private void Update()
        {
            _machine.Tick();
        }

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