using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SocialArcade.Unity.UI.Screens
{
    public class LoginScreen : UIScreen
    {
        [Header("Login Form")]
        [SerializeField] private InputField _emailInput;
        [SerializeField] private InputField _passwordInput;
        [SerializeField] private Button _loginButton;
        [SerializeField] private Text _loginButtonText;
        
        [Header("Register Form")]
        [SerializeField] private GameObject _registerPanel;
        [SerializeField] private InputField _registerEmailInput;
        [SerializeField] private InputField _registerUsernameInput;
        [SerializeField] private InputField _registerPasswordInput;
        [SerializeField] private InputField _confirmPasswordInput;
        [SerializeField] private Button _registerButton;
        
        [Header("UI Elements")]
        [SerializeField] private Button _switchToRegisterButton;
        [SerializeField] private Button _switchToLoginButton;
        [SerializeField] private GameObject _loadingIndicator;
        [SerializeField] private Text _errorText;
        
        [Header("Visual")]
        [SerializeField] private CanvasGroup _logoCanvasGroup;
        [SerializeField] private Image _backgroundImage;
        
        private bool _isLoginMode = true;
        
        protected override void OnShow()
        {
            base.OnShow();
            
            _loginButton.onClick.AddListener(OnLoginClick);
            _registerButton.onClick.AddListener(OnRegisterClick);
            _switchToRegisterButton.onClick.AddListener(ShowRegisterForm);
            _switchToLoginButton.onClick.AddListener(ShowLoginForm);
            
            AnimateEntrance();
            
            _errorText.text = "";
        }
        
        private void AnimateEntrance()
        {
            if (_logoCanvasGroup != null)
            {
                _logoCanvasGroup.alpha = 0;
                _logoCanvasGroup.DOFade(1, 0.5f);
            }
            
            if (_backgroundImage != null)
            {
                _backgroundImage.DOFade(0.8f, 0.5f);
            }
        }
        
        private void ShowRegisterForm()
        {
            _isLoginMode = false;
            _registerPanel.SetActive(true);
            AnimateFormSwitch();
        }
        
        private void ShowLoginForm()
        {
            _isLoginMode = true;
            _registerPanel.SetActive(false);
            AnimateFormSwitch();
        }
        
        private void AnimateFormSwitch()
        {
            var rectTransform = (_isLoginMode ? _loginButton.transform.parent : _registerPanel.transform) as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.DOComplete();
                rectTransform.localScale = Vector3.zero;
                rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
        }
        
        private async void OnLoginClick()
        {
            string email = _emailInput.text.Trim();
            string password = _passwordInput.text;
            
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Please fill in all fields");
                return;
            }
            
            SetLoading(true);
            _errorText.text = "";
            
            try
            {
                var success = await Core.GameManager.Instance.LoginAsync(email, password);
                
                if (success)
                {
                    UIManager.Instance.ShowScreen("Home");
                }
                else
                {
                    ShowError("Login failed. Please check your credentials.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }
        
        private async void OnRegisterClick()
        {
            string email = _registerEmailInput.text.Trim();
            string username = _registerUsernameInput.text.Trim();
            string password = _registerPasswordInput.text;
            string confirmPassword = _confirmPasswordInput.text;
            
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ShowError("Please fill in all fields");
                return;
            }
            
            if (password != confirmPassword)
            {
                ShowError("Passwords do not match");
                return;
            }
            
            if (password.Length < 6)
            {
                ShowError("Password must be at least 6 characters");
                return;
            }
            
            SetLoading(true);
            _errorText.text = "";
            
            try
            {
                var success = await Core.GameManager.Instance.RegisterAsync(email, username, password);
                
                if (success)
                {
                    UIManager.Instance.ShowScreen("Home");
                }
                else
                {
                    ShowError("Registration failed. Please try again.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }
        
        private void SetLoading(bool loading)
        {
            _loginButton.interactable = !loading;
            _registerButton.interactable = !loading;
            
            _loadingIndicator?.SetActive(loading);
            
            _loginButtonText.text = loading ? "Loading..." : "Login";
        }
        
        private void ShowError(string message)
        {
            _errorText.text = message;
            _errorText.DOComplete();
            _errorText.color = Color.red;
            _errorText.transform.DOShakeScale(0.3f, Vector3.one * 0.1f);
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            
            _loginButton.onClick.RemoveListener(OnLoginClick);
            _registerButton.onClick.RemoveListener(OnRegisterClick);
            _switchToRegisterButton.onClick.RemoveListener(ShowRegisterForm);
            _switchToLoginButton.onClick.RemoveListener(ShowLoginForm);
            
            _emailInput.text = "";
            _passwordInput.text = "";
        }
    }
}
