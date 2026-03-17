using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SocialArcade.Unity.UI.Screens
{
    public class GameHUD : UIScreen
    {
        [Header("Player Stats")]
        [SerializeField] private Image _healthBar;
        [SerializeField] private Text _healthText;
        [SerializeField] private Image _staminaBar;
        [SerializeField] private Text _ammoText;
        
        [Header("Minimap")]
        [SerializeField] private RawImage _minimapImage;
        [SerializeField] private RectTransform _playerIndicator;
        
        [Header("Score/Kills")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _killsText;
        [SerializeField] private Text _rankText;
        
        [Header("Timer")]
        [SerializeField] private Text _gameTimerText;
        
        [Header("Player List")]
        [SerializeField] private Transform _playerListContainer;
        [SerializeField] private GameObject _playerListItemPrefab;
        
        [Header("Actions")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _emoteButton;
        [SerializeField] private Button _inventoryButton;
        [SerializeField] private Button _mapButton;
        
        [Header("Kill Feed")]
        [SerializeField] private Transform _killFeedContainer;
        [SerializeField] private GameObject _killFeedItemPrefab;
        
        [Header("Damage Indicator")]
        [SerializeField] private Image _damageIndicator;
        
        private float _gameTime;
        private bool _isPaused;
        
        protected override void OnShow()
        {
            base.OnShow();
            
            _pauseButton.onClick.AddListener(OnPauseClick);
            _emoteButton.onClick.AddListener(OnEmoteClick);
            _inventoryButton.onClick.AddListener(OnInventoryClick);
            _mapButton.onClick.AddListener(OnMapClick);
            
            ResetHUD();
            AnimateEntrance();
        }
        
        private void ResetHUD()
        {
            _healthBar.fillAmount = 1f;
            _healthText.text = "100/100";
            _staminaBar.fillAmount = 1f;
            _scoreText.text = "0";
            _killsText.text = "0";
            _rankText.text = "#1";
            _gameTime = 0;
            _isPaused = false;
        }
        
        private void AnimateEntrance()
        {
            if (_damageIndicator != null)
            {
                _damageIndicator.color = new Color(1, 0, 0, 0);
            }
            
            var animElements = new[] { _healthBar.transform, _scoreText.transform, _gameTimerText.transform };
            
            foreach (var element in animElements)
            {
                element?.DOScale(0.8f, 0.2f).SetEase(Ease.OutBack).OnComplete(
                    () => element?.DOScale(1f, 0.2f));
            }
        }
        
        private void Update()
        {
            if (_isPaused) return;
            
            _gameTime += Time.deltaTime;
            UpdateTimer();
        }
        
        private void UpdateTimer()
        {
            int minutes = Mathf.FloorToInt(_gameTime / 60);
            int seconds = Mathf.FloorToInt(_gameTime % 60);
            _gameTimerText.text = $"{minutes:00}:{seconds:00}";
        }
        
        public void UpdateHealth(float current, float max)
        {
            float percent = current / max;
            _healthBar.DOFillAmount(percent, 0.2f);
            _healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            
            if (percent < 0.3f)
            {
                _healthBar.DOColor(Color.red, 0.2f);
            }
            else
            {
                _healthBar.DOColor(Color.green, 0.2f);
            }
        }
        
        public void UpdateStamina(float current, float max)
        {
            float percent = current / max;
            _staminaBar.DOFillAmount(percent, 0.1f);
        }
        
        public void UpdateScore(int score)
        {
            _scoreText.text = score.ToString();
            _scoreText.transform.DOComplete();
            _scoreText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
        }
        
        public void UpdateKills(int kills)
        {
            _killsText.text = kills.ToString();
            _killsText.transform.DOComplete();
            _killsText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
        }
        
        public void UpdateRank(int rank, int total)
        {
            _rankText.text = $"#{rank}/{total}";
        }
        
        public void ShowDamageIndicator(Vector3 fromDirection)
        {
            if (_damageIndicator == null) return;
            
            float angle = Vector3.SignedAngle(fromDirection, Camera.main.transform.forward, Vector3.up);
            _damageIndicator.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            
            _damageIndicator.DOFade(0.5f, 0.1f).OnComplete(
                () => _damageIndicator.DOFade(0, 0.3f));
        }
        
        public void AddKillFeedItem(string killer, string victim, string weapon)
        {
            if (_killFeedItemPrefab != null && _killFeedContainer != null)
            {
                var item = Instantiate(_killFeedItemPrefab, _killFeedContainer);
                var text = item.GetComponent<Text>();
                if (text != null)
                {
                    text.text = $"<color=red>{killer}</color> eliminated <color=blue>{victim}</color>";
                    
                    (item as GameObject).transform.SetAsFirstSibling();
                    
                    Destroy(item, 3f);
                }
            }
        }
        
        public void UpdatePlayerList(List<PlayerListData> players)
        {
            foreach (Transform child in _playerListContainer)
            {
                Destroy(child.gameObject);
            }
            
            foreach (var player in players)
            {
                var item = Instantiate(_playerListItemPrefab, _playerListContainer);
                var script = item.GetComponent<PlayerListItem>();
                if (script != null)
                {
                    script.Initialize(player);
                }
            }
        }
        
        private void OnPauseClick()
        {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0 : 1;
            
            if (_isPaused)
            {
                UIManager.Instance.ShowScreen("PauseMenu");
            }
            else
            {
                UIManager.Instance.GoBack();
            }
        }
        
        private void OnEmoteClick()
        {
            UIManager.Instance.ShowScreen("EmoteWheel");
        }
        
        private void OnInventoryClick()
        {
            UIManager.Instance.ShowScreen("Inventory");
        }
        
        private void OnMapClick()
        {
            UIManager.Instance.ShowScreen("FullMap");
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            
            _pauseButton.onClick.RemoveAllListeners();
            _emoteButton.onClick.RemoveAllListeners();
            _inventoryButton.onClick.RemoveAllListeners();
            _mapButton.onClick.RemoveAllListeners();
            
            Time.timeScale = 1;
        }
    }
    
    [Serializable]
    public class PlayerListData
    {
        public string odId;
        public string username;
        public int health;
        public bool isAlive;
        public bool isLocal;
    }
    
    public class PlayerListItem : MonoBehaviour
    {
        [SerializeField] private Text _nameText;
        [SerializeField] private Image _healthBar;
        [SerializeField] private Image _avatarImage;
        
        public void Initialize(PlayerListData data)
        {
            _nameText.text = data.username;
            _healthBar.fillAmount = data.health / 100f;
            
            if (!data.isAlive)
            {
                _nameText.color = Color.gray;
                _healthBar.fillAmount = 0;
            }
            
            if (data.isLocal)
            {
                _nameText.fontStyle = FontStyle.Bold;
            }
        }
    }
}
