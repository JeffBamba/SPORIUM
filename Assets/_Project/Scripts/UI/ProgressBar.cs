using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class ProgressBar : MonoBehaviour
    {
        public float Value
        {
            get => _value;
            set
            {
                _value = value;

                Debug.Log($"Set: {value}");
                
                var offset = _value * _maxWidth;
                _bar.rectTransform.localPosition = new Vector3((-_maxWidth + offset) / 2, 0, 0);
                _bar.rectTransform.sizeDelta = new Vector2(offset, _bar.rectTransform.sizeDelta.y);
            }
        }
        
        private float _value;
        private float _maxWidth;
        private Image _bar; 
        
        private void Awake()
        {
            _bar = transform.GetChild(0).GetComponent<Image>();
            _maxWidth = _bar.rectTransform.sizeDelta.x;
        }
    }
}