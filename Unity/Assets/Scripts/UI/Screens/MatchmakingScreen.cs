using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SocialArcade.Unity.UI.Screens
{
    public class MatchmakingScreen : UIScreen
    {
        [Header("Status")]
        [SerializeField] private Text _statusText;
        [SerializeField] private Image _statusIcon;
        [SerializeField] private Text _queuePositionText;
        
        [Header("Progress")]
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private Text _progressText;
        
        [Header("Game Mode")]
        [SerializeField] private Image _modeIcon;
        [SerializeField] private Text _modeNameText;
        [SerializeField] private Text _modeDescriptionText;
        
        [Header("Players Waiting")]
        [SerializeField] private Transform _playersContainer;
        [SerializeField] private GameObject _playerSlotPrefab;
        
        [Header("Buttons")]
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _changeModeButton;
        
        [Header("Animation")]
        [SerializeField] private CanvasGroup _loadingCanvasGroup;
        [SerializeField] private RectTransform _searchingIcon;
        
        private string _selectedMode;
        private bool _isSearching;
        private Coroutine _searchingAnimation;
        
        protected override void OnShow()
        {
            base.OnShow();
            
            _cancelButton.onClick.AddListener(OnCancelClick);
            _changeModeButton.onClick.AddListener(OnChangeModeClick);
            
            StartMatchmaking();
        }
        
        private void StartMatchmaking()
        {
            _isSearching = true;
            _statusText.text = "Searching for players...";
            _queuePositionText.text = "Position: --";
            
            Networking.NetworkManager.Instance.JoinMatchmaking(_selectedMode ?? "ranked", 1000);
            
            StartSearchAnimation();
        }
        
        private void StartSearchAnimation()
        {
            if (_searchingAnimation != null) StopCoroutine(_searchingAnimation);
            _searchingAnimation = StartCoroutine(SearchAnimationRoutine());
        }
        
        private IEnumerator SearchAnimationRoutine()
        {
            while (_isSearching)
            {
                _searchingIcon.DORotate(new Vector3(0, 0, -360), 2f, RotateMode.LocalAxisAdd)
                    .SetEase(Ease.Linear);
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        public void UpdateQueuePosition(int position)
        {
            _queuePositionText.text = $"Position: #{position}";
            
            if (position <= 3)
            {
                _statusText.text = "Almost there...";
                _progressSlider.value = 1f - (position / 10f);
            }
        }
        
        public void OnMatchFound()
        {
            _isSearching = false;
            StopCoroutine(_searchingAnimation);
            
            _statusText.text = "Match Found!";
            _statusText.color = Color.green;
            
            _loadingCanvasGroup.DOFade(0, 0.3f);
            
            StartCoroutine(LoadGameRoutine());
        }
        
        private IEnumerator LoadGameRoutine()
        {
            yield return new WaitForSeconds(1f);
            
            Core.GameEvents.OnMatchFound.Invoke();
            UIManager.Instance.ShowScreen("Game");
        }
        
        public void OnMatchmakingError(string error)
        {
            _isSearching = false;
            _statusText.text = error;
            _statusText.color = Color.red;
            
            _cancelButton.GetComponentInChildren<Text>().text = "Go Back";
        }
        
        private void OnCancelClick()
        {
            if (_isSearching)
            {
                Networking.NetworkManager.Instance.LeaveMatchmaking();
            }
            
            _isSearching = false;
            StopCoroutine(_searchingAnimation);
            
            UIManager.Instance.GoBack();
        }
        
        private void OnChangeModeClick()
        {
            UIManager.Instance.ShowScreen("GameModeSelect");
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            
            _cancelButton.onClick.RemoveAllListeners();
            _changeModeButton.onClick.RemoveAllListeners();
            
            if (_isSearching)
            {
                Networking.NetworkManager.Instance.LeaveMatchmaking();
            }
            
            _isSearching = false;
        }
    }
}
