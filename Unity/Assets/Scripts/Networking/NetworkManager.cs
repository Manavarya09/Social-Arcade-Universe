using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SocketIOClient;
using SocketIOClient.Newtonsoft.JsonSerializer;

namespace SocialArcade.Unity.Networking
{
    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager _instance;
        public static NetworkManager Instance => _instance;
        
        private SocketIO _socket;
        private string _serverUrl;
        private string _authToken;
        private bool _isConnected;
        
        public bool IsConnected => _isConnected;
        public bool IsSocketConnected => _socket?.Connected ?? false;
        public string AuthToken => _authToken;
        
        private readonly Dictionary<string, Action<SocketIOResponse>> _eventHandlers = new();
        private readonly Dictionary<string, List<Action<SocketIOResponse>>> _socketListeners = new();
        
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
        
        public void Initialize(string serverUrl)
        {
            _serverUrl = serverUrl;
            
            _socket = new SocketIO(serverUrl, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", _authToken }
                },
                Reconnection = true,
                ReconnectionAttempts = 5,
                ReconnectionDelay = 1000,
            });
            
            _socket.OnConnected += OnSocketConnected;
            _socket.OnDisconnected += OnSocketDisconnected;
            _socket.OnReconnectAttempt += OnReconnectAttempt;
            _socket.OnReconnectFailed += OnReconnectFailed;
            
            RegisterDefaultSocketHandlers();
        }
        
        public async Task ConnectSocketAsync()
        {
            if (_socket == null)
            {
                Debug.LogError("Socket not initialized. Call Initialize first.");
                return;
            }
            
            try
            {
                await _socket.ConnectAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Socket connection failed: {ex.Message}");
            }
        }
        
        public void Disconnect()
        {
            _socket?.DisconnectAsync();
            _isConnected = false;
        }
        
        private void OnSocketConnected(object sender, EventArgs e)
        {
            _isConnected = true;
            Debug.Log("Socket connected");
            Core.GameEvents.OnPlayerConnected.Invoke();
        }
        
        private void OnSocketDisconnected(object sender, string e)
        {
            _isConnected = false;
            Debug.Log("Socket disconnected");
            Core.GameEvents.OnPlayerDisconnected.Invoke();
        }
        
        private void OnReconnectAttempt(object sender, int e)
        {
            Debug.Log($"Reconnect attempt {e}");
        }
        
        private void OnReconnectFailed(object sender, EventArgs e)
        {
            Debug.LogError("Reconnection failed");
        }
        
        private void RegisterDefaultSocketHandlers()
        {
            On("match_found", response =>
            {
                Core.GameEvents.OnMatchFound.Invoke(response);
            });
            
            On("matchmaking_queued", response =>
            {
                Debug.Log("In matchmaking queue");
            });
            
            On("lobby_updated", response =>
            {
                Core.GameEvents.OnLobbyUpdated.Invoke(response);
            });
            
            On("game_started", response =>
            {
                Core.GameEvents.OnGameStart.Invoke(response);
            });
            
            On("game_ended", response =>
            {
                Core.GameEvents.OnGameEnd.Invoke(response);
            });
            
            On("position_sync", response =>
            {
                NetworkEvents.OnPositionSync.Invoke(response);
            });
            
            On("chat_message", response =>
            {
                Core.GameEvents.OnChatMessage.Invoke(response);
            });
        }
        
        public void On(string eventName, Action<SocketIOResponse> handler)
        {
            if (!_socketListeners.ContainsKey(eventName))
            {
                _socketListeners[eventName] = new List<Action<SocketIOResponse>>();
                _socket.On(eventName, response =>
                {
                    if (_socketListeners.TryGetValue(eventName, out var handlers))
                    {
                        foreach (var h in handlers)
                        {
                            h?.Invoke(response);
                        }
                    }
                });
            }
            
            _socketListeners[eventName].Add(handler);
        }
        
        public void Off(string eventName, Action<SocketIOResponse> handler)
        {
            if (_socketListeners.TryGetValue(eventName, out var handlers))
            {
                handlers.Remove(handler);
            }
        }
        
        public async Task EmitAsync(string eventName, object data)
        {
            if (_socket == null || !_isConnected)
            {
                Debug.LogWarning("Socket not connected");
                return;
            }
            
            try
            {
                await _socket.EmitAsync(eventName, data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Emit failed: {ex.Message}");
            }
        }
        
        public async void JoinMatchmaking(string gameMode, int rating = 1000)
        {
            await EmitAsync("join_matchmaking", new
            {
                gameMode,
                rating
            });
        }
        
        public async void LeaveMatchmaking()
        {
            await EmitAsync("leave_matchmaking", new { });
        }
        
        public async void JoinLobby(string roomId)
        {
            await EmitAsync("join_lobby", new { roomId });
        }
        
        public async void LeaveLobby(string roomId)
        {
            await EmitAsync("leave_lobby", new { roomId });
        }
        
        public async void SetPlayerReady(string roomId, bool ready)
        {
            await EmitAsync("player_ready", new { roomId, ready });
        }
        
        public async void SendPositionUpdate(string roomId, Vector3 position, Quaternion rotation)
        {
            await EmitAsync("position_update", new
            {
                roomId,
                position = new { x = position.x, y = position.y, z = position.z },
                rotation = new { x = rotation.x, y = rotation.y, z = rotation.z, w = rotation.w }
            });
        }
        
        public async void SendPlayerAction(string roomId, string action, object data)
        {
            await EmitAsync("player_action", new
            {
                roomId,
                action,
                data
            });
        }
        
        public async void SendChatMessage(string roomId, string message)
        {
            await EmitAsync("chat_message", new { roomId, message });
        }
        
        // HTTP API Methods
        public async Task<ApiResponse> LoginAsync(string email, string password)
        {
            return await PostAsync("/api/auth/login", new { email, password });
        }
        
        public async Task<ApiResponse> RegisterAsync(string email, string username, string password)
        {
            return await PostAsync("/api/auth/register", new { email, username, password });
        }
        
        public async Task<ApiResponse> LogoutAsync()
        {
            return await PostAsync("/api/auth/logout", new { });
        }
        
        public async Task<ApiResponse> GetProfileAsync(string token = null)
        {
            var authToken = token ?? _authToken;
            return await GetAsync("/api/users/me", authToken);
        }
        
        public async Task<ApiResponse> UpdateProfileAsync(object data)
        {
            return await PutAsync("/api/users/me", data);
        }
        
        public async Task<ApiResponse> GetCurrencyAsync()
        {
            return await GetAsync("/api/economy/balance");
        }
        
        public async Task<ApiResponse> GetInventoryAsync()
        {
            return await GetAsync("/api/player/inventory");
        }
        
        public async Task<ApiResponse> GetShopItemsAsync(string type = null)
        {
            var endpoint = "/api/shop/items";
            if (!string.IsNullOrEmpty(type))
            {
                endpoint += $"?type={type}";
            }
            return await GetAsync(endpoint);
        }
        
        public async Task<ApiResponse> PurchaseItemAsync(string itemId)
        {
            return await PostAsync("/api/shop/purchase", new { itemId });
        }
        
        public async Task<ApiResponse> GetFriendsAsync()
        {
            return await GetAsync("/api/social/friends");
        }
        
        public async Task<ApiResponse> GetReelsAsync(int page = 1, int limit = 20)
        {
            return await GetAsync($"/api/reels?page={page}&limit={limit}");
        }
        
        public async Task<ApiResponse> GetLeaderboardAsync(string gameMode = "ranked")
        {
            return await GetAsync($"/api/leaderboard/global?gameMode={gameMode}");
        }
        
        public async Task<ApiResponse> GetGameModesAsync()
        {
            return await GetAsync("/api/games/modes");
        }
        
        private async Task<ApiResponse> GetAsync(string endpoint, string token = null)
        {
            var url = _serverUrl + endpoint;
            
            using var request = new UnityEngine.Networking.UnityWebRequest(url, "GET");
            request.SetRequestHeader("Content-Type", "application/json");
            
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            }
            else if (!string.IsNullOrEmpty(_authToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
            }
            
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            
            var operation = request.SendWebRequest();
            
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            
            return ParseResponse(request);
        }
        
        private async Task<ApiResponse> PostAsync(string endpoint, object data)
        {
            var url = _serverUrl + endpoint;
            var json = JsonUtility.ToJson(data);
            
            using var request = new UnityEngine.Networking.UnityWebRequest(url, "POST");
            request.SetRequestHeader("Content-Type", "application/json");
            
            if (!string.IsNullOrEmpty(_authToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
            }
            
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            
            var operation = request.SendWebRequest();
            
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            
            return ParseResponse(request);
        }
        
        private async Task<ApiResponse> PutAsync(string endpoint, object data)
        {
            var url = _serverUrl + endpoint;
            var json = JsonUtility.ToJson(data);
            
            using var request = new UnityEngine.Networking.UnityWebRequest(url, "PUT");
            request.SetRequestHeader("Content-Type", "application/json");
            
            if (!string.IsNullOrEmpty(_authToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
            }
            
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            
            var operation = request.SendWebRequest();
            
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            
            return ParseResponse(request);
        }
        
        private ApiResponse ParseResponse(UnityEngine.Networking.UnityWebRequest request)
        {
            var response = new ApiResponse();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                response.success = true;
                response.rawJson = request.downloadHandler.text;
                
                try
                {
                    response.data = JsonUtility.FromJson<ApiResponseData>(request.downloadHandler.text);
                }
                catch
                {
                    Debug.LogError($"Failed to parse response: {request.downloadHandler.text}");
                }
            }
            else
            {
                response.success = false;
                response.error = new ApiError
                {
                    message = request.error
                };
            }
            
            return response;
        }
        
        public void SetAuthToken(string token)
        {
            _authToken = token;
        }
    }
    
    [Serializable]
    public class ApiResponse
    {
        public bool success;
        public ApiResponseData data;
        public ApiError error;
        public string rawJson;
    }
    
    [Serializable]
    public class ApiResponseData
    {
        public object user;
        public object accessToken;
        public string refreshToken;
    }
    
    [Serializable]
    public class ApiError
    {
        public string message;
        public string errorCode;
    }
}
