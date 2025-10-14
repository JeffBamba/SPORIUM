using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.BlackMarket
{
    public class UIBlackMarketSellItem : MonoBehaviour
    {
        [SerializeField] private Button _sellButton;
        [SerializeField] private Button _sellAllButton;

        private TextMeshProUGUI _sellButtonLabel;
        private TextMeshProUGUI _sellAllButtonLabel;
        
        public event Action<UIBlackMarketSellItem> OnSellOne;
        public event Action<UIBlackMarketSellItem> OnSellAll;
        
        private void Awake()
        {
            _sellButtonLabel = _sellButton.GetComponentInChildren<TextMeshProUGUI>();
            _sellAllButtonLabel = _sellAllButton.GetComponentInChildren<TextMeshProUGUI>();
            
            _sellButton.onClick.AddListener(HandleSellOne);
            _sellAllButton.onClick.AddListener(HandleSellAll);
        }

        private void HandleSellOne()
        {
            OnSellOne?.Invoke(this);
        }

        private void HandleSellAll()
        {
            OnSellAll?.Invoke(this);   
        }

        public void SetData(int price, int allPrice)
        {
            _sellButtonLabel.text = $"Sell x1 ({price} CRY)";
            _sellAllButtonLabel.text = $"Sell All ({allPrice} CRY)";
        }
    }
}