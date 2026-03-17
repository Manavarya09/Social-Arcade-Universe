using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SocialArcade.Unity.Economy
{
    public class EconomyManager : MonoBehaviour
    {
        private static EconomyManager _instance;
        public static EconomyManager Instance => _instance;
        
        [Header("Current Currency")]
        [SerializeField] private int _coins;
        [SerializeField] private int _gems;
        [SerializeField] private int _premiumGems;
        
        public int Coins => _coins;
        public int Gems => _gems;
        public int PremiumGems => _premiumGems;
        
        private List<ShopItem> _shopItems = new();
        private List<InventoryItem> _inventoryItems = new();
        
        public event Action<CurrencyData> OnCurrencyChanged;
        public event Action<List<ShopItem>> OnShopItemsLoaded;
        public event Action<List<InventoryItem>> OnInventoryLoaded;
        public event Action<ShopItem> OnItemPurchased;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            Core.GameEvents.OnCurrencyUpdated.AddListener(UpdateCurrency);
        }
        
        public async Task LoadCurrencyAsync()
        {
            try
            {
                var response = await Networking.NetworkManager.Instance.GetCurrencyAsync();
                if (response.success)
                {
                    var data = JsonUtility.FromJson<CurrencyDataResponse>(response.rawJson);
                    if (data != null)
                    {
                        UpdateCurrency(new Core.CurrencyData
                        {
                            Coins = data.data.coins,
                            Gems = data.data.gems,
                            PremiumGems = data.data.premiumGems
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load currency: {ex.Message}");
            }
        }
        
        private void UpdateCurrency(Core.CurrencyData currency)
        {
            _coins = currency.Coins;
            _gems = currency.Gems;
            _premiumGems = currency.PremiumGems;
            
            OnCurrencyChanged?.Invoke(currency);
        }
        
        public async Task LoadShopItemsAsync(string type = null)
        {
            try
            {
                var response = await Networking.NetworkManager.Instance.GetShopItemsAsync(type);
                if (response.success)
                {
                    var items = ParseShopItems(response.rawJson);
                    _shopItems = items;
                    OnShopItemsLoaded?.Invoke(items);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load shop items: {ex.Message}");
            }
        }
        
        public async Task<bool> PurchaseItemAsync(string itemId)
        {
            try
            {
                var response = await Networking.NetworkManager.Instance.PurchaseItemAsync(itemId);
                if (response.success)
                {
                    var item = _shopItems.Find(i => i.itemId == itemId);
                    if (item != null)
                    {
                        OnItemPurchased?.Invoke(item);
                    }
                    
                    await LoadCurrencyAsync();
                    await LoadInventoryAsync();
                    
                    return true;
                }
                
                if (response.error != null)
                {
                    Core.GameEvents.OnError.Invoke(response.error.message);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Purchase failed: {ex.Message}");
                return false;
            }
        }
        
        public bool CanAfford(int coins = 0, int gems = 0)
        {
            return _coins >= coins && _gems >= gems;
        }
        
        public async Task LoadInventoryAsync()
        {
            try
            {
                var response = await Networking.NetworkManager.Instance.GetInventoryAsync();
                if (response.success)
                {
                    var items = ParseInventoryItems(response.rawJson);
                    _inventoryItems = items;
                    OnInventoryLoaded?.Invoke(items);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load inventory: {ex.Message}");
            }
        }
        
        public void AddCoins(int amount)
        {
            _coins += amount;
            OnCurrencyChanged?.Invoke(new Core.CurrencyData
            {
                Coins = _coins,
                Gems = _gems,
                PremiumGems = _premiumGems
            });
        }
        
        public void AddGems(int amount)
        {
            _gems += amount;
            OnCurrencyChanged?.Invoke(new Core.CurrencyData
            {
                Coins = _coins,
                Gems = _gems,
                PremiumGems = _premiumGems
            });
        }
        
        private List<ShopItem> ParseShopItems(string json)
        {
            return new List<ShopItem>();
        }
        
        private List<InventoryItem> ParseInventoryItems(string json)
        {
            return new List<InventoryItem>();
        }
    }
    
    [Serializable]
    public class ShopItem
    {
        public string id;
        public string itemId;
        public string name;
        public string description;
        public string type;
        public int priceCoins;
        public int priceGems;
        public string rarity;
        public string imageUrl;
    }
    
    [Serializable]
    public class InventoryItem
    {
        public string id;
        public string itemId;
        public string itemType;
        public int quantity;
        public bool isEquipped;
    }
    
    [Serializable]
    public class CurrencyDataResponse
    {
        public CurrencyDataInner data;
    }
    
    [Serializable]
    public class CurrencyDataInner
    {
        public int coins;
        public int gems;
        public int premiumGems;
    }
}
