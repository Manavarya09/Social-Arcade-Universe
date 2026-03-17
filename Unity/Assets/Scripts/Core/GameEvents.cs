using System;
using System.Collections.Generic;
using UnityEngine;

namespace SocialArcade.Unity.Core
{
    public class GameEvents
    {
        private static readonly Dictionary<string, Event> _events = new();
        
        public static Event OnPlayerConnected => GetOrCreate("OnPlayerConnected");
        public static Event OnPlayerDisconnected => GetOrCreate("OnPlayerDisconnected");
        public static Event OnGameStart => GetOrCreate("OnGameStart");
        public static Event OnGameEnd => GetOrCreate("OnGameEnd");
        public static Event OnMatchFound => GetOrCreate("OnMatchFound");
        public static Event OnMatchCanceled => GetOrCreate("OnMatchCanceled");
        public static Event OnLobbyUpdated => GetOrCreate("OnLobbyUpdated");
        public static Event OnChatMessage => GetOrCreate("OnChatMessage");
        public static Event OnReelUploaded => GetOrCreate("OnReelUploaded");
        public static Event OnReelLiked => GetOrCreate("OnReelLiked");
        public static Event OnCurrencyUpdated => GetOrCreate("OnCurrencyUpdated");
        public static Event OnLevelUp => GetOrCreate("OnLevelUp");
        public static Event OnInventoryUpdated => GetOrCreate("OnInventoryUpdated");
        public static Event OnFriendRequestReceived => GetOrCreate("OnFriendRequestReceived");
        public static Event OnFriendRequestAccepted => GetOrCreate("OnFriendRequestAccepted");
        public static Event OnError => GetOrCreate("OnError");
        public static Event OnLoadingStart => GetOrCreate("OnLoadingStart");
        public static Event OnLoadingEnd => GetOrCreate("OnLoadingEnd");
        
        private static Event GetOrCreate(string name)
        {
            if (!_events.ContainsKey(name))
            {
                _events[name] = new Event(name);
            }
            return _events[name];
        }
    }
    
    public class Event
    {
        private readonly string _name;
        private readonly List<Delegate> _listeners = new();
        private readonly object _lock = new();
        
        public string Name => _name;
        
        public Event(string name)
        {
            _name = name;
        }
        
        public void AddListener(Action listener)
        {
            lock (_lock)
            {
                _listeners.Add(listener);
            }
        }
        
        public void AddListener<T>(Action<T> listener)
        {
            lock (_lock)
            {
                _listeners.Add(listener);
            }
        }
        
        public void AddListener<T1, T2>(Action<T1, T2> listener)
        {
            lock (_lock)
            {
                _listeners.Add(listener);
            }
        }
        
        public void RemoveListener(Action listener)
        {
            lock (_lock)
            {
                _listeners.Remove(listener);
            }
        }
        
        public void RemoveListener<T>(Action<T> listener)
        {
            lock (_lock)
            {
                _listeners.Remove(listener);
            }
        }
        
        public void RemoveListener<T1, T2>(Action<T1, T2> listener)
        {
            lock (_lock)
            {
                _listeners.Remove(listener);
            }
        }
        
        public void Invoke()
        {
            lock (_lock)
            {
                foreach (var listener in _listeners)
                {
                    try
                    {
                        ((Action)listener)?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error invoking event {Name}: {ex.Message}");
                    }
                }
            }
        }
        
        public void Invoke<T>(T param)
        {
            lock (_lock)
            {
                foreach (var listener in _listeners)
                {
                    try
                    {
                        ((Action<T>)listener)?.Invoke(param);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error invoking event {Name}: {ex.Message}");
                    }
                }
            }
        }
        
        public void Invoke<T1, T2>(T1 param1, T2 param2)
        {
            lock (_lock)
            {
                foreach (var listener in _listeners)
                {
                    try
                    {
                        ((Action<T1, T2>)listener)?.Invoke(param1, param2);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error invoking event {Name}: {ex.Message}");
                    }
                }
            }
        }
        
        public void Clear()
        {
            lock (_lock)
            {
                _listeners.Clear();
            }
        }
    }
}
