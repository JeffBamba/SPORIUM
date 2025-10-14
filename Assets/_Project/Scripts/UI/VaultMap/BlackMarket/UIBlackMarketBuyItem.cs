using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.BlackMarket
{
    public class UIBlackMarketBuyItem : MonoBehaviour
    {
        [SerializeField] private Button _buyButton;
        
        private TextMeshProUGUI _buyButtonLabel;
        
        public event Action<UIBlackMarketBuyItem> OnBuy;
        
        private void Awake()
        {
            _buyButtonLabel = _buyButton.GetComponentInChildren<TextMeshProUGUI>();
            
            _buyButton.onClick.AddListener(HandleBuy);
        }

        private void HandleBuy()
        {
            OnBuy?.Invoke(this);
        }

        public void SetData(int price)
        {
            _buyButtonLabel.text = $"Buy ({price} CRY)";
        }
    }
}