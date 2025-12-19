using UnityEngine;
using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Componente decorativo per creare pixel art corners (L-shaped) agli angoli di un pannello.
    /// </summary>
    public class PixelCornersUI
    {
        private VisualElement _parent;
        private Color _cornerColor;
        private int _cornerSize;
        
        private VisualElement _cornerTopLeft;
        private VisualElement _cornerTopRight;
        private VisualElement _cornerBottomLeft;
        private VisualElement _cornerBottomRight;
        
        public PixelCornersUI(VisualElement parent, Color cornerColor, int cornerSize = 3)
        {
            _parent = parent;
            _cornerColor = cornerColor;
            _cornerSize = cornerSize;
            
            CreateCorners();
        }
        
        private void CreateCorners()
        {
            // Top Left
            _cornerTopLeft = new VisualElement();
            _cornerTopLeft.name = "corner-top-left";
            _cornerTopLeft.AddToClassList("pixel-corner");
            _cornerTopLeft.AddToClassList("corner-top-left");
            _parent.Add(_cornerTopLeft);
            
            // Top Right
            _cornerTopRight = new VisualElement();
            _cornerTopRight.name = "corner-top-right";
            _cornerTopRight.AddToClassList("pixel-corner");
            _cornerTopRight.AddToClassList("corner-top-right");
            _parent.Add(_cornerTopRight);
            
            // Bottom Left
            _cornerBottomLeft = new VisualElement();
            _cornerBottomLeft.name = "corner-bottom-left";
            _cornerBottomLeft.AddToClassList("pixel-corner");
            _cornerBottomLeft.AddToClassList("corner-bottom-left");
            _parent.Add(_cornerBottomLeft);
            
            // Bottom Right
            _cornerBottomRight = new VisualElement();
            _cornerBottomRight.name = "corner-bottom-right";
            _cornerBottomRight.AddToClassList("pixel-corner");
            _cornerBottomRight.AddToClassList("corner-bottom-right");
            _parent.Add(_cornerBottomRight);
        }
        
        /// <summary>
        /// Imposta il colore dei corners.
        /// </summary>
        public void SetColor(Color color)
        {
            _cornerColor = color;
            
            if (_cornerTopLeft != null) _cornerTopLeft.style.backgroundColor = new StyleColor(color);
            if (_cornerTopRight != null) _cornerTopRight.style.backgroundColor = new StyleColor(color);
            if (_cornerBottomLeft != null) _cornerBottomLeft.style.backgroundColor = new StyleColor(color);
            if (_cornerBottomRight != null) _cornerBottomRight.style.backgroundColor = new StyleColor(color);
        }
        
        /// <summary>
        /// Imposta la dimensione dei corners.
        /// </summary>
        public void SetSize(int size)
        {
            _cornerSize = size;
            
            if (_cornerTopLeft != null) _cornerTopLeft.style.width = size;
            if (_cornerTopLeft != null) _cornerTopLeft.style.height = size;
            if (_cornerTopRight != null) _cornerTopRight.style.width = size;
            if (_cornerTopRight != null) _cornerTopRight.style.height = size;
            if (_cornerBottomLeft != null) _cornerBottomLeft.style.width = size;
            if (_cornerBottomLeft != null) _cornerBottomLeft.style.height = size;
            if (_cornerBottomRight != null) _cornerBottomRight.style.width = size;
            if (_cornerBottomRight != null) _cornerBottomRight.style.height = size;
        }
    }
}

