using System;
using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;

namespace SocialArcade.Unity.Networking
{
    public class NetworkPlayer
    {
        public string Id { get; set; }
        public string OdId { get; set; }
        public string Username { get; set; }
        public GameObject GameObject { get; set; }
        
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        
        public bool IsLocal { get; set; }
        public bool IsAlive { get; set; } = true;
        
        public float LastUpdateTime { get; set; }
        public Vector3 Velocity { get; set; }
        
        public NetworkPlayer(string odId, string username, GameObject gameObject, bool isLocal = false)
        {
            OdId = odId;
            Username = username;
            GameObject = gameObject;
            IsLocal = isLocal;
            Position = gameObject.transform.position;
            Rotation = gameObject.transform.rotation;
        }
    }
    
    public static class NetworkEvents
    {
        public static readonly Event<SocketIOResponse> OnPositionSync = new("OnPositionSync");
        public static readonly Event<SocketIOResponse> OnPlayerJoined = new("OnPlayerJoined");
        public static readonly Event<SocketIOResponse> OnPlayerLeft = new("OnPlayerLeft");
        public static readonly Event<SocketIOResponse> OnPlayerAction = new("OnPlayerAction");
        public static readonly Event<SocketIOResponse> OnPlayerDied = new("OnPlayerDied");
        public static readonly Event<SocketIOResponse> OnPlayerRespawned = new("OnPlayerRespawned");
    }
    
    public class NetworkPlayerManager : MonoBehaviour
    {
        private static NetworkPlayerManager _instance;
        public static NetworkPlayerManager Instance => _instance;
        
        [SerializeField] private GameObject _playerPrefab;
        
        private readonly Dictionary<string, NetworkPlayer> _players = new();
        private readonly Dictionary<string, Vector3> _targetPositions = new();
        private readonly Dictionary<string, Quaternion> _targetRotations = new();
        
        private string _localPlayerId;
        private float _syncInterval = 0.05f;
        private float _lastSyncTime;
        
        public event Action<NetworkPlayer> OnPlayerAdded;
        public event Action<string> OnPlayerRemoved;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
        }
        
        private void Start()
        {
            NetworkEvents.OnPositionSync.AddListener(HandlePositionSync);
            NetworkEvents.OnPlayerJoined.AddListener(HandlePlayerJoined);
            NetworkEvents.OnPlayerLeft.AddListener(HandlePlayerLeft);
            NetworkEvents.OnPlayerDied.AddListener(HandlePlayerDied);
            NetworkEvents.OnPlayerRespawned.AddListener(HandlePlayerRespawned);
        }
        
        private void OnDestroy()
        {
            NetworkEvents.OnPositionSync.RemoveListener(HandlePositionSync);
            NetworkEvents.OnPlayerJoined.RemoveListener(HandlePlayerJoined);
            NetworkEvents.OnPlayerLeft.RemoveListener(HandlePlayerLeft);
            NetworkEvents.OnPlayerDied.RemoveListener(HandlePlayerDied);
            NetworkEvents.OnPlayerRespawned.RemoveListener(HandlePlayerRespawned);
        }
        
        private void Update()
        {
            if (Time.time - _lastSyncTime > _syncInterval)
            {
                SyncLocalPlayer();
                _lastSyncTime = Time.time;
            }
            
            InterpolatePlayers();
        }
        
        public void SetLocalPlayer(string odId, GameObject playerObject)
        {
            _localPlayerId = odId;
            
            var networkPlayer = new NetworkPlayer(odId, "Local", playerObject, true);
            _players[odId] = networkPlayer;
            
            OnPlayerAdded?.Invoke(networkPlayer);
        }
        
        public NetworkPlayer AddRemotePlayer(string odId, string username, Vector3 position)
        {
            if (_players.ContainsKey(odId))
            {
                return _players[odId];
            }
            
            var playerObject = Instantiate(_playerPrefab, position, Quaternion.identity);
            var networkPlayer = new NetworkPlayer(odId, username, playerObject, false);
            _players[odId] = networkPlayer;
            
            OnPlayerAdded?.Invoke(networkPlayer);
            
            return networkPlayer;
        }
        
        public void RemovePlayer(string odId)
        {
            if (_players.TryGetValue(odId, out var player))
            {
                if (player.GameObject != null)
                {
                    Destroy(player.GameObject);
                }
                
                _players.Remove(odId);
                _targetPositions.Remove(odId);
                _targetRotations.Remove(odId);
                
                OnPlayerRemoved?.Invoke(odId);
            }
        }
        
        public NetworkPlayer GetPlayer(string odId)
        {
            return _players.TryGetValue(odId, out var player) ? player : null;
        }
        
        public IEnumerable<NetworkPlayer> GetAllPlayers()
        {
            return _players.Values;
        }
        
        private void SyncLocalPlayer()
        {
            if (string.IsNullOrEmpty(_localPlayerId)) return;
            
            if (!_players.TryGetValue(_localPlayerId, out var localPlayer)) return;
            
            var roomId = GameManager.Instance.CurrentPlayer?.PlayerProfile?.DisplayName ?? "game";
            NetworkManager.Instance.SendPositionUpdate(
                roomId,
                localPlayer.GameObject.transform.position,
                localPlayer.GameObject.transform.rotation
            );
        }
        
        private void HandlePositionSync(SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<PositionSyncData>();
                
                if (data.odId == _localPlayerId) return;
                
                if (!_players.ContainsKey(data.odId))
                {
                    AddRemotePlayer(data.odId, "Unknown", new Vector3(data.position.x, data.position.y, data.position.z));
                }
                
                _targetPositions[data.odId] = new Vector3(data.position.x, data.position.y, data.position.z);
                _targetRotations[data.odId] = new Quaternion(data.rotation.x, data.rotation.y, data.rotation.z, data.rotation.w);
                
                if (_players.TryGetValue(data.odId, out var player))
                {
                    player.LastUpdateTime = Time.time;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling position sync: {ex.Message}");
            }
        }
        
        private void HandlePlayerJoined(SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<PlayerJoinData>();
                AddRemotePlayer(data.odId, data.username, new Vector3(data.position.x, data.position.y, data.position.z));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling player join: {ex.Message}");
            }
        }
        
        private void HandlePlayerLeft(SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<PlayerLeaveData>();
                RemovePlayer(data.odId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling player leave: {ex.Message}");
            }
        }
        
        private void HandlePlayerDied(SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<PlayerActionData>();
                if (_players.TryGetValue(data.odId, out var player))
                {
                    player.IsAlive = false;
                    if (player.GameObject != null)
                    {
                        player.GameObject.SetActive(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling player death: {ex.Message}");
            }
        }
        
        private void HandlePlayerRespawned(SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<PlayerRespawnData>();
                if (_players.TryGetValue(data.odId, out var player))
                {
                    player.IsAlive = true;
                    player.Position = new Vector3(data.position.x, data.position.y, data.position.z);
                    
                    if (player.GameObject != null)
                    {
                        player.GameObject.SetActive(true);
                        player.GameObject.transform.position = player.Position;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling player respawn: {ex.Message}");
            }
        }
        
        private void InterpolatePlayers()
        {
            float interpolationSpeed = 10f;
            
            foreach (var kvp in _players)
            {
                var odId = kvp.Key;
                var player = kvp.Value;
                
                if (player.IsLocal) continue;
                
                if (_targetPositions.TryGetValue(odId, out var targetPos))
                {
                    var newPos = Vector3.Lerp(player.GameObject.transform.position, targetPos, Time.deltaTime * interpolationSpeed);
                    player.GameObject.transform.position = newPos;
                }
                
                if (_targetRotations.TryGetValue(odId, out var targetRot))
                {
                    var newRot = Quaternion.Slerp(player.GameObject.transform.rotation, targetRot, Time.deltaTime * interpolationSpeed);
                    player.GameObject.transform.rotation = newRot;
                }
            }
        }
        
        public void ClearAllPlayers()
        {
            foreach (var player in _players.Values)
            {
                if (player.GameObject != null)
                {
                    Destroy(player.GameObject);
                }
            }
            
            _players.Clear();
            _targetPositions.Clear();
            _targetRotations.Clear();
        }
    }
    
    [Serializable]
    public class PositionSyncData
    {
        public string odId;
        public PositionData position;
        public RotationData rotation;
        public long timestamp;
    }
    
    [Serializable]
    public class PlayerJoinData
    {
        public string odId;
        public string username;
        public PositionData position;
    }
    
    [Serializable]
    public class PlayerLeaveData
    {
        public string odId;
    }
    
    [Serializable]
    public class PlayerActionData
    {
        public string odId;
        public string action;
        public object data;
    }
    
    [Serializable]
    public class PlayerRespawnData
    {
        public string odId;
        public PositionData position;
    }
    
    [Serializable]
    public class PositionData
    {
        public float x, y, z;
    }
    
    [Serializable]
    public class RotationData
    {
        public float x, y, z, w;
    }
}
