using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.Wikipedia
{
    public class WikipediaCategoryTab : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Color _selectedColor;
        [SerializeField] private Color _normalColor;

        private Image _image;

        public event Action OnClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke();
        }

        public void Select()
        {
            _image.color = _selectedColor;
        }

        public void Deselect()
        {
            _image.color = _normalColor;
        }

        private void Awake()
        {
            _image = GetComponentInChildren<Image>();
        }
    }
}