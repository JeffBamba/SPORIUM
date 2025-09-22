using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project
{
    public class HUDInventoryItem : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _scoreLabel;

        [SerializeField] private Color _normalColor;
        [SerializeField] private Color _selectedColor;
        
        private Image _image;
        
        public event Action<HUDInventoryItem> OnClick;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }
        
        public void SetItem(string itemName, int score)
        {
            _nameLabel.text = itemName;
            _scoreLabel.text = score.ToString();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(this);
        }

        public void Deselect()
        {
            _image.color = _normalColor;    
        }

        public void Select()
        {
            _image.color = _selectedColor;
        }
    }
}