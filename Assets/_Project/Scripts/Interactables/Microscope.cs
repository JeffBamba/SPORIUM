using UnityEngine;

namespace _Project
{
    public class Microscope : MonoBehaviour
    {
        [SerializeField] private LabMinigame _labMiniGame;
        [SerializeField] private float _interactDistance;

        [SerializeField] private Color _normalColor;
        [SerializeField] private Color _highlightColor;
        
        private Transform _playerTransform;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;
        }
        
        public void OnMouseDown()
        {
            if (UIBlocker.IsPointerOverUI())
                return;
            
            float distance = Vector2.Distance(_playerTransform.position, transform.position);
            if (distance > _interactDistance)
                return;
            
            _labMiniGame.Show();
        }
        
        private void OnMouseEnter()
        {
            _spriteRenderer.color = _highlightColor;
        }
    
        private void OnMouseExit()
        {
            _spriteRenderer.color = _normalColor;
        }
    }
}