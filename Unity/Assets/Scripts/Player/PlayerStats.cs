using System;
using UnityEngine;

namespace SocialArcade.Unity.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth;
        
        [Header("Stamina Settings")]
        [SerializeField] private float _maxStamina = 100f;
        [SerializeField] private float _currentStamina;
        [SerializeField] private float _staminaRegenRate = 10f;
        [SerializeField] private float _staminaDrainRate = 20f;
        
        [Header("Combat Settings")]
        [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _attackSpeed = 1f;
        [SerializeField] private float _defense = 0f;
        
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public float HealthPercentage => _currentHealth / _maxHealth;
        
        public float MaxStamina => _maxStamina;
        public float CurrentStamina => _currentStamina;
        public float StaminaPercentage => _currentStamina / _maxStamina;
        
        public float AttackDamage => _attackDamage;
        public float AttackSpeed => _attackSpeed;
        public float Defense => _defense;
        
        public bool IsDead => _currentHealth <= 0;
        
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action OnDeath;
        
        private void Awake()
        {
            _currentHealth = _maxHealth;
            _currentStamina = _maxStamina;
        }
        
        private void Update()
        {
            RegenerateStamina();
        }
        
        public void TakeDamage(float damage)
        {
            float actualDamage = Mathf.Max(1, damage - _defense);
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);
            
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            
            if (_currentHealth <= 0)
            {
                OnDeath?.Invoke();
            }
        }
        
        public void Heal(float amount)
        {
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
        
        public void ResetHealth()
        {
            _currentHealth = _maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
        
        public bool UseStamina(float amount)
        {
            if (_currentStamina >= amount)
            {
                _currentStamina -= amount;
                OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
                return true;
            }
            return false;
        }
        
        private void RegenerateStamina()
        {
            if (_currentStamina < _maxStamina)
            {
                _currentStamina = Mathf.Min(_maxStamina, _currentStamina + _staminaRegenRate * Time.deltaTime);
                OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
            }
        }
        
        public void SetMaxHealth(float value)
        {
            _maxHealth = value;
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
        
        public void SetAttackDamage(float value)
        {
            _attackDamage = value;
        }
        
        public void SetDefense(float value)
        {
            _defense = value;
        }
        
        public void SetStaminaRegenRate(float value)
        {
            _staminaRegenRate = value;
        }
        
        public void AddBuff(StatBuff buff, float duration)
        {
            StartCoroutine(ApplyBuff(buff, duration));
        }
        
        private System.Collections.IEnumerator ApplyBuff(StatBuff buff, float duration)
        {
            float originalDamage = _attackDamage;
            float originalDefense = _defense;
            
            _attackDamage += buff.damageBonus;
            _defense += buff.defenseBonus;
            
            yield return new WaitForSeconds(duration);
            
            _attackDamage = originalDamage;
            _defense = originalDefense;
        }
    }
    
    [Serializable]
    public class StatBuff
    {
        public float damageBonus;
        public float defenseBonus;
        public float healthBonus;
        public float speedBonus;
    }
}
