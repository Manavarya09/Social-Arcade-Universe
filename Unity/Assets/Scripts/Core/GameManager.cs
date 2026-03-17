using System;
using System.Collections.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SocialArcade.Unity.Core
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance => _instance;
        
        [Header("Configuration")]
        [SerializeField] private string _serverUrl = "http://localhost:3000";
        [SerializeField] private bool _autoConnect = true;
        
        [Header("Game State")]
        [SerializeField] private GameState _currentState = GameState.MainMenu;
        
        private PlayerData _currentPlayer;
        private bool _isInitialized;
        
        public string ServerUrl => _serverUrl;
        public GameState CurrentState => _currentState;
        public PlayerData CurrentPlayer => _currentPlayer;
        public bool IsLoggedIn => _currentPlayer != null;
        
        public event Action<GameState> OnStateChanged;
        public event Action<PlayerData> OnPlayerDataLoaded;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private async void Start()
        {
            if (_autoConnect)
            {
                await InitializeAsync();
            }
        }
        
        public async Task InitializeAsync()
        {
            try
            {
                GameEvents.OnLoadingStart.Invoke();
                
                NetworkManager.Instance.Initialize(_serverUrl);
                
                if (PlayerPrefs.HasKey("auth_token"))
                {
                    var token = PlayerPrefs.GetString("auth_token");
                    await AuthenticateWithTokenAsync(token);
                }
                
                _isInitialized = true;
                Debug.Log("GameManager initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize: {ex.Message}");
                GameEvents.OnError.Invoke($"Failed to initialize: {ex.Message}");
            }
            finally
            {
                GameEvents.OnLoadingEnd.Invoke();
            }
        }
        
        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                GameEvents.OnLoadingStart.Invoke();
                
                var response = await NetworkManager.Instance.LoginAsync(email, password);
                
                if (response.success)
                {
                    _currentPlayer = new PlayerData(response.data.user);
                    PlayerPrefs.SetString("auth_token", response.data.accessToken);
                    
                    OnPlayerDataLoaded?.Invoke(_currentPlayer);
                    GameEvents.OnCurrencyUpdated.Invoke(_currentPlayer.Currencies);
                    
                    SetState(GameState.Home);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Login failed: {ex.Message}");
                GameEvents.OnError.Invoke("Login failed. Please check your credentials.");
                return false;
            }
            finally
            {
                GameEvents.OnLoadingEnd.Invoke();
            }
        }
        
        public async Task<bool> RegisterAsync(string email, string username, string password)
        {
            try
            {
                GameEvents.OnLoadingStart.Invoke();
                
                var response = await NetworkManager.Instance.RegisterAsync(email, username, password);
                
                if (response.success)
                {
                    _currentPlayer = new PlayerData(response.data.user);
                    PlayerPrefs.SetString("auth_token", response.data.accessToken);
                    
                    OnPlayerDataLoaded?.Invoke(_currentPlayer);
                    
                    SetState(GameState.Home);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Registration failed: {ex.Message}");
                GameEvents.OnError.Invoke("Registration failed. Please try again.");
                return false;
            }
            finally
            {
                GameEvents.OnLoadingEnd.Invoke();
            }
        }
        
        private async Task<bool> AuthenticateWithTokenAsync(string token)
        {
            try
            {
                var response = await NetworkManager.Instance.GetProfileAsync(token);
                
                if (response.success)
                {
                    _currentPlayer = new PlayerData(response.data);
                    OnPlayerDataLoaded?.Invoke(_currentPlayer);
                    SetState(GameState.Home);
                    return true;
                }
                
                PlayerPrefs.DeleteKey("auth_token");
                return false;
            }
            catch
            {
                PlayerPrefs.DeleteKey("auth_token");
                return false;
            }
        }
        
        public async void Logout()
        {
            await NetworkManager.Instance.LogoutAsync();
            PlayerPrefs.DeleteKey("auth_token");
            _currentPlayer = null;
            NetworkManager.Instance.Disconnect();
            SetState(GameState.MainMenu);
            SceneManager.LoadScene("Login");
        }
        
        public void SetState(GameState newState)
        {
            if (_currentState == newState) return;
            
            _currentState = newState;
            OnStateChanged?.Invoke(newState);
            
            Debug.Log($"Game state changed to: {newState}");
        }
        
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
        
        public async Task RefreshPlayerDataAsync()
        {
            if (_currentPlayer == null) return;
            
            var response = await NetworkManager.Instance.GetProfileAsync();
            if (response.success)
            {
                _currentPlayer = new PlayerData(response.data);
                OnPlayerDataLoaded?.Invoke(_currentPlayer);
            }
        }
    }
    
    public enum GameState
    {
        Loading,
        MainMenu,
        Login,
        Register,
        Home,
        Lobby,
        Game,
        Shop,
        Profile,
        Friends,
        Reels,
        Leaderboard,
    }
    
    [Serializable]
    public class PlayerData
    {
        public string Id;
        public string Email;
        public string Username;
        public string AvatarUrl;
        public string Bio;
        public DateTime CreatedAt;
        public PlayerProfileData PlayerProfile;
        public CurrencyData Currencies;
        
        public PlayerData(dynamic data)
        {
            Id = data.id;
            Email = data.email;
            Username = data.username;
            AvatarUrl = data.avatarUrl;
            Bio = data.bio;
            CreatedAt = DateTime.Parse(data.createdAt.ToString());
            
            if (data.playerProfile != null)
            {
                PlayerProfile = new PlayerProfileData(data.playerProfile);
            }
            
            if (data.currencies != null)
            {
                Currencies = new CurrencyData(data.currencies);
            }
        }
    }
    
    [Serializable]
    public class PlayerProfileData
    {
        public string DisplayName;
        public int Level;
        public int XP;
        public int Wins;
        public int Losses;
        public int GamesPlayed;
        public int TotalPlaytime;
        
        public PlayerProfileData(dynamic data)
        {
            DisplayName = data.displayName;
            Level = data.level;
            XP = data.xp;
            Wins = data.wins;
            Losses = data.losses;
            GamesPlayed = data.gamesPlayed;
            TotalPlaytime = data.totalPlaytime;
        }
    }
    
    [Serializable]
    public class CurrencyData
    {
        public int Coins;
        public int Gems;
        public int PremiumGems;
        
        public CurrencyData(dynamic data)
        {
            Coins = data.coins;
            Gems = data.gems;
            PremiumGems = data.premiumGems;
        }
    }
}
