using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SocialArcade.Unity.UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance => _instance;
        
        [Header("Screen Management")]
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private Transform _screensContainer;
        
        [Header("Loading")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private Image _loadingProgressBar;
        [SerializeField] private Text _loadingText;
        
        [Header("Toast Notifications")]
        [SerializeField] private Transform _toastContainer;
        
        private readonly Dictionary<string, UIScreen> _screens = new();
        private UIScreen _currentScreen;
        private readonly Stack<UIScreen> _screenHistory = new();
        
        public UIScreen CurrentScreen => _currentScreen;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeScreens();
            RegisterEvents();
        }
        
        private void RegisterEvents()
        {
            Core.GameEvents.OnLoadingStart.AddListener(ShowLoading);
            Core.GameEvents.OnLoadingEnd.AddListener(HideLoading);
            Core.GameEvents.OnError.AddListener(ShowError);
        }
        
        private void OnDestroy()
        {
            Core.GameEvents.OnLoadingStart.RemoveListener(ShowLoading);
            Core.GameEvents.OnLoadingEnd.RemoveListener(HideLoading);
            Core.GameEvents.OnError.RemoveListener(ShowError);
        }
        
        private void InitializeScreens()
        {
            if (_screensContainer == null) return;
            
            var screens = _screensContainer.GetComponentsInChildren<UIScreen>(true);
            foreach (var screen in screens)
            {
                _screens[screen.ScreenId] = screen;
                screen.Initialize();
                screen.Hide();
            }
            
            if (_screens.Count > 0)
            {
                ShowScreen("Splash");
            }
        }
        
        public void ShowScreen(string screenId)
        {
            if (!_screens.TryGetValue(screenId, out var screen))
            {
                Debug.LogWarning($"Screen not found: {screenId}");
                return;
            }
            
            if (_currentScreen != null)
            {
                if (_currentScreen == screen) return;
                
                if (_currentScreen.KeepInHistory)
                {
                    _screenHistory.Push(_currentScreen);
                }
                
                _currentScreen.Hide();
            }
            
            _currentScreen = screen;
            _currentScreen.Show();
            
            Debug.Log($"Showing screen: {screenId}");
        }
        
        public void ShowScreen(string screenId, object data)
        {
            if (!_screens.TryGetValue(screenId, out var screen))
            {
                Debug.LogWarning($"Screen not found: {screenId}");
                return;
            }
            
            screen.SetData(data);
            ShowScreen(screenId);
        }
        
        public void GoBack()
        {
            if (_screenHistory.Count > 0)
            {
                var previousScreen = _screenHistory.Pop();
                _currentScreen?.Hide();
                _currentScreen = previousScreen;
                _currentScreen.Show();
            }
        }
        
        public void CloseAllScreens()
        {
            foreach (var screen in _screens.Values)
            {
                screen.Hide();
            }
            
            _currentScreen = null;
            _screenHistory.Clear();
        }
        
        public T GetScreen<T>(string screenId) where T : UIScreen
        {
            if (_screens.TryGetValue(screenId, out var screen))
            {
                return screen as T;
            }
            return null;
        }
        
        public void ShowLoading()
        {
            if (_loadingPanel != null)
            {
                _loadingPanel.SetActive(true);
            }
        }
        
        public void HideLoading()
        {
            if (_loadingPanel != null)
            {
                _loadingPanel.SetActive(false);
            }
        }
        
        public void SetLoadingProgress(float progress)
        {
            if (_loadingProgressBar != null)
            {
                _loadingProgressBar.fillAmount = progress;
            }
        }
        
        public void SetLoadingText(string text)
        {
            if (_loadingText != null)
            {
                _loadingText.text = text;
            }
        }
        
        public void ShowToast(string message, ToastType type = ToastType.Info, float duration = 3f)
        {
            if (_toastContainer == null) return;
            
            var toastPrefab = Resources.Load<GameObject>("Prefabs/UI/Toast");
            if (toastPrefab != null)
            {
                var toast = Instantiate(toastPrefab, _toastContainer);
                var toastComponent = toast.GetComponent<ToastComponent>();
                if (toastComponent != null)
                {
                    toastComponent.Show(message, type, duration);
                }
            }
        }
        
        public void ShowError(string message)
        {
            ShowToast(message, ToastType.Error, 5f);
        }
    }
    
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
