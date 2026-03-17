using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SocialArcade.Unity.UI.Screens
{
    public class HomeScreen : UIScreen
    {
        [Header("Navigation")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _friendsButton;
        [SerializeField] private Button _reelsButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _profileButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _settingsButton;
        
        [Header("Player Info")]
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Text _playerNameText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Slider _xpSlider;
        [SerializeField] private Text _xpText;
        
        [Header("Currency")]
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _gemsText;
        
        [Header("Quick Actions")]
        [SerializeField] private Button _dailyChallengeButton;
        [SerializeField] private Button _eventsButton;
        
        [Header("Feed")]
        [SerializeField] private Transform _feedContainer;
        [SerializeField] private GameObject _feedItemPrefab;
        
        [Header("Animations")]
        [SerializeField] private CanvasGroup _navCanvasGroup;
        
        private List<FeedItemData> _feedItems = new();
        
        protected override void OnShow()
        {
            base.OnShow();
            
            _playButton.onClick.AddListener(OnPlayClick);
            _friendsButton.onClick.AddListener(OnFriendsClick);
            _reelsButton.onClick.AddListener(OnReelsClick);
            _shopButton.onClick.AddListener(OnShopClick);
            _profileButton.onClick.AddListener(OnProfileClick);
            _leaderboardButton.onClick.AddListener(OnLeaderboardClick);
            _settingsButton.onClick.AddListener(OnSettingsClick);
            _dailyChallengeButton.onClick.AddListener(OnDailyChallengeClick);
            _eventsButton.onClick.AddListener(OnEventsClick);
            
            RefreshPlayerInfo();
            LoadFeed();
            AnimateEntrance();
        }
        
        private void AnimateEntrance()
        {
            if (_navCanvasGroup != null)
            {
                _navCanvasGroup.alpha = 0;
                _navCanvasGroup.DOFade(1, 0.4f);
            }
            
            var buttons = new[] { _playButton, _friendsButton, _reelsButton, _shopButton, _profileButton };
            float delay = 0;
            
            foreach (var button in buttons)
            {
                button.transform.DOComplete();
                button.transform.localScale = Vector3.zero;
                button.transform.DOScale(Vector3.one, 0.3f)
                    .SetEase(Ease.OutBack)
                    .SetDelay(delay);
                delay += 0.05f;
            }
        }
        
        private void RefreshPlayerInfo()
        {
            var player = Core.GameManager.Instance.CurrentPlayer;
            
            if (player != null)
            {
                _playerNameText.text = player.PlayerProfile?.DisplayName ?? player.Username;
                _levelText.text = $"Lv.{player.PlayerProfile?.Level ?? 1}";
                
                int currentXP = player.PlayerProfile?.XP ?? 0;
                int maxXP = GetXPForLevel((player.PlayerProfile?.Level ?? 1) + 1);
                int minXP = GetXPForLevel(player.PlayerProfile?.Level ?? 1);
                
                _xpSlider.minValue = minXP;
                _xpSlider.maxValue = maxXP;
                _xpSlider.value = currentXP;
                _xpText.text = $"{currentXP} / {maxXP} XP";
                
                _coinsText.text = FormatNumber(player.Currencies?.Coins ?? 0);
                _gemsText.text = FormatNumber(player.Currencies?.Gems ?? 0);
            }
        }
        
        private void LoadFeed()
        {
            StartCoroutine(LoadFeedRoutine());
        }
        
        private System.Collections.IEnumerator LoadFeedRoutine()
        {
            yield return new WaitForSeconds(0.5f);
            
            for (int i = 0; i < 5; i++)
            {
                AddFeedItem(new FeedItemData
                {
                    username = $"Player{i + 1}",
                    action = "just won",
                    gameMode = "Battle Royale",
                    timeAgo = $"{i + 1}m ago"
                });
            }
        }
        
        private void AddFeedItem(FeedItemData data)
        {
            if (_feedItemPrefab != null && _feedContainer != null)
            {
                var item = Instantiate(_feedItemPrefab, _feedContainer);
                var script = item.GetComponent<FeedItemComponent>();
                if (script != null)
                {
                    script.Initialize(data);
                }
            }
        }
        
        private int GetXPForLevel(int level)
        {
            return level * 100 + (level - 1) * 50;
        }
        
        private string FormatNumber(int number)
        {
            if (number >= 1000000)
                return (number / 1000000f).ToString("F1") + "M";
            if (number >= 1000)
                return (number / 1000f).ToString("F1") + "K";
            return number.ToString();
        }
        
        private void OnPlayClick()
        {
            _playButton.transform.DOComplete();
            _playButton.transform.DOScale(0.9f, 0.1f)
                .OnComplete(() => _playButton.transform.DOScale(Vector3.one, 0.1f));
            
            UIManager.Instance.ShowScreen("GameModeSelect");
        }
        
        private void OnFriendsClick()
        {
            UIManager.Instance.ShowScreen("Friends");
        }
        
        private void OnReelsClick()
        {
            UIManager.Instance.ShowScreen("Reels");
        }
        
        private void OnShopClick()
        {
            UIManager.Instance.ShowScreen("Shop");
        }
        
        private void OnProfileClick()
        {
            UIManager.Instance.ShowScreen("Profile");
        }
        
        private void OnLeaderboardClick()
        {
            UIManager.Instance.ShowScreen("Leaderboard");
        }
        
        private void OnSettingsClick()
        {
            UIManager.Instance.ShowScreen("Settings");
        }
        
        private void OnDailyChallengeClick()
        {
            UIManager.Instance.ShowScreen("Challenges");
        }
        
        private void OnEventsClick()
        {
            UIManager.Instance.ShowScreen("Events");
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            
            _playButton.onClick.RemoveAllListeners();
            _friendsButton.onClick.RemoveAllListeners();
            _reelsButton.onClick.RemoveAllListeners();
            _shopButton.onClick.RemoveAllListeners();
            _profileButton.onClick.RemoveAllListeners();
            _leaderboardButton.onClick.RemoveAllListeners();
            _settingsButton.onClick.RemoveAllListeners();
            _dailyChallengeButton.onClick.RemoveAllListeners();
            _eventsButton.onClick.RemoveAllListeners();
        }
    }
    
    [Serializable]
    public class FeedItemData
    {
        public string username;
        public string action;
        public string gameMode;
        public string timeAgo;
    }
    
    public class FeedItemComponent : MonoBehaviour
    {
        [SerializeField] private Text _contentText;
        
        public void Initialize(FeedItemData data)
        {
            _contentText.text = $"<b>{data.username}</b> {data.action} in {data.gameMode} · {data.timeAgo}";
        }
    }
}
