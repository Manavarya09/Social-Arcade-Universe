using System;
using UnityEngine;

namespace SocialArcade.Unity.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _sprintSpeed = 8f;
        [SerializeField] private float _rotationSpeed = 720f;
        [SerializeField] private float _acceleration = 50f;
        [SerializeField] private float _deceleration = 50f;
        [SerializeField] private float _gravity = -20f;
        
        [Header("Jump Settings")]
        [SerializeField] private float _jumpHeight = 2f;
        [SerializeField] private float _jumpCooldown = 0.2f;
        
        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private PlayerAnimationController _animationController;
        [SerializeField] private PlayerStats _playerStats;
        
        [Header("Ground Detection")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float _groundRadius = 0.3f;
        [SerializeField] private LayerMask _groundLayer;
        
        private Vector3 _velocity;
        private Vector2 _input;
        private bool _isGrounded;
        private bool _isSprinting;
        private bool _canJump = true;
        private float _jumpTimer;
        
        public bool IsMoving => _input.magnitude > 0.1f;
        public bool IsSprinting => _isSprinting;
        public bool IsJumping => !_isGrounded && _velocity.y > 0;
        
        public event Action OnJump;
        public event Action OnLand;
        public event Action<float> OnDamage;
        public event Action OnDeath;
        
        private void Awake()
        {
            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }
            
            if (_playerStats == null)
            {
                _playerStats = GetComponent<PlayerStats>();
            }
        }
        
        private void Start()
        {
            if (_animationController == null)
            {
                _animationController = GetComponent<PlayerAnimationController>();
            }
        }
        
        private void Update()
        {
            CheckGround();
            HandleMovement();
            HandleJump();
            ApplyGravity();
            
            _jumpTimer += Time.deltaTime;
            if (_jumpTimer >= _jumpCooldown)
            {
                _canJump = true;
            }
        }
        
        private void CheckGround()
        {
            bool wasGrounded = _isGrounded;
            _isGrounded = Physics.CheckSphere(_groundCheck.position, _groundRadius, _groundLayer);
            
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }
            
            if (!wasGrounded && _isGrounded)
            {
                OnLand?.Invoke();
            }
        }
        
        public void SetInput(Vector2 input, bool sprint = false)
        {
            _input = input;
            _isSprinting = sprint && _input.magnitude > 0.1f;
        }
        
        private void HandleMovement()
        {
            if (!_isGrounded) return;
            
            float targetSpeed = _isSprinting ? _sprintSpeed : _moveSpeed;
            Vector3 targetVelocity = new Vector3(_input.x, 0, _input.y) * targetSpeed;
            
            _velocity.x = Mathf.MoveTowards(_velocity.x, targetVelocity.x, Time.deltaTime * (targetVelocity.magnitude > 0.1f ? _acceleration : _deceleration));
            _velocity.z = Mathf.MoveTowards(_velocity.z, targetVelocity.y, Time.deltaTime * (targetVelocity.magnitude > 0.1f ? _acceleration : _deceleration));
            
            if (_input.magnitude > 0.1f)
            {
                float targetAngle = Mathf.Atan2(_input.x, _input.y) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _currentRotationVelocity, _rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, angle, 0);
            }
            
            _animationController?.SetMovementState(_input.magnitude, _isSprinting);
        }
        
        private float _currentRotationVelocity;
        
        private void HandleJump()
        {
            if (!_canJump) return;
            
            if (Input.GetButtonDown("Jump") && _isGrounded)
            {
                PerformJump();
            }
        }
        
        public void PerformJump()
        {
            if (!_isGrounded || !_canJump) return;
            
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            _canJump = false;
            _jumpTimer = 0;
            
            OnJump?.Invoke();
            _animationController?.SetJump(true);
        }
        
        private void ApplyGravity()
        {
            _velocity.y += _gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }
        
        public void TakeDamage(float damage)
        {
            if (_playerStats != null)
            {
                _playerStats.TakeDamage(damage);
                OnDamage?.Invoke(damage);
                
                if (_playerStats.CurrentHealth <= 0)
                {
                    Die();
                }
            }
        }
        
        public void Heal(float amount)
        {
            _playerStats?.Heal(amount);
        }
        
        public void Die()
        {
            _animationController?.SetDeath(true);
            OnDeath?.Invoke();
            enabled = false;
        }
        
        public void Respawn(Vector3 position)
        {
            transform.position = position;
            _velocity = Vector3.zero;
            
            if (_playerStats != null)
            {
                _playerStats.ResetHealth();
            }
            
            _animationController?.SetDeath(false);
            _animationController?.SetJump(false);
            enabled = true;
        }
        
        public void EnableMovement()
        {
            enabled = true;
        }
        
        public void DisableMovement()
        {
            enabled = false;
            _velocity = Vector3.zero;
        }
        
        private void OnDrawGizmos()
        {
            if (_groundCheck != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(_groundCheck.position, _groundRadius);
            }
        }
    }
}
