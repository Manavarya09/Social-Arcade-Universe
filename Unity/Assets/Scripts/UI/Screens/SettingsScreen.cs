using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SocialArcade.Unity.UI.Screens
{
    public class SettingsScreen : UIScreen
    {
        [Header("Audio")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        
        [Header("Graphics")]
        [SerializeField] private Dropdown _qualityDropdown;
        [SerializeField] private Toggle _vSyncToggle;
        [SerializeField] private Toggle _bloomToggle;
        [SerializeField] private Toggle _shadowsToggle;
        
        [Header("Gameplay")]
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private Toggle _invertYToggle;
        [SerializeField] private Toggle _controllerVibrationToggle;
        
        [Header("Social")]
        [SerializeField] private Toggle _notificationsToggle;
        [SerializeField] private Toggle _friendRequestsToggle;
        [SerializeField] private Toggle _showOnlineStatusToggle;
        
        [Header("Account")]
        [SerializeField] private Button _changeUsernameButton;
        [SerializeField] private Button _changePasswordButton;
        [SerializeField] private Button _linkSocialButton;
        [SerializeField] private Button _deleteAccountButton;
        
        [Header("Navigation")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _logoutButton;
        
        [Header("Version")]
        [SerializeField] private Text _versionText;
        
        [Header("Confirmation")]
        [SerializeField] private GameObject _confirmDialog;
        [SerializeField] private Text _confirmText;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;
        
        private Action _pendingAction;
        
        protected override void OnShow()
        {
            base.OnShow();
            
            LoadSettings();
            SetupListeners();
            AnimateEntrance();
        }
        
        private void LoadSettings()
        {
            _masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            _musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            _sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            
            _qualityDropdown.value = PlayerPrefs.GetInt("QualityLevel", 2);
            _vSyncToggle.isOn = PlayerPrefs.GetInt("VSync", 1) == 1;
            _bloomToggle.isOn = PlayerPrefs.GetInt("Bloom", 1) == 1;
            _shadowsToggle.isOn = PlayerPrefs.GetInt("Shadows", 1) == 1;
            
            _sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", 1f);
            _invertYToggle.isOn = PlayerPrefs.GetInt("InvertY", 0) == 1;
            _controllerVibrationToggle.isOn = PlayerPrefs.GetInt("ControllerVibration", 1) == 1;
            
            _notificationsToggle.isOn = PlayerPrefs.GetInt("Notifications", 1) == 1;
            _friendRequestsToggle.isOn = PlayerPrefs.GetInt("FriendRequests", 1) == 1;
            _showOnlineStatusToggle.isOn = PlayerPrefs.GetInt("ShowOnlineStatus", 1) == 1;
            
            _versionText.text = $"Version {Application.version}";
        }
        
        private void SetupListeners()
        {
            _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            
            _qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            _vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            _bloomToggle.onValueChanged.AddListener(OnBloomChanged);
            _shadowsToggle.onValueChanged.AddListener(OnShadowsChanged);
            
            _sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            _invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
            _controllerVibrationToggle.onValueChanged.AddListener(OnControllerVibrationChanged);
            
            _notificationsToggle.onValueChanged.AddListener(OnNotificationsChanged);
            _friendRequestsToggle.onValueChanged.AddListener(OnFriendRequestsChanged);
            _showOnlineStatusToggle.onValueChanged.AddListener(OnShowOnlineStatusChanged);
            
            _changeUsernameButton.onClick.AddListener(OnChangeUsernameClick);
            _changePasswordButton.onClick.AddListener(OnChangePasswordClick);
            _linkSocialButton.onClick.AddListener(OnLinkSocialClick);
            _deleteAccountButton.onClick.AddListener(OnDeleteAccountClick);
            
            _backButton.onClick.AddListener(OnBackClick);
            _logoutButton.onClick.AddListener(OnLogoutClick);
            
            _confirmNoButton.onClick.AddListener(OnConfirmNoClick);
        }
        
        private void AnimateEntrance()
        {
            var sections = GetComponentsInChildren<CanvasGroup>();
            foreach (var section in sections)
            {
                section.alpha = 0;
                section.DOFade(1, 0.3f);
            }
        }
        
        private void OnMasterVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("MasterVolume", value);
            AudioListener.volume = value;
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume", value);
        }
        
        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
        }
        
        private void OnQualityChanged(int value)
        {
            PlayerPrefs.SetInt("QualityLevel", value);
            QualitySettings.SetQualityLevel(value);
        }
        
        private void OnVSyncChanged(bool value)
        {
            PlayerPrefs.SetInt("VSync", value ? 1 : 0);
            QualitySettings.vSyncCount = value ? 1 : 0;
        }
        
        private void OnBloomChanged(bool value)
        {
            PlayerPrefs.SetInt("Bloom", value ? 1 : 0);
        }
        
        private void OnShadowsChanged(bool value)
        {
            PlayerPrefs.SetInt("Shadows", value ? 1 : 0);
            QualitySettings.shadowQuality = value ? ShadowQuality.All : ShadowQuality.Disable;
        }
        
        private void OnSensitivityChanged(float value)
        {
            PlayerPrefs.SetFloat("Sensitivity", value);
        }
        
        private void OnInvertYChanged(bool value)
        {
            PlayerPrefs.SetInt("InvertY", value ? 1 : 0);
        }
        
        private void OnControllerVibrationChanged(bool value)
        {
            PlayerPrefs.SetInt("ControllerVibration", value ? 1 : 0);
        }
        
        private void OnNotificationsChanged(bool value)
        {
            PlayerPrefs.SetInt("Notifications", value ? 1 : 0);
        }
        
        private void OnFriendRequestsChanged(bool value)
        {
            PlayerPrefs.SetInt("FriendRequests", value ? 1 : 0);
        }
        
        private void OnShowOnlineStatusChanged(bool value)
        {
            PlayerPrefs.SetInt("ShowOnlineStatus", value ? 1 : 0);
        }
        
        private void OnChangeUsernameClick()
        {
            ShowConfirmDialog("Are you sure you want to change your username?", () => {
                UIManager.Instance.ShowScreen("ChangeUsername");
            });
        }
        
        private void OnChangePasswordClick()
        {
            UIManager.Instance.ShowScreen("ChangePassword");
        }
        
        private void OnLinkSocialClick()
        {
            UIManager.Instance.ShowScreen("LinkSocial");
        }
        
        private void OnDeleteAccountClick()
        {
            ShowConfirmDialog("Are you sure you want to delete your account? This action cannot be undone.", () => {
                UIManager.Instance.ShowScreen("DeleteAccount");
            });
        }
        
        private void OnBackClick()
        {
            UIManager.Instance.GoBack();
        }
        
        private void OnLogoutClick()
        {
            ShowConfirmDialog("Are you sure you want to log out?", () => {
                Core.GameManager.Instance.Logout();
            });
        }
        
        private void ShowConfirmDialog(string message, Action onConfirm)
        {
            _confirmText.text = message;
            _pendingAction = onConfirm;
            _confirmDialog.SetActive(true);
            
            _confirmYesButton.onClick.AddListener(OnConfirmYesClick);
        }
        
        private void OnConfirmYesClick()
        {
            _pendingAction?.Invoke();
            CloseConfirmDialog();
        }
        
        private void OnConfirmNoClick()
        {
            CloseConfirmDialog();
        }
        
        private void CloseConfirmDialog()
        {
            _confirmDialog.SetActive(false);
            _confirmYesButton.onClick.RemoveListener(OnConfirmYesClick);
            _pendingAction = null;
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            
            PlayerPrefs.Save();
        }
    }
}
