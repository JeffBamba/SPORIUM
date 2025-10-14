using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.NightResearch
{
    public class NightResearchOption : MonoBehaviour, IPointerClickHandler
    {
        public enum OptionTypes
        {
            HistoricalArchive,
            BotanicalDatabase,
            VaultProtocols
        }
        
        [SerializeField] private Color _selectedColor;
        [SerializeField] private Color _normalColor;

        [field: SerializeField] public OptionTypes OptionType { get; private set; }
        
        private Image _image;

        public event Action<NightResearchOption> OnClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(this);
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