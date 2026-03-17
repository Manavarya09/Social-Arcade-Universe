using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SocialArcade.Unity.Social
{
    public class SocialManager : MonoBehaviour
    {
        private static SocialManager _instance;
        public static SocialManager Instance => _instance;
        
        [Header("Friends")]
        [SerializeField] private List<FriendData> _friends = new();
        
        [Header("Chat")]
        [SerializeField] private List<ChatMessage> _currentChatMessages = new();
        
        public List<FriendData> Friends => _friends;
        public List<ChatMessage> CurrentChatMessages => _currentChatMessages;
        
        public event Action<List<FriendData>> OnFriendsLoaded;
        public event Action<FriendData> OnFriendRequestReceived;
        public event Action<ChatMessage> OnChatMessageReceived;
        
        private string _currentChatRoom;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Core.GameEvents.OnChatMessage.AddListener(HandleChatMessage);
            Core.GameEvents.OnFriendRequestReceived.AddListener(HandleFriendRequest);
        }
        
        private void OnDestroy()
        {
            Core.GameEvents.OnChatMessage.RemoveListener(HandleChatMessage);
            Core.GameEvents.OnFriendRequestReceived.RemoveListener(HandleFriendRequest);
        }
        
        public async Task LoadFriendsAsync()
        {
            try
            {
                var response = await Networking.NetworkManager.Instance.GetFriendsAsync();
                if (response.success)
                {
                    var friends = ParseFriends(response.rawJson);
                    _friends = friends;
                    OnFriendsLoaded?.Invoke(friends);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load friends: {ex.Message}");
            }
        }
        
        public async Task SendFriendRequestAsync(string userId)
        {
            // Implementation for sending friend request via API
        }
        
        public async Task AcceptFriendRequestAsync(string requestId)
        {
            // Implementation for accepting friend request
        }
        
        public async Task RemoveFriendAsync(string friendId)
        {
            _friends.RemoveAll(f => f.id == friendId);
            OnFriendsLoaded?.Invoke(_friends);
        }
        
        public void JoinChatRoom(string roomId)
        {
            _currentChatRoom = roomId;
            _currentChatMessages.Clear();
            
            Networking.NetworkManager.Instance.JoinLobby(roomId);
        }
        
        public void LeaveChatRoom()
        {
            if (!string.IsNullOrEmpty(_currentChatRoom))
            {
                Networking.NetworkManager.Instance.LeaveLobby(_currentChatRoom);
            }
            _currentChatRoom = null;
            _currentChatMessages.Clear();
        }
        
        public void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(_currentChatRoom) || string.IsNullOrEmpty(message)) return;
            
            Networking.NetworkManager.Instance.SendChatMessage(_currentChatRoom, message);
        }
        
        private void HandleChatMessage(SocketIOClient.SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<ChatMessageData>();
                var message = new ChatMessage
                {
                    id = data.id,
                    odId = data.userId,
                    username = data.username,
                    content = data.message,
                    timestamp = DateTime.Now
                };
                
                _currentChatMessages.Add(message);
                OnChatMessageReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling chat message: {ex.Message}");
            }
        }
        
        private void HandleFriendRequest(SocketIOClient.SocketIOResponse response)
        {
            // Handle friend request notification
        }
        
        private List<FriendData> ParseFriends(string json)
        {
            return new List<FriendData>();
        }
    }
    
    [Serializable]
    public class FriendData
    {
        public string id;
        public string odId;
        public string username;
        public string displayName;
        public string avatarUrl;
        public int level;
        public bool isOnline;
    }
    
    [Serializable]
    public class ChatMessage
    {
        public string id;
        public string odId;
        public string username;
        public string content;
        public DateTime timestamp;
    }
    
    [Serializable]
    public class ChatMessageData
    {
        public string id;
        public string userId;
        public string username;
        public string message;
    }
}
