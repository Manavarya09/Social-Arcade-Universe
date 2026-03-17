using System;
using UnityEngine;
using UnityEngine.AI;

namespace SocialArcade.Unity.AI
{
    public class AIActionNode : BehaviorNode
    {
        protected AIPlayer aiPlayer;
        
        public AIActionNode(AIPlayer player)
        {
            aiPlayer = player;
            NodeId = Guid.NewGuid().ToString();
        }
    }
    
    public class MoveToTarget : AIActionNode
    {
        private float stopDistance = 1.5f;
        private float rotationSpeed = 5f;
        
        public MoveToTarget(AIPlayer player, float stopDistance = 1.5f) : base(player)
        {
            this.stopDistance = stopDistance;
        }
        
        public override NodeState Execute()
        {
            if (aiPlayer.Target == null)
            {
                return NodeState.Failure;
            }
            
            float distance = Vector3.Distance(aiPlayer.transform.position, aiPlayer.Target.position);
            
            if (distance <= stopDistance)
            {
                aiPlayer.NavMeshAgent.isStopped = true;
                return NodeState.Success;
            }
            
            aiPlayer.NavMeshAgent.SetDestination(aiPlayer.Target.position);
            aiPlayer.NavMeshAgent.isStopped = false;
            
            Vector3 direction = (aiPlayer.Target.position - aiPlayer.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            aiPlayer.transform.rotation = Quaternion.Slerp(
                aiPlayer.transform.rotation,
                lookRotation,
                Time.deltaTime * rotationSpeed
            );
            
            return NodeState.Running;
        }
    }
    
    public class ChasePlayer : AIActionNode
    {
        private float chaseDistance = 20f;
        
        public ChasePlayer(AIPlayer player, float chaseDistance = 20f) : base(player)
        {
            this.chaseDistance = chaseDistance;
        }
        
        public override NodeState Execute()
        {
            if (aiPlayer.Target == null)
            {
                return NodeState.Failure;
            }
            
            float distance = Vector3.Distance(aiPlayer.transform.position, aiPlayer.Target.position);
            
            if (distance > chaseDistance)
            {
                return NodeState.Failure;
            }
            
            aiPlayer.NavMeshAgent.SetDestination(aiPlayer.Target.position);
            aiPlayer.NavMeshAgent.speed = aiPlayer.ChaseSpeed;
            aiPlayer.NavMeshAgent.isStopped = false;
            
            return NodeState.Running;
        }
    }
    
    public class AttackTarget : AIActionNode
    {
        private float attackCooldown = 1f;
        private float lastAttackTime;
        
        public AttackTarget(AIPlayer player, float cooldown = 1f) : base(player)
        {
            attackCooldown = cooldown;
        }
        
        public override NodeState Execute()
        {
            if (aiPlayer.Target == null)
            {
                return NodeState.Failure;
            }
            
            if (Time.time - lastAttackTime < attackCooldown)
            {
                return NodeState.Running;
            }
            
            float distance = Vector3.Distance(aiPlayer.transform.position, aiPlayer.Target.position);
            
            if (distance > aiPlayer.AttackRange)
            {
                return NodeState.Failure;
            }
            
            aiPlayer.PerformAttack();
            lastAttackTime = Time.time;
            
            return NodeState.Success;
        }
    }
    
    public class Wander : AIActionNode
    {
        private float wanderRadius = 10f;
        private float wanderTimer;
        private float wanderInterval = 5f;
        
        public Wander(AIPlayer player, float radius = 10f) : base(player)
        {
            wanderRadius = radius;
        }
        
        public override NodeState Execute()
        {
            wanderTimer += Time.deltaTime;
            
            if (wanderTimer >= wanderInterval)
            {
                Vector3 newPos = GetRandomPoint(aiPlayer.transform.position, wanderRadius);
                aiPlayer.NavMeshAgent.SetDestination(newPos);
                wanderTimer = 0;
            }
            
            return NodeState.Running;
        }
        
        private Vector3 GetRandomPoint(Vector3 center, float radius)
        {
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
            randomDirection += center;
            
            UnityEngine.AI.NavMeshHit hit;
            UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out hit, radius, 1);
            
            return hit.position;
        }
    }
    
    public class Patrol : AIActionNode
    {
        private Transform[] waypoints;
        private int currentWaypointIndex;
        
        public Patrol(AIPlayer player, Transform[] waypoints) : base(player)
        {
            this.waypoints = waypoints;
        }
        
        public override NodeState Execute()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                return NodeState.Failure;
            }
            
            Transform targetWaypoint = waypoints[currentWaypointIndex];
            
            float distance = Vector3.Distance(aiPlayer.transform.position, targetWaypoint.position);
            
            if (distance < 1f)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
            
            aiPlayer.NavMeshAgent.SetDestination(targetWaypoint.position);
            aiPlayer.NavMeshAgent.speed = aiPlayer.PatrolSpeed;
            aiPlayer.NavMeshAgent.isStopped = false;
            
            return NodeState.Running;
        }
    }
    
    public class CheckHealth : AIActionNode
    {
        private float healthThreshold = 0.3f;
        
        public CheckHealth(AIPlayer player, float threshold = 0.3f) : base(player)
        {
            healthThreshold = threshold;
        }
        
        public override NodeState Execute()
        {
            var stats = aiPlayer.GetComponent<Player.PlayerStats>();
            
            if (stats == null)
            {
                return NodeState.Failure;
            }
            
            float healthPercent = stats.CurrentHealth / stats.MaxHealth;
            
            return healthPercent <= healthThreshold ? NodeState.Success : NodeState.Failure;
        }
    }
    
    public class CheckDistanceToTarget : AIActionNode
    {
        private float checkDistance;
        private bool useGreaterThan;
        
        public CheckDistanceToTarget(AIPlayer player, float distance, bool greaterThan = true) : base(player)
        {
            checkDistance = distance;
            useGreaterThan = greaterThan;
        }
        
        public override NodeState Execute()
        {
            if (aiPlayer.Target == null)
            {
                return NodeState.Failure;
            }
            
            float distance = Vector3.Distance(aiPlayer.transform.position, aiPlayer.Target.position);
            
            if (useGreaterThan)
            {
                return distance > checkDistance ? NodeState.Success : NodeState.Failure;
            }
            else
            {
                return distance < checkDistance ? NodeState.Success : NodeState.Failure;
            }
        }
    }
    
    public class LookAtTarget : AIActionNode
    {
        private float rotationSpeed = 5f;
        
        public LookAtTarget(AIPlayer player) : base(player)
        {
        }
        
        public override NodeState Execute()
        {
            if (aiPlayer.Target == null)
            {
                return NodeState.Failure;
            }
            
            Vector3 direction = (aiPlayer.Target.position - aiPlayer.transform.position).normalized;
            direction.y = 0;
            
            if (direction.magnitude < 0.01f)
            {
                return NodeState.Success;
            }
            
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            aiPlayer.transform.rotation = Quaternion.Slerp(
                aiPlayer.transform.rotation,
                lookRotation,
                Time.deltaTime * rotationSpeed
            );
            
            return NodeState.Running;
        }
    }
    
    public class Wait : BehaviorNode
    {
        private float waitTime;
        private float timer;
        
        public Wait(float time)
        {
            waitTime = time;
            NodeId = Guid.NewGuid().ToString();
        }
        
        public override NodeState Execute()
        {
            timer += Time.deltaTime;
            
            if (timer >= waitTime)
            {
                timer = 0;
                return NodeState.Success;
            }
            
            return NodeState.Running;
        }
        
        public override void OnEnter()
        {
            timer = 0;
        }
    }
}
