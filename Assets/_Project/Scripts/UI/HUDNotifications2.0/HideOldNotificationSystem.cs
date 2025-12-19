using UnityEngine;

namespace _Project.UI.HUDNotifications2_0
{
    /// <summary>
    /// Script per nascondere il sistema vecchio ToastNotificationSystem
    /// Mantiene il sistema funzionante ma lo nasconde alla vista
    /// </summary>
    public class HideOldNotificationSystem : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Se true, disattiva il GameObject. Se false, lo sposta fuori schermo")]
        [SerializeField] private bool disableGameObject = true;
        
        [Tooltip("Se disableGameObject è false, questa è la posizione fuori schermo")]
        [SerializeField] private Vector2 offscreenPosition = new Vector2(10000f, 10000f);
        
        private void Start()
        {
            HideSystem();
        }
        
        private void HideSystem()
        {
            if (disableGameObject)
            {
                // Disattiva il GameObject (mantiene funzionante ma invisibile)
                gameObject.SetActive(false);
            }
            else
            {
                // Sposta fuori schermo
                var rectTransform = GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = offscreenPosition;
                }
                else
                {
                    // Fallback: disattiva se non ha RectTransform
                    gameObject.SetActive(false);
                }
            }
        }
    }
}

