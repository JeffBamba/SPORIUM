using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    [Header("Scene Configuration")]
    [SerializeField] private string firstScene = "SCN_VaultMap";
    [SerializeField] private float delayBeforeLoad = 0.1f;
    [SerializeField] private bool loadOnStart = true;
    
    [Header("Validation")]
    [SerializeField] private bool validateOnStart = true;
    [SerializeField] private bool showDebugLogs = true;

    private bool isLoading = false;

    void Start()
    {
        if (validateOnStart)
        {
            if (!ValidateConfiguration())
            {
                Debug.LogError("[BootstrapLoader] Configurazione non valida! BootstrapLoader disabilitato.");
                enabled = false;
                return;
            }
        }
        
        if (loadOnStart)
        {
            StartBootstrap();
        }
    }

    private bool ValidateConfiguration()
    {
        bool isValid = true;
        
        if (string.IsNullOrEmpty(firstScene))
        {
            Debug.LogError("[BootstrapLoader] Nome scena non specificato!");
            isValid = false;
        }
        
        if (delayBeforeLoad < 0)
        {
            Debug.LogWarning("[BootstrapLoader] delayBeforeLoad non può essere negativo. Impostato a 0.");
            delayBeforeLoad = 0;
        }
        
        return isValid;
    }

    public void StartBootstrap()
    {
        if (isLoading)
        {
            Debug.LogWarning("[BootstrapLoader] Bootstrap già in corso!");
            return;
        }
        
        if (string.IsNullOrEmpty(firstScene))
        {
            Debug.LogError("[BootstrapLoader] Nome scena non specificato!");
            return;
        }
        
        StartCoroutine(BootstrapSequence());
    }

    private System.Collections.IEnumerator BootstrapSequence()
    {
        isLoading = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"[BootstrapLoader] Iniziato bootstrap per scena: {firstScene}");
        }
        
        // Delay iniziale per stabilizzare la scena
        if (delayBeforeLoad > 0)
        {
            yield return new WaitForSeconds(delayBeforeLoad);
        }
        
        // Verifica se c'è già un SceneLoadingUI attivo
        SceneLoadingUI existingLoader = FindObjectOfType<SceneLoadingUI>();
        if (existingLoader != null)
        {
            if (showDebugLogs)
            {
                Debug.Log("[BootstrapLoader] SceneLoadingUI trovato, delega il caricamento a lui.");
            }
            
            // Delega il caricamento al SceneLoadingUI esistente
            existingLoader.LoadScene(firstScene);
            isLoading = false;
            yield break;
        }
        
        // Verifica se siamo già nella scena target
        if (SceneManager.GetActiveScene().name == firstScene)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[BootstrapLoader] Già nella scena target: {firstScene}");
            }
            isLoading = false;
            yield break;
        }
        
        // Caricamento diretto se non c'è SceneLoadingUI
        if (showDebugLogs)
        {
            Debug.Log($"[BootstrapLoader] Caricamento diretto scena: {firstScene}");
        }
        
        SceneManager.LoadScene(firstScene, LoadSceneMode.Single);
        
        isLoading = false;
    }

    public void SetFirstScene(string sceneName)
    {
        if (!isLoading)
        {
            firstScene = sceneName;
        }
        else
        {
            Debug.LogWarning("[BootstrapLoader] Non è possibile cambiare scena durante il caricamento!");
        }
    }

    public void SetDelayBeforeLoad(float delay)
    {
        delayBeforeLoad = Mathf.Max(0, delay);
    }

    public void SetLoadOnStart(bool load)
    {
        loadOnStart = load;
    }

    public string GetFirstScene()
    {
        return firstScene;
    }

    public bool IsLoading => isLoading;

    // Metodo per forzare il caricamento immediato
    public void ForceLoad()
    {
        if (isLoading) return;
        
        if (string.IsNullOrEmpty(firstScene))
        {
            Debug.LogError("[BootstrapLoader] Nome scena non specificato!");
            return;
        }
        
        SceneManager.LoadScene(firstScene, LoadSceneMode.Single);
    }

    // Metodo per verificare se la scena target è nelle Build Settings
    public bool IsSceneInBuildSettings()
    {
        if (string.IsNullOrEmpty(firstScene)) return false;
        
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameFromPath == firstScene)
            {
                return true;
            }
        }
        return false;
    }

    // Metodo per ottenere informazioni sul bootstrap
    public string GetBootstrapInfo()
    {
        string info = $"Scena target: {firstScene}\n";
        info += $"Delay: {delayBeforeLoad}s\n";
        info += $"Carica all'avvio: {loadOnStart}\n";
        info += $"In caricamento: {isLoading}\n";
        info += $"Scena nelle Build Settings: {IsSceneInBuildSettings()}";
        
        return info;
    }

    void OnDestroy()
    {
        if (isLoading)
        {
            StopAllCoroutines();
        }
    }
}
