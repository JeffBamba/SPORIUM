using System.Collections;
using TMPro;
using UnityEngine;

namespace _Project
{
    public class UINotification : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _notificationText;

        public void ShowNotification(string notification, float duration)
        {
            _notificationText.text = notification;
            StartCoroutine(NotificationClearRoutine(duration));
        }

        private IEnumerator NotificationClearRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            _notificationText.text = "";
        }
    }
}