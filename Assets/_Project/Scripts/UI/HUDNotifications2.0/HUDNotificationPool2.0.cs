using System.Collections.Generic;
using UnityEngine;
using _Project.UI.HUDNotifications2_0;

namespace _Project.UI.HUDNotifications2_0
{
    /// <summary>
    /// Sistema di object pooling per HUD Notifications 2.0
    /// Evita Instantiate/Destroy continuo migliorando performance
    /// Pre-warm con 5-10 item iniziali
    /// </summary>
    public class HUDNotificationPool2_0 : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private HUDNotificationItem2_0 _prefab;
        [SerializeField] private int _initialPoolSize = 8; // Pre-warm con 8 item (tra 5-10)
        
        private Queue<HUDNotificationItem2_0> _pool = new Queue<HUDNotificationItem2_0>();
        private List<HUDNotificationItem2_0> _activeItems = new List<HUDNotificationItem2_0>();
        
        private void Awake()
        {
            // Pre-warm pool
            if (_prefab != null)
            {
                for (int i = 0; i < _initialPoolSize; i++)
                {
                    var item = CreateNewItem();
                    if (item != null)
                        ReturnToPool(item);
                }
            }
        }
        
        /// <summary>
        /// Ottiene una notifica dal pool (o ne crea una nuova se pool vuoto)
        /// </summary>
        public HUDNotificationItem2_0 GetFromPool()
        {
            HUDNotificationItem2_0 item;
            
            if (_pool.Count > 0)
            {
                item = _pool.Dequeue();
            }
            else
            {
                item = CreateNewItem();
            }
            
            if (item != null)
            {
                item.gameObject.SetActive(true);
                _activeItems.Add(item);
            }
            
            return item;
        }
        
        /// <summary>
        /// Restituisce una notifica al pool
        /// </summary>
        public void ReturnToPool(HUDNotificationItem2_0 item)
        {
            if (item == null) return;
            
            _activeItems.Remove(item);
            item.ResetForPool();
            item.transform.SetParent(transform);
            item.gameObject.SetActive(false);
            _pool.Enqueue(item);
        }
        
        private HUDNotificationItem2_0 CreateNewItem()
        {
            if (_prefab == null)
            {
                Debug.LogError("[HUDNotificationPool2.0] Prefab non assegnato!");
                return null;
            }
            
            var instance = Instantiate(_prefab, transform);
            instance.gameObject.SetActive(false);
            return instance;
        }
        
        /// <summary>
        /// Ottiene la lista di tutte le notifiche attive
        /// </summary>
        public List<HUDNotificationItem2_0> GetActiveItems() => _activeItems;
        
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

