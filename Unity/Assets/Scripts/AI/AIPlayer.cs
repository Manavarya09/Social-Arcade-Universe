using System;
using UnityEngine;
using UnityEngine.AI;

namespace SocialArcade.Unity.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIPlayer : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private AIDifficulty _difficulty = AIDifficulty.Normal;
        
        [Header("Movement")]
        [SerializeField] private float _patrolSpeed = 2f;
        [SerializeField] private float _chaseSpeed = 4f;
        [SerializeField] private float _patrolRadius = 10f;
        
        [Header("Combat")]
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _detectionRange = 15f;
        [SerializeField] private float _chaseRange = 20f;
        
        [Header("Patrol Waypoints")]
        [SerializeField] private Transform[] _waypoints;
        
        public Transform Target { get; private set; }
        public NavMeshAgent NavMeshAgent => _navMeshAgent;
        public float AttackRange => _attackRange;
        public float ChaseSpeed => _chaseSpeed * _difficultyMultiplier;
        public float PatrolSpeed => _patrolSpeed * _difficultyMultiplier;
        
        private NavMeshAgent _navMeshAgent;
        private BehaviorTree _behaviorTree;
        private bool _isInitialized;
        private float _difficultyMultiplier = 1f;
        
        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _navMeshAgent.speed = _patrolSpeed;
        }
        
        private void Start()
        {
            InitializeBehaviorTree();
            _isInitialized = true;
        }
        
        private void Update()
        {
            if (!_isInitialized || _behaviorTree == null) return;
            
            _behaviorTree.Tick();
            
            UpdateDifficulty();
        }
        
        private void InitializeBehaviorTree()
        {
            _difficultyMultiplier = GetDifficultyMultiplier();
            
            var root = new Selector();
            
            var healthCheck = new CheckHealth(this, 0.3f);
            var fleeSequence = new Sequence();
            fleeSequence.AddChild(healthCheck);
            fleeSequence.AddChild(new Wander(this, _patrolRadius * 2));
            
            var combatSelector = new Selector();
            
            var chaseSequence = new Sequence();
            chaseSequence.AddChild(new CheckDistanceToTarget(this, _attackRange, true));
            chaseSequence.AddChild(new ChasePlayer(this, _chaseRange));
            chaseSequence.AddChild(new LookAtTarget(this));
            chaseSequence.AddChild(new AttackTarget(this, GetAttackCooldown()));
            
            var attackSequence = new Sequence();
            attackSequence.AddChild(new CheckDistanceToTarget(this, _attackRange, false));
            attackSequence.AddChild(new LookAtTarget(this));
            attackSequence.AddChild(new AttackTarget(this, GetAttackCooldown()));
            
            combatSelector.AddChild(chaseSequence);
            combatSelector.AddChild(attackSequence);
            
            var patrolSequence = new Sequence();
            if (_waypoints != null && _waypoints.Length > 0)
            {
                patrolSequence.AddChild(new Patrol(this, _waypoints));
            }
            else
            {
                patrolSequence.AddChild(new Wander(this, _patrolRadius));
            }
            
            var mainSelector = new Selector();
            mainSelector.AddChild(combatSelector);
            mainSelector.AddChild(patrolSequence);
            
            root.AddChild(fleeSequence);
            root.AddChild(mainSelector);
            
            _behaviorTree = new BehaviorTree(root);
        }
        
        private void UpdateDifficulty()
        {
            _difficultyMultiplier = GetDifficultyMultiplier();
            
            switch (_difficulty)
            {
                case AIDifficulty.Easy:
                    _navMeshAgent.speed = _patrolSpeed * 0.8f;
                    break;
                case AIDifficulty.Normal:
                    _navMeshAgent.speed = _patrolSpeed;
                    break;
                case AIDifficulty.Hard:
                    _navMeshAgent.speed = _patrolSpeed * 1.2f;
                    break;
                case AIDifficulty.Elite:
                    _navMeshAgent.speed = _patrolSpeed * 1.5f;
                    break;
            }
        }
        
        private float GetDifficultyMultiplier()
        {
            switch (_difficulty)
            {
                case AIDifficulty.Easy: return 0.7f;
                case AIDifficulty.Normal: return 1f;
                case AIDifficulty.Hard: return 1.3f;
                case AIDifficulty.Elite: return 1.6f;
                default: return 1f;
            }
        }
        
        private float GetAttackCooldown()
        {
            switch (_difficulty)
            {
                case AIDifficulty.Easy: return 1.5f;
                case AIDifficulty.Normal: return 1f;
                case AIDifficulty.Hard: return 0.8f;
                case AIDifficulty.Elite: return 0.5f;
                default: return 1f;
            }
        }
        
        public void SetTarget(Transform target)
        {
            Target = target;
        }
        
        public void ClearTarget()
        {
            Target = null;
        }
        
        public void PerformAttack()
        {
            if (Target == null) return;
            
            var targetStats = Target.GetComponent<Player.PlayerStats>();
            if (targetStats != null)
            {
                float damage = _attackDamage * _difficultyMultiplier;
                targetStats.TakeDamage(damage);
                
                Debug.Log($"{gameObject.name} attacked {Target.name} for {damage} damage");
            }
        }
        
        public void SetDifficulty(AIDifficulty difficulty)
        {
            _difficulty = difficulty;
            _difficultyMultiplier = GetDifficultyMultiplier();
        }
        
        public void SetWaypoints(Transform[] waypoints)
        {
            _waypoints = waypoints;
        }
        
        public void EnableAI()
        {
            enabled = true;
        }
        
        public void DisableAI()
        {
            enabled = false;
            if (_navMeshAgent != null)
            {
                _navMeshAgent.isStopped = true;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _chaseRange);
            
            if (_waypoints != null)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < _waypoints.Length; i++)
                {
                    if (_waypoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(_waypoints[i].position, 0.5f);
                        if (i < _waypoints.Length - 1 && _waypoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
                        }
                    }
                }
            }
        }
    }
    
    public enum AIDifficulty
    {
        Easy,
        Normal,
        Hard,
        Elite
    }
    
    public class BehaviorTree
    {
        private BehaviorNode _root;
        
        public BehaviorTree(BehaviorNode root)
        {
            _root = root;
        }
        
        public void Tick()
        {
            _root?.Execute();
        }
    }
}
