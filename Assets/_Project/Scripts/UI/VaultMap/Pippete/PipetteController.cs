using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project
{
    public class PipetteController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _pipette;
        [SerializeField] private RectTransform _gameArea;

        private bool _isDragging;
        
        private void Update()
        {
            if (!_isDragging)
                return;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _gameArea,
                Input.mousePosition,
                null,
                out var localMouse
            );
            
            _pipette.anchoredPosition = localMouse;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
        }
    }
}