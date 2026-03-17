using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SocialArcade.Unity.UI
{
    public class ToastComponent : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Text _messageText;
        [SerializeField] private Image _iconImage;
        
        [Header("Colors")]
        [SerializeField] private Color _infoColor = new Color(0.455f, 0.725f, 1f);
        [SerializeField] private Color _successColor = new Color(0f, 0.722f, 0.58f);
        [SerializeField] private Color _warningColor = new Color(0.992f, 0.796f, 0.431f);
        [SerializeField] private Color _errorColor = new Color(0.906f, 0.298f, 0.235f);
        
        private void Start()
        {
            transform.SetAsLastSibling();
        }
        
        public void Show(string message, ToastType type, float duration)
        {
            _messageText.text = message;
            
            Color bgColor;
            switch (type)
            {
                case ToastType.Success:
                    bgColor = _successColor;
                    break;
                case ToastType.Warning:
                    bgColor = _warningColor;
                    break;
                case ToastType.Error:
                    bgColor = _errorColor;
                    break;
                default:
                    bgColor = _infoColor;
                    break;
            }
            
            _background.color = bgColor;
            
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            
            Invoke(nameof(Hide), duration);
        }
        
        private void Hide()
        {
            transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    Destroy(gameObject);
                });
        }
    }
}
