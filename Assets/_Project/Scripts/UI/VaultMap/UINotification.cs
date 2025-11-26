using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace _Project
{
    public class UINotification : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _notificationText;
        [SerializeField] private TextMeshProUGUI _bannerLabel;

        private struct Notification
        {
            public string Message;
            public float Duration;
            public Color Color;
        }
        
        private readonly Queue<Notification> _queue = new();
        private Coroutine _coroutine;

        public void ShowBanner(string banner, Color color, out System.Action clearCallback)
        {
            _bannerLabel.text = banner;
            _bannerLabel.color = color;
            
            clearCallback = () => {
                _bannerLabel.text = "";
            };
        }
        
        public void ShowNotification(string notification, float duration, Color color)
        {
            _queue.Enqueue(new Notification()
            {
                Message = notification,
                Duration = duration,
                Color = color
            });
        }
        
        /// <summary>
        /// Cancella immediatamente la notifica corrente
        /// </summary>
        public void ClearNotification()
        {
            if (_notificationText != null)
            {
                _notificationText.text = "";
            }
        }

        private IEnumerator NotificationRoutine()
        {
            while (true)
            {
                if (_queue.Count <= 0)
                {
                    yield return null;
                    continue;
                }
                
                var notification = _queue.Dequeue();
                
                _notificationText.text = notification.Message;
                _notificationText.color = notification.Color; 
                yield return new WaitForSeconds(notification.Duration);
                
                _notificationText.text = "";
            }
        }

        private void Awake()
        {
            _coroutine = StartCoroutine(NotificationRoutine());
        }
    }
}