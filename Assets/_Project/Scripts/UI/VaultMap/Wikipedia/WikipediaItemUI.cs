using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Wikipedia
{
    public class WikipediaItemUI : MonoBehaviour
    {
        private Button _button;
        private TextMeshProUGUI _titleLabel;
        private Image _icon;
        
        private WikipediaItemData _itemData;

        public Action OnSeeDetails;
        
        public void SetData(WikipediaItemData data)
        {
            _itemData = data;
            
            _titleLabel.text = _itemData.Title;
            _icon.sprite = _itemData.Sprite;
        }

        private void Awake()
        {
            _button = GetComponentInChildren<Button>();
            _titleLabel = GetComponentInChildren<TextMeshProUGUI>();
            _icon = GetComponentInChildren<Image>();
        }

        private void Start()
        {
            _button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            OnSeeDetails?.Invoke();
        }
    }
}