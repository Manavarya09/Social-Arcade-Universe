using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SocialArcade.Unity.Social
{
    public class ReelsManager : MonoBehaviour
    {
        private static ReelsManager _instance;
        public static ReelsManager Instance => _instance;
        
        [Header("Reels Feed")]
        [SerializeField] private List<ReelData> _reels = new();
        [SerializeField] private int _currentReelIndex;
        
        [Header("Video Player")]
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private RawImage _videoDisplay;
        [SerializeField] private bool _autoPlay = true;
        
        public List<ReelData> Reels => _reels;
        public ReelData CurrentReel => _reels.Count > _currentReelIndex ? _reels[_currentReelIndex] : null;
        
        public event Action<List<ReelData>> OnReelsLoaded;
        public event Action<ReelData> OnReelChanged;
        public event Action<bool> OnLikeStateChanged;
        
        private bool _isLoading;
        private string _currentCategory;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            if (_videoPlayer == null)
            {
                _videoPlayer = GetComponent<VideoPlayer>();
            }
            
            if (_videoPlayer != null)
            {
                _videoPlayer.prepareCompleted += OnVideoPrepared;
                _videoPlayer.loopPointReached += OnVideoEnded;
            }
        }
        
        private void OnDestroy()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.prepareCompleted -= OnVideoPrepared;
                _videoPlayer.loopPointReached -= OnVideoEnded;
            }
        }
        
        public async Task LoadReelsAsync(int page = 1, string category = null)
        {
            if (_isLoading) return;
            
            _isLoading = true;
            _currentCategory = category;
            
            try
            {
                var response = await Networking.NetworkManager.Instance.GetReelsAsync(page);
                if (response.success)
                {
                    var reels = ParseReels(response.rawJson);
                    
                    if (page == 1)
                    {
                        _reels = reels;
                    }
                    else
                    {
                        _reels.AddRange(reels);
                    }
                    
                    OnReelsLoaded?.Invoke(_reels);
                    
                    if (_autoPlay && _reels.Count > 0)
                    {
                        PlayReel(0);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load reels: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }
        
        public void PlayReel(int index)
        {
            if (index < 0 || index >= _reels.Count) return;
            
            _currentReelIndex = index;
            var reel = _reels[index];
            
            if (_videoPlayer != null && _videoDisplay != null)
            {
                _videoPlayer.source = VideoSource.Url;
                _videoPlayer.url = reel.videoUrl;
                _videoPlayer.Prepare();
                
                _videoDisplay.texture = null;
            }
            
            OnReelChanged?.Invoke(reel);
        }
        
        public void NextReel()
        {
            int nextIndex = _currentReelIndex + 1;
            
            if (nextIndex >= _reels.Count)
            {
                _ = LoadReelsAsync(_reels.Count / 20 + 1, _currentCategory);
                return;
            }
            
            PlayReel(nextIndex);
        }
        
        public void PreviousReel()
        {
            int prevIndex = _currentReelIndex - 1;
            if (prevIndex >= 0)
            {
                PlayReel(prevIndex);
            }
        }
        
        public async void LikeReel()
        {
            if (CurrentReel == null) return;
            
            try
            {
                // Call API to like
                CurrentReel.isLiked = !CurrentReel.isLiked;
                CurrentReel.likes += CurrentReel.isLiked ? 1 : -1;
                
                OnLikeStateChanged?.Invoke(CurrentReel.isLiked);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to like reel: {ex.Message}");
            }
        }
        
        public async Task UploadReel(string videoPath, string title, string gameMode)
        {
            // Implementation for uploading reel
        }
        
        private void OnVideoPrepared(VideoPlayer source)
        {
            source.Play();
        }
        
        private void OnVideoEnded(VideoPlayer source)
        {
            NextReel();
        }
        
        public void Pause()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Pause();
            }
        }
        
        public void Resume()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Play();
            }
        }
        
        public void TogglePlayPause()
        {
            if (_videoPlayer != null)
            {
                if (_videoPlayer.isPlaying)
                {
                    Pause();
                }
                else
                {
                    Resume();
                }
            }
        }
        
        private List<ReelData> ParseReels(string json)
        {
            return new List<ReelData>();
        }
    }
    
    [Serializable]
    public class ReelData
    {
        public string id;
        public string odId;
        public string username;
        public string displayName;
        public string avatarUrl;
        public string videoUrl;
        public string thumbnailUrl;
        public string title;
        public string description;
        public string gameMode;
        public int duration;
        public int views;
        public int likes;
        public int commentsCount;
        public bool isLiked;
        public bool isFeatured;
        public DateTime createdAt;
    }
}
