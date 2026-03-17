using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SocialArcade.Unity.UI
{
    public abstract class UIScreen : MonoBehaviour
    {
        [Header("Screen Properties")]
        [SerializeField] protected string _screenId;
        [SerializeField] protected bool _keepInHistory = true;
        [SerializeField] protected bool _animateOnShow = true;
        
        [Header("Animation Settings")]
        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private Vector3 _showScale = Vector3.one;
        [SerializeField] private Vector3 _hideScale = Vector3.zero;
        [SerializeField] private CanvasGroup _canvasGroup;
        
        public string ScreenId => _screenId;
        public bool KeepInHistory => _keepInHistory;
        
        protected bool _isInitialized;
        
        public virtual void Initialize()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
            
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            _isInitialized = true;
        }
        
        public virtual void Show()
        {
            if (!_isInitialized) Initialize();
            
            gameObject.SetActive(true);
            
            if (_animateOnShow && _canvasGroup != null)
            {
                _canvasGroup.alpha = 0;
                transform.localScale = _hideScale;
                
                _canvasGroup.DOFade(1, _animationDuration);
                transform.DOScale(_showScale, _animationDuration).SetEase(Ease.OutBack);
            }
            
            OnShow();
        }
        
        public virtual void Hide()
        {
            if (_animateOnShow && _canvasGroup != null)
            {
                _canvasGroup.DOFade(0, _animationDuration);
                transform.DOScale(_hideScale, _animationDuration).SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                        OnHide();
                    });
            }
            else
            {
                gameObject.SetActive(false);
                OnHide();
            }
        }
        
        public virtual void SetData(object data)
        {
        }
        
        protected virtual void OnShow()
        {
        }
        
        protected virtual void OnHide()
        {
        }
        
        protected void SetInteractable(bool interactable)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = interactable;
                _canvasGroup.blocksRaycasts = interactable;
            }
        }
    }
}
