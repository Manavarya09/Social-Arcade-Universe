using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SocialArcade.Unity.UI.Screens
{
    public class ShopScreen : UIScreen
    {
        [Header("Categories")]
        [SerializeField] private Button _allButton;
        [SerializeField] private Button _skinsButton;
        [SerializeField] private Button _emotesButton;
        [SerializeField] private Button _effectsButton;
        [SerializeField] private Button _boostsButton;
        
        [Header("Currency Display")]
        [SerializeField] private Text _coinsText;
        [SerializeField] private Text _gemsText;
        
        [Header("Items Grid")]
        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private GameObject _shopItemPrefab;
        
        [Header("Filter")]
        [SerializeField] private Dropdown _sortDropdown;
        
        [Header("Loading")]
        [SerializeField] private GameObject _loadingIndicator;
        [SerializeField] private Text _emptyText;
        
        [Header("Purchase Dialog")]
        [SerializeField] private GameObject _purchaseDialog;
        [SerializeField] private Image _itemPreviewImage;
        [SerializeField] private Text _itemNameText;
        [SerializeField] private Text _itemDescriptionText;
        [SerializeField] private Text _itemPriceText;
        [SerializeField] private Button _purchaseButton;
        [SerializeField] private Button _cancelButton;
        
        private string _currentCategory = "all";
        private List<ShopItemData> _items = new();
        private ShopItemData _selectedItem;
        
        protected override void OnShow()
        {
            base.OnShow();
            
            SetupCategoryButtons();
            _purchaseButton.onClick.AddListener(OnPurchaseClick);
            _cancelButton.onClick.AddListener(OnCancelPurchase);
            
            RefreshCurrency();
            LoadItems();
            AnimateEntrance();
        }
        
        private void AnimateEntrance()
        {
            var buttons = new[] { _allButton, _skinsButton, _emotesButton, _effectsButton, _boostsButton };
            float delay = 0;
            
            foreach (var button in buttons)
            {
                button.transform.DOComplete();
                button.transform.localScale = Vector3.zero;
                button.transform.DOScale(Vector3.one, 0.2f)
                    .SetEase(Ease.OutBack)
                    .SetDelay(delay);
                delay += 0.05f;
            }
        }
        
        private void SetupCategoryButtons()
        {
            _allButton.onClick.AddListener(() => OnCategoryClick("all"));
            _skinsButton.onClick.AddListener(() => OnCategoryClick("skin"));
            _emotesButton.onClick.AddListener(() => OnCategoryClick("emote"));
            _effectsButton.onClick.AddListener(() => OnCategoryClick("effect"));
            _boostsButton.onClick.AddListener(() => OnCategoryClick("boost"));
        }
        
        private void OnCategoryClick(string category)
        {
            _currentCategory = category;
            
            var buttons = new[] { _allButton, _skinsButton, _emotesButton, _effectsButton, _boostsButton };
            foreach (var button in buttons)
            {
                var isSelected = category == "all" && button == _allButton ||
                                category == "skin" && button == _skinsButton ||
                                category == "emote" && button == _emotesButton ||
                                category == "effect" && button == _effectsButton ||
                                category == "boost" && button == _boostsButton;
                
                button.image.DOColor(isSelected ? new Color(0.424f, 0.361f, 0.906f) : Color.white, 0.2f);
            }
            
            LoadItems();
        }
        
        private async void LoadItems()
        {
            _loadingIndicator.SetActive(true);
            _emptyText.gameObject.SetActive(false);
            
            ClearItems();
            
            try
            {
                var response = await Networking.NetworkManager.Instance.GetShopItemsAsync(
                    _currentCategory == "all" ? null : _currentCategory
                );
                
                var mockItems = GetMockItems();
                
                foreach (var item in mockItems)
                {
                    AddShopItem(item);
                }
                
                _emptyText.gameObject.SetActive(mockItems.Count == 0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load shop items: {ex.Message}");
            }
            finally
            {
                _loadingIndicator.SetActive(false);
            }
        }
        
        private List<ShopItemData> GetMockItems()
        {
            var items = new List<ShopItemData>();
            
            if (_currentCategory == "all" || _currentCategory == "skin")
            {
                items.Add(new ShopItemData { itemId = "skin_warrior", name = "Warrior", type = "skin", priceCoins = 500, rarity = "rare" });
                items.Add(new ShopItemData { itemId = "skin_ninja", name = "Ninja", type = "skin", priceGems = 100, rarity = "epic" });
                items.Add(new ShopItemData { itemId = "skin_royal", name = "Royal", type = "skin", priceGems = 500, rarity = "legendary" });
            }
            
            if (_currentCategory == "all" || _currentCategory == "emote")
            {
                items.Add(new ShopItemData { itemId = "emote_dance", name = "Dance", type = "emote", priceCoins = 200, rarity = "common" });
                items.Add(new ShopItemData { itemId = "emote_victory", name = "Victory", type = "emote", priceCoins = 300, rarity = "rare" });
            }
            
            if (_currentCategory == "all" || _currentCategory == "effect")
            {
                items.Add(new ShopItemData { itemId = "effect_fire", name = "Fire Trail", type = "effect", priceCoins = 400, rarity = "rare" });
                items.Add(new ShopItemData { itemId = "effect_explode", name = "Explosion", type = "effect", priceGems = 50, rarity = "epic" });
            }
            
            if (_currentCategory == "all" || _currentCategory == "boost")
            {
                items.Add(new ShopItemData { itemId = "boost_xp", name = "XP Boost", type = "boost", priceCoins = 100, rarity = "common" });
            }
            
            return items;
        }
        
        private void ClearItems()
        {
            foreach (Transform child in _itemsContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        private void AddShopItem(ShopItemData item)
        {
            var itemObj = Instantiate(_shopItemPrefab, _itemsContainer);
            var script = itemObj.GetComponent<ShopItemComponent>();
            
            if (script != null)
            {
                script.Initialize(item, OnItemClicked);
            }
        }
        
        private void OnItemClicked(ShopItemData item)
        {
            _selectedItem = item;
            
            _itemNameText.text = item.name;
            _itemDescriptionText.text = item.description ?? "A special item!";
            
            if (item.priceGems > 0)
            {
                _itemPriceText.text = $"{item.priceGems} Gems";
            }
            else
            {
                _itemPriceText.text = $"{item.priceCoins} Coins";
            }
            
            _purchaseDialog.SetActive(true);
            _purchaseDialog.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
        
        private async void OnPurchaseClick()
        {
            if (_selectedItem == null) return;
            
            bool hasEnough = _selectedItem.priceGems > 0 
                ? Economy.EconomyManager.Instance.Gems >= _selectedItem.priceGems
                : Economy.EconomyManager.Instance.Coins >= _selectedItem.priceCoins;
            
            if (!hasEnough)
            {
                UIManager.Instance.ShowError("Not enough currency!");
                return;
            }
            
            _loadingIndicator.SetActive(true);
            
            try
            {
                var success = await Economy.EconomyManager.Instance.PurchaseItemAsync(_selectedItem.itemId);
                
                if (success)
                {
                    UIManager.Instance.ShowToast($"Purchased {_selectedItem.name}!", UI.ToastType.Success);
                    _purchaseDialog.SetActive(false);
                    RefreshCurrency();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Purchase failed: {ex.Message}");
            }
            finally
            {
                _loadingIndicator.SetActive(false);
            }
        }
        
        private void OnCancelPurchase()
        {
            _purchaseDialog.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => _purchaseDialog.SetActive(false));
        }
        
        private void RefreshCurrency()
        {
            var currency = Economy.EconomyManager.Instance;
            _coinsText.text = currency.Coins.ToString("N0");
            _gemsText.text = currency.Gems.ToString("N0");
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            
            _allButton.onClick.RemoveAllListeners();
            _skinsButton.onClick.RemoveAllListeners();
            _emotesButton.onClick.RemoveAllListeners();
            _effectsButton.onClick.RemoveAllListeners();
            _boostsButton.onClick.RemoveAllListeners();
            _purchaseButton.onClick.RemoveAllListeners();
            _cancelButton.onClick.RemoveAllListeners();
        }
    }
    
    public class ShopItemData
    {
        public string itemId;
        public string name;
        public string description;
        public string type;
        public int priceCoins;
        public int priceGems;
        public string rarity;
    }
    
    public class ShopItemComponent : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _priceText;
        [SerializeField] private Image _rarityIndicator;
        [SerializeField] private GameObject _ownedIndicator;
        
        private ShopItemData _item;
        private Action<ShopItemData> _onClick;
        
        public void Initialize(ShopItemData item, Action<ShopItemData> onClick)
        {
            _item = item;
            _onClick = onClick;
            
            _nameText.text = item.name;
            
            if (item.priceGems > 0)
            {
                _priceText.text = $"{item.priceGems} Gems";
                _priceText.color = new Color(0.992f, 0.475f, 0.659f);
            }
            else
            {
                _priceText.text = $"{item.priceCoins} Coins";
                _priceText.color = new Color(1f, 0.843f, 0f);
            }
            
            _rarityIndicator.color = GetRarityColor(item.rarity);
            
            GetComponent<Button>().onClick.AddListener(OnClick);
        }
        
        private Color GetRarityColor(string rarity)
        {
            return rarity switch
            {
                "common" => Color.gray,
                "rare" => Color.blue,
                "epic" => new Color(0.643f, 0.078f, 0.878f),
                "legendary" => new Color(1f, 0.647f, 0f),
                _ => Color.white
            };
        }
        
        private void OnClick()
        {
            _onClick?.Invoke(_item);
        }
    }
}
