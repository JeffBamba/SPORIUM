using UnityEngine;

namespace Sporae.Core
{
    public class AppRoot : MonoBehaviour
    {
        private static AppRoot _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // Esiste già un AppRoot -> distruggi il duplicato
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
