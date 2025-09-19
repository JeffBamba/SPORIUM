using Sporae.Core;
using UnityEngine;

namespace _Project
{
    public class SeedStorage : Storage
    {
        [SerializeField] private HUDInventory _inventoryUI;
        [SerializeField] private SeedStorageUI _seedStorageUI;
        [SerializeField] private float _interactDistance;

        [SerializeField] private Color _normalColor;
        [SerializeField] private Color _highlightColor;
        
        private Transform _playerTransform;
        private SpriteRenderer _spriteRenderer;
        private Inventory _inventory = new Inventory();
        
        public Inventory Storage => _inventory;

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
            
            _inventoryUI.Show();
            _seedStorageUI.Show();
        }
        
        private void OnMouseEnter()
        {
            _spriteRenderer.color = _highlightColor;
        }
    
        private void OnMouseExit()
        {
            _spriteRenderer.color = _normalColor;
        }

        public override Inventory GetInventory()
        {
            return _inventory;
        }
    }
}