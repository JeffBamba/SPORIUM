using System.Collections;
using TMPro;
using UnityEngine;

namespace _Project
{
    public class UINotification : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _notificationText;

        private Coroutine _coroutine;
        
        public void ShowNotification(string notification, float duration, Color color)
        {
            _notificationText.text = notification;
            
            if (_coroutine != null)
                StopCoroutine(_coroutine);
            
            _notificationText.color = color;
            _coroutine = StartCoroutine(NotificationClearRoutine(duration));
        }

        private IEnumerator NotificationClearRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            _notificationText.text = "";
        }
    }
}