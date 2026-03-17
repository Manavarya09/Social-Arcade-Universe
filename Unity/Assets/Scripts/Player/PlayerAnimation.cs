using System;
using UnityEngine;

namespace SocialArcade.Unity.Player
{
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("Animation Parameters")]
        [SerializeField] private string _movementBlendParam = "Movement";
        [SerializeField] private string _sprintBlendParam = "Sprint";
        [SerializeField] private string _jumpTriggerParam = "Jump";
        [SerializeField] private string _landTriggerParam = "Land";
        [SerializeField] private string _deathTriggerParam = "Death";
        [SerializeField] private string _attackTriggerParam = "Attack";
        [SerializeField] private string _hitTriggerParam = "Hit";
        
        [Header("Animation Settings")]
        [SerializeField] private float _blendSpeed = 5f;
        
        private Animator _animator;
        private bool _isInitialized;
        
        private float _currentMovement;
        private float _targetMovement;
        private float _currentSprint;
        private float _targetSprint;
        
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
            
            if (_animator != null)
            {
                _isInitialized = true;
            }
        }
        
        private void Update()
        {
            if (!_isInitialized) return;
            
            _currentMovement = Mathf.Lerp(_currentMovement, _targetMovement, Time.deltaTime * _blendSpeed);
            _currentSprint = Mathf.Lerp(_currentSprint, _targetSprint, Time.deltaTime * _blendSpeed);
            
            _animator.SetFloat(_movementBlendParam, _currentMovement);
            _animator.SetFloat(_sprintBlendParam, _currentSprint);
        }
        
        public void SetMovementState(float magnitude, bool isSprinting)
        {
            _targetMovement = magnitude;
            _targetSprint = isSprinting ? 1f : 0f;
        }
        
        public void SetJump(bool isJumping)
        {
            if (!_isInitialized) return;
            _animator.SetTrigger(isJumping ? _jumpTriggerParam : _landTriggerParam);
        }
        
        public void SetDeath(bool isDead)
        {
            if (!_isInitialized) return;
            _animator.SetTrigger(_deathTriggerParam);
        }
        
        public void SetAttack()
        {
            if (!_isInitialized) return;
            _animator.SetTrigger(_attackTriggerParam);
        }
        
        public void SetHit()
        {
            if (!_isInitialized) return;
            _animator.SetTrigger(_hitTriggerParam);
        }
        
        public void PlayAnimation(string stateName, float crossFade = 0.2f)
        {
            if (!_isInitialized) return;
            _animator.CrossFade(stateName, crossFade);
        }
        
        public void SetLayerWeight(int layerIndex, float weight)
        {
            if (!_isInitialized) return;
            _animator.SetLayerWeight(layerIndex, weight);
        }
        
        public AnimatorStateInfo GetCurrentState(int layerIndex = 0)
        {
            if (!_isInitialized) return default;
            return _animator.GetCurrentAnimatorStateInfo(layerIndex);
        }
        
        public bool IsInState(string stateName, int layerIndex = 0)
        {
            if (!_isInitialized) return false;
            return _animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName);
        }
    }
}
