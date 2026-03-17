using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SocialArcade.Unity.UI.Screens
{
    public class LeaderboardScreen : UIScreen
    {
        [Header("Tabs")]
        [SerializeField] private Button _globalTab;
        [SerializeField] private Button _friendsTab;
        [SerializeField] private Button _seasonTab;
        
        [Header("Current Tab Indicator")]
        [SerializeField] private RectTransform _tabIndicator;
        
        [Header("Leaderboard List")]
        [SerializeField] private Transform _leaderboardContainer;
        [SerializeField] private GameObject _leaderboardItemPrefab;
        
        [Header("User Rank")]
        [SerializeField] private GameObject _userRankPanel;
        [SerializeField] private Text _userRankText;
        [SerializeField] private Text _userNameText;
        [SerializeField] private Text _userScoreText;
        
        [Header("Game Mode Selector")]
        [SerializeField] private Dropdown _gameModeDropdown;
        
        [Header("Loading")]
        [SerializeField] private GameObject _loadingIndicator;
        
        private string _currentTab = "global";
        private string _currentGameMode = "ranked";
        private List<LeaderboardEntry> _entries = new();
        
        protected override void OnShow()
        {
            base.OnShow();
            
            _globalTab.onClick.AddListener(() => OnTabClick("global"));
            _friendsTab.onClick.AddListener(() => OnTabClick("friends"));
            _seasonTab.onClick.AddListener(() => OnTabClick("season"));
            
            _gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);
            
            _gameModeDropdown.ClearOptions();
            _gameModeDropdown.AddOptions(new List<string> { "Ranked", "Battle Royale", "Deathmatch" });
            
            LoadLeaderboard();
            AnimateEntrance();
        }
        
        private void AnimateEntrance()
        {
            _leaderboardContainer.DOScaleX(0, 0.3f).From();
        }
        
        private void OnTabClick(string tab)
        {
            _currentTab = tab;
            
            _tabIndicator.DOKill();
            RectTransform targetTab = tab switch
            {
                "global" => _globalTab.GetComponent<RectTransform>(),
                "friends" => _friendsTab.GetComponent<RectTransform>(),
                "season" => _seasonTab.GetComponent<RectTransform>(),
                _ => _globalTab.GetComponent<RectTransform>()
            };
            
            _tabIndicator.anchoredPosition = targetTab.anchoredPosition;
            
            LoadLeaderboard();
        }
        
        private void OnGameModeChanged(int index)
        {
            _currentGameMode = index switch
            {
                0 => "ranked",
                1 => "battle_royale",
                2 => "deathmatch",
                _ => "ranked"
            };
            
            LoadLeaderboard();
        }
        
        private async void LoadLeaderboard()
        {
            _loadingIndicator.SetActive(true);
            
            ClearLeaderboard();
            
            try
            {
                var response = await Networking.NetworkManager.Instance.GetLeaderboardAsync(_currentGameMode);
                
                if (response.success)
                {
                    var mockData = GenerateMockData();
                    
                    foreach (var entry in mockData)
                    {
                        AddLeaderboardEntry(entry);
                    }
                    
                    UpdateUserRank();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load leaderboard: {ex.Message}");
            }
            finally
            {
                _loadingIndicator.SetActive(false);
            }
        }
        
        private List<LeaderboardEntry> GenerateMockData()
        {
            var data = new List<LeaderboardEntry>();
            
            string[] names = { "ProGamer", "NoScopeKing", "VictoryRoy", "ElitePlayer", 
                             "ShadowStrike", "NeonBlade", "CyberNinja", "StarHunter",
                             "LunarWolf", "ThunderBolt" };
            
            for (int i = 0; i < names.Length; i++)
            {
                data.Add(new LeaderboardEntry
                {
                    rank = i + 1,
                    username = names[i],
                    score = (10 - i) * 500,
                    isCurrentUser = i == 4
                });
            }
            
            return data;
        }
        
        private void ClearLeaderboard()
        {
            foreach (Transform child in _leaderboardContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        private void AddLeaderboardEntry(LeaderboardEntry entry)
        {
            var item = Instantiate(_leaderboardItemPrefab, _leaderboardContainer);
            var script = item.GetComponent<LeaderboardItem>();
            
            if (script != null)
            {
                script.Initialize(entry);
            }
        }
        
        private void UpdateUserRank()
        {
            _userRankPanel.SetActive(true);
            _userRankText.text = "#42";
            _userNameText.text = Core.GameManager.Instance.CurrentPlayer?.Username ?? "You";
            _userScoreText.text = "2,500";
            
            _userRankPanel.transform.DOScale(Vector3.one * 0.9f, 0.2f).From();
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            
            _globalTab.onClick.RemoveAllListeners();
            _friendsTab.onClick.RemoveAllListeners();
            _seasonTab.onClick.RemoveAllListeners();
            _gameModeDropdown.onValueChanged.RemoveAllListeners();
        }
    }
    
    public class LeaderboardEntry
    {
        public int rank;
        public string username;
        public int score;
        public bool isCurrentUser;
    }
    
    public class LeaderboardItem : MonoBehaviour
    {
        [SerializeField] private Text _rankText;
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Image _rankBadge;
        
        public void Initialize(LeaderboardEntry entry)
        {
            _rankText.text = entry.rank.ToString();
            _nameText.text = entry.username;
            _scoreText.text = entry.score.ToString("N0");
            
            if (entry.isCurrentUser)
            {
                GetComponent<Image>().color = new Color(0.424f, 0.361f, 0.906f, 0.2f);
            }
            
            if (entry.rank <= 3)
            {
                _rankBadge.gameObject.SetActive(true);
                _rankBadge.color = entry.rank switch
                {
                    1 => new Color(1f, 0.843f, 0f),
                    2 => new Color(0.753f, 0.753f, 0.753f),
                    3 => new Color(0.804f, 0.498f, 0.196f),
                    _ => Color.white
                };
            }
        }
    }
}
