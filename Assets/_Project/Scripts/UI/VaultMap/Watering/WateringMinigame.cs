using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Watering
{
    public class WateringMinigame : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _wateringLabel;
        [SerializeField] private TextMeshProUGUI _coverageLabel;
        
        [SerializeField] private int _textureSize;
        [SerializeField] private int _brushSize;
        [SerializeField] private Color _wetColor;
        [SerializeField] private Color _dryColor;

        [SerializeField] private Button _finishButton;
        
        private float _waterAmount;
        private float _coverageAmount;
        
        private RawImage _soilImage;
        private Texture2D _soilTexture;
        private RectTransform _rectTransform;

        private PotSlot _pot;
        
        private void Awake()
        {
            _soilImage = GetComponentInChildren<RawImage>();
        }
        
        private void Start()
        {
            _rectTransform = _soilImage.rectTransform;
            _finishButton.onClick.AddListener(HandleFinish);
            
            Reset();
        }

        
        private void HandleFinish()
        {
            if (_coverageAmount > 50f)
                _pot.PotActions.DoWater();
            Hide();
        }

        private void Update()
        {
            _coverageAmount = CalculateCoverage();
            
            _wateringLabel.text = $"Water amount: {(int)_waterAmount}%";
            _coverageLabel.text = $"Coverage: {(int)_coverageAmount}%";
            
            PaintUpdate();
        }

        private void PaintUpdate()
        {
            if (_waterAmount < 0.5f)
                return;
            
            if (!Input.GetMouseButton(0)) 
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform, Input.mousePosition, null, out var localPoint)) 
                return;
            
            var xNorm = (localPoint.x / _rectTransform.rect.width) + 0.5f;
            var yNorm = (localPoint.y / _rectTransform.rect.height) + 0.5f;

            var px = Mathf.RoundToInt(xNorm * _textureSize);
            var py = Mathf.RoundToInt(yNorm * _textureSize);

            _waterAmount -= 0.5f;   
            
            PaintCircle(px, py);
        }

        private void PaintCircle(int centerX, int centerY)
        {
            var radius = _brushSize / 2;

            for (var x = -radius; x < radius; x++)
                for (var y = -radius; y < radius; y++)
                {
                    var px = centerX + x;
                    var py = centerY + y;

                    if (px < 0 || px >= _textureSize || py < 0 || py >= _textureSize)
                        continue;
                    
                    float dist = x * x + y * y;
                    if (dist <= radius * radius)
                        _soilTexture.SetPixel(px, py, _wetColor);
                }
            
            _soilTexture.Apply();
        }

        private float CalculateCoverage()
        {
            var pixels = _soilTexture.GetPixels();
            var wetCount = pixels.Count(c => IsCloseTo(c, _wetColor, 0.02f));
            return (float)wetCount / pixels.Length * 100f;
        }
        
        private static bool IsCloseTo(Color a, Color b, float tolerance)
        {
            return Mathf.Abs(a.r - b.r) < tolerance &&
                   Mathf.Abs(a.g - b.g) < tolerance &&
                   Mathf.Abs(a.b - b.b) < tolerance;
        }

        public void Reset()
        {
            ResetTexture();
            
            _waterAmount = 100;
            _coverageAmount = 0;
        }

        private void ResetTexture()
        {
            _soilTexture = new Texture2D(_textureSize, _textureSize, TextureFormat.RGBA32, false);
            
            var pixels = new Color[_textureSize * _textureSize];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = _dryColor;
            
            _soilTexture.SetPixels(pixels);
            _soilTexture.Apply();

            _soilImage.texture = _soilTexture;   
        }
        
        public void Show(PotSlot pot)
        {
            gameObject.SetActive(true);
            
            Reset();
            _pot = pot;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}