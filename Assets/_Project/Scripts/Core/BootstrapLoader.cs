using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    [SerializeField] private string firstScene = "SCN_Dome_Main";

    void Start()
    {
        // Se c'è già SceneLoadingUI attivo, lascia fare a lei
        if (FindObjectOfType<SceneLoadingUI>() != null) return;

        // Altrimenti fai il load “classico”
        if (SceneManager.GetActiveScene().name != firstScene)
            SceneManager.LoadScene(firstScene, LoadSceneMode.Single);
    }
}
