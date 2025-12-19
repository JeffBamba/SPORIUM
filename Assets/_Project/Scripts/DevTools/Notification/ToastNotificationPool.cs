using System.Collections.Generic;
using UnityEngine;
using Sporae.DevTools;

namespace Sporae.DevTools
{
    /// <summary>
    /// Sistema di object pooling per toast UI
    /// Evita Instantiate/Destroy continuo migliorando performance
    /// </summary>
    public class ToastNotificationPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private ToastNotificationUIItem _prefab;
        [SerializeField] private int _initialPoolSize = 10;
        
        private Queue<ToastNotificationUIItem> _pool = new Queue<ToastNotificationUIItem>();
        private List<ToastNotificationUIItem> _activeItems = new List<ToastNotificationUIItem>();
        
        private void Awake()
        {
            // Pre-warm pool
            for (int i = 0; i < _initialPoolSize; i++)
            {
                var item = CreateNewItem();
                ReturnToPool(item);
            }
        }
        
        /// <summary>
        /// Ottiene un toast dal pool (o ne crea uno nuovo se pool vuoto)
        /// </summary>
        public ToastNotificationUIItem GetFromPool()
        {
            ToastNotificationUIItem item;
            
            if (_pool.Count > 0)
            {
                item = _pool.Dequeue();
            }
            else
            {
                item = CreateNewItem();
            }
            
            item.gameObject.SetActive(true);
            _activeItems.Add(item);
            return item;
        }
        
        /// <summary>
        /// Restituisce un toast al pool
        /// </summary>
        public void ReturnToPool(ToastNotificationUIItem item)
        {
            if (item == null) return;
            
            _activeItems.Remove(item);
            item.ReturnToPool();
            item.transform.SetParent(transform);
            _pool.Enqueue(item);
        }
        
        private ToastNotificationUIItem CreateNewItem()
        {
            if (_prefab == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "ToastNotificationPool: Prefab non assegnato!");
                return null;
            }
            
            var instance = Instantiate(_prefab, transform);
            instance.gameObject.SetActive(false);
            return instance;
        }
        
        /// <summary>
        /// Ottiene la lista di tutti i toast attivi
        /// </summary>
        public List<ToastNotificationUIItem> GetActiveItems() => _activeItems;
        
        /// <summary>
        /// Ottiene il numero di item nel pool
        /// </summary>
        public int PoolCount => _pool.Count;
        
        /// <summary>
        /// Ottiene il numero di item attivi
        /// </summary>
        public int ActiveCount => _activeItems.Count;
    }
}

