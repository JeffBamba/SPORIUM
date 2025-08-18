using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sporae.Core
{
    public class BootstrapLoader : MonoBehaviour
    {
        [SerializeField] private string firstScene = "SCN_Dome_Main";

        void Start()
        {
            var active = SceneManager.GetActiveScene().name;
            if (active != firstScene)
            {
                SceneManager.LoadScene(firstScene, LoadSceneMode.Single);
            }
        }
    }
}
