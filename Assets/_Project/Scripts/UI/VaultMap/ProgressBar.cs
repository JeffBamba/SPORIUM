using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    [DefaultExecutionOrder(-1)]
    public class ProgressBar : MonoBehaviour
    {
        public float Value
        {
            get => _value;
            set
            {
                _value = value;
                
                var offset = _value * _maxWidth;
                _bar.rectTransform.localPosition = new Vector3((-_maxWidth + offset) / 2, 0, 0);
                _bar.rectTransform.sizeDelta = new Vector2(offset, _bar.rectTransform.sizeDelta.y);

                if (_percentageText)
                    _percentageText.text = $"{(int)(value * 100)}%";
            }
        }
        
        private float _value;
        private float _maxWidth;
        private Image _bar;
        private TextMeshProUGUI _percentageText;
        
        private void Awake()
        {
            _percentageText = transform.GetComponentInChildren<TextMeshProUGUI>(); 
            _bar = transform.GetChild(0).GetComponent<Image>();
            _maxWidth = _bar.rectTransform.sizeDelta.x;
        }
    }
}