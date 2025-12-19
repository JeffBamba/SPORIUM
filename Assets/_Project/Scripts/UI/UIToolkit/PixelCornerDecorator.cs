using UnityEngine;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit
{
    /// <summary>
    /// Componente decorativo per creare pixel art corners agli angoli del pannello.
    /// I corners sono già definiti in UXML, questo script gestisce solo la logica se necessaria.
    /// </summary>
    public class PixelCornerDecorator : MonoBehaviour
    {
        [Header("Corner Configuration")]
        [SerializeField] private Color _cornerColor = new Color(0.498f, 1f, 0.478f, 1f); // #7FFF7A
        
        private UIDocument _uiDocument;
        private VisualElement _root;
        
        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }
        
        private void Start()
        {
            if (_uiDocument != null)
            {
                _root = _uiDocument.rootVisualElement;
                UpdateCornerColors();
            }
        }
        
        /// <summary>
        /// Aggiorna i colori dei corners (se necessario runtime).
        /// I corners sono già stilizzati via USS, questo metodo è per override dinamico.
        /// </summary>
        private void UpdateCornerColors()
        {
            if (_root == null) return;
            
            var topLeft = _root.Q<VisualElement>("corner-top-left");
            var topRight = _root.Q<VisualElement>("corner-top-right");
            var bottomLeft = _root.Q<VisualElement>("corner-bottom-left");
            var bottomRight = _root.Q<VisualElement>("corner-bottom-right");
            
            if (topLeft != null) topLeft.style.backgroundColor = new StyleColor(_cornerColor);
            if (topRight != null) topRight.style.backgroundColor = new StyleColor(_cornerColor);
            if (bottomLeft != null) bottomLeft.style.backgroundColor = new StyleColor(_cornerColor);
            if (bottomRight != null) bottomRight.style.backgroundColor = new StyleColor(_cornerColor);
        }
        
        /// <summary>
        /// Imposta un nuovo colore per i corners.
        /// </summary>
        public void SetCornerColor(Color color)
        {
            _cornerColor = color;
            UpdateCornerColors();
        }
    }
}

