using System;
using System.Collections.Generic;
using UnityEngine;

namespace SocialArcade.Unity.AI
{
    public enum NodeState
    {
        Running,
        Success,
        Failure
    }

    public abstract class BehaviorNode
    {
        public string NodeId { get; set; }
        public virtual string Name => GetType().Name;
        
        public abstract NodeState Execute();
        
        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        
        public List<BehaviorNode> Children = new();
        
        public virtual void AddChild(BehaviorNode child)
        {
            Children.Add(child);
        }
    }
    
    public class Sequence : BehaviorNode
    {
        private int _currentChildIndex;
        
        public Sequence()
        {
            NodeId = Guid.NewGuid().ToString();
        }
        
        public override NodeState Execute()
        {
            if (Children.Count == 0)
            {
                return NodeState.Success;
            }
            
            while (_currentChildIndex < Children.Count)
            {
                var child = Children[_currentChildIndex];
                var result = child.Execute();
                
                if (result == NodeState.Failure)
                {
                    _currentChildIndex = 0;
                    return NodeState.Failure;
                }
                
                if (result == NodeState.Running)
                {
                    return NodeState.Running;
                }
                
                _currentChildIndex++;
            }
            
            _currentChildIndex = 0;
            return NodeState.Success;
        }
        
        public override void OnEnter()
        {
            _currentChildIndex = 0;
        }
    }
    
    public class Selector : BehaviorNode
    {
        private int _currentChildIndex;
        
        public Selector()
        {
            NodeId = Guid.NewGuid().ToString();
        }
        
        public override NodeState Execute()
        {
            if (Children.Count == 0)
            {
                return NodeState.Failure;
            }
            
            while (_currentChildIndex < Children.Count)
            {
                var child = Children[_currentChildIndex];
                var result = child.Execute();
                
                if (result == NodeState.Success)
                {
                    _currentChildIndex = 0;
                    return NodeState.Success;
                }
                
                if (result == NodeState.Running)
                {
                    return NodeState.Running;
                }
                
                _currentChildIndex++;
            }
            
            _currentChildIndex = 0;
            return NodeState.Failure;
        }
        
        public override void OnEnter()
        {
            _currentChildIndex = 0;
        }
    }
    
    public class Parallel : BehaviorNode
    {
        public enum FailurePolicy
        {
            RequireOne,
            RequireAll
        }
        
        public enum SuccessPolicy
        {
            RequireOne,
            RequireAll
        }
        
        public FailurePolicy failurePolicy = FailurePolicy.RequireOne;
        public SuccessPolicy successPolicy = SuccessPolicy.RequireAll;
        
        public override NodeState Execute()
        {
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var child in Children)
            {
                var result = child.Execute();
                
                if (result == NodeState.Success)
                {
                    successCount++;
                    if (successPolicy == SuccessPolicy.RequireOne)
                    {
                        return NodeState.Success;
                    }
                }
                
                if (result == NodeState.Failure)
                {
                    failureCount++;
                    if (failurePolicy == FailurePolicy.RequireOne)
                    {
                        return NodeState.Failure;
                    }
                }
            }
            
            if (failurePolicy == FailurePolicy.RequireAll && failureCount == Children.Count)
            {
                return NodeState.Failure;
            }
            
            if (successPolicy == SuccessPolicy.RequireAll && successCount == Children.Count)
            {
                return NodeState.Success;
            }
            
            return NodeState.Running;
        }
    }
    
    public class Inverter : BehaviorNode
    {
        public Inverter(BehaviorNode child)
        {
            AddChild(child);
        }
        
        public override NodeState Execute()
        {
            if (Children.Count == 0) return NodeState.Failure;
            
            var result = Children[0].Execute();
            
            switch (result)
            {
                case NodeState.Success:
                    return NodeState.Failure;
                case NodeState.Failure:
                    return NodeState.Success;
                default:
                    return NodeState.Running;
            }
        }
    }
    
    public class Repeater : BehaviorNode
    {
        public int LoopCount = -1;
        public float LoopDuration = 0;
        
        private int _currentLoop;
        private float _timer;
        
        public Repeater(BehaviorNode child, int loopCount = -1)
        {
            AddChild(child);
            LoopCount = loopCount;
        }
        
        public override NodeState Execute()
        {
            if (LoopCount > 0 && _currentLoop >= LoopCount)
            {
                _currentLoop = 0;
                return NodeState.Success;
            }
            
            if (LoopDuration > 0)
            {
                _timer += Time.deltaTime;
                if (_timer >= LoopDuration)
                {
                    _timer = 0;
                    _currentLoop = 0;
                    return NodeState.Success;
                }
            }
            
            if (Children.Count == 0) return NodeState.Success;
            
            var result = Children[0].Execute();
            
            if (result == NodeState.Success)
            {
                _currentLoop++;
                
                if (LoopCount > 0 && _currentLoop >= LoopCount)
                {
                    _currentLoop = 0;
                    return NodeState.Success;
                }
            }
            
            return NodeState.Running;
        }
        
        public override void OnEnter()
        {
            _currentLoop = 0;
            _timer = 0;
        }
    }
}
