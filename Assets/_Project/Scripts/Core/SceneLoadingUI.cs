using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneLoadingUI : MonoBehaviour
{
    [Header("Scene Configuration")]
    [SerializeField] private string targetScene = "SCN_VaultMap";
    [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;
    [SerializeField] private bool allowSceneActivation = true;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private UnityEngine.UI.Image loadingBarFill;
    [SerializeField] private GameObject loadingPanel;
    
    [Header("Loading Settings")]
    [SerializeField] private float minLoadingTime = 1f;
    [SerializeField] private bool showProgressPercentage = true;
    [SerializeField] private bool showProgressBar = true;
    [SerializeField] private string[] loadingMessages = {
        "Caricamento...",
        "Preparazione scena...",
        "Caricamento asset...",
        "Quasi pronto..."
    };
    
    [Header("Validation")]
    [SerializeField] private bool validateOnStart = true;
    [SerializeField] private bool showDebugLogs = true;

    private AsyncOperation loadingOperation;
    private bool isLoading = false;
    private float loadingStartTime;

    void Start()
    {
        if (validateOnStart)
        {
            if (!ValidateConfiguration())
            {
                Debug.LogError("[SceneLoadingUI] Configurazione non valida! SceneLoadingUI disabilitato.");
                enabled = false;
                return;
            }
        }
        
        InitializeSceneLoading();
    }

    private bool ValidateConfiguration()
    {
        bool isValid = true;
        
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[SceneLoadingUI] Nome scena target non specificato!");
            isValid = false;
        }
        
        if (minLoadingTime < 0)
        {
            Debug.LogWarning("[SceneLoadingUI] minLoadingTime non può essere negativo. Impostato a 0.");
            minLoadingTime = 0;
        }
        
        return isValid;
    }

    private void InitializeSceneLoading()
    {
        // Verifica se la scena target è nelle Build Settings
        if (!IsSceneInBuildSettings(targetScene))
        {
            Debug.LogError($"[SceneLoadingUI] Scena '{targetScene}' non trovata nelle Build Settings!");
            enabled = false;
            return;
        }
        
        // Nascondi pannello di loading inizialmente
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[SceneLoadingUI] SceneLoadingUI inizializzato per scena: {targetScene}");
        }
    }

    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameFromPath == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    public void LoadTargetScene()
    {
        if (isLoading)
        {
            Debug.LogWarning("[SceneLoadingUI] Caricamento già in corso!");
            return;
        }
        
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[SceneLoadingUI] Nome scena target non specificato!");
            return;
        }
        
        StartCoroutine(LoadSceneAsync());
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning("[SceneLoadingUI] Caricamento già in corso!");
            return;
        }
        
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoadingUI] Nome scena non può essere vuoto!");
            return;
        }
        
        if (!IsSceneInBuildSettings(sceneName))
        {
            Debug.LogError($"[SceneLoadingUI] Scena '{sceneName}' non trovata nelle Build Settings!");
            return;
        }
        
        targetScene = sceneName;
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        isLoading = true;
        loadingStartTime = Time.time;
        
        // Mostra pannello di loading
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        // Inizia caricamento asincrono
        loadingOperation = SceneManager.LoadSceneAsync(targetScene, loadMode);
        loadingOperation.allowSceneActivation = allowSceneActivation;
        
        if (showDebugLogs)
        {
            Debug.Log($"[SceneLoadingUI] Iniziato caricamento scena: {targetScene}");
        }
        
        // Loop di caricamento
        while (!loadingOperation.isDone)
        {
            UpdateLoadingUI();
            yield return null;
        }
        
        // Assicurati che sia passato il tempo minimo
        float elapsedTime = Time.time - loadingStartTime;
        if (elapsedTime < minLoadingTime)
        {
            yield return new WaitForSeconds(minLoadingTime - elapsedTime);
        }
        
        // Nascondi pannello di loading
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        isLoading = false;
        
        if (showDebugLogs)
        {
            Debug.Log($"[SceneLoadingUI] Caricamento completato per scena: {targetScene}");
        }
    }

    private void UpdateLoadingUI()
    {
        if (loadingOperation == null) return;
        
        float progress = loadingOperation.progress;
        
        // Aggiorna barra di progresso
        if (showProgressBar && loadingBarFill != null)
        {
            loadingBarFill.fillAmount = Mathf.Clamp01(progress / 0.9f);
        }
        
        // Aggiorna testo di loading
        if (loadingText != null)
        {
            string message = GetLoadingMessage(progress);
            
            if (showProgressPercentage)
            {
                message += $" {progress * 100f:0}%";
            }
            
            loadingText.text = message;
        }
    }

    private string GetLoadingMessage(float progress)
    {
        if (loadingMessages == null || loadingMessages.Length == 0)
        {
            return "Caricamento...";
        }
        
        int messageIndex = Mathf.Clamp(Mathf.FloorToInt(progress * loadingMessages.Length), 0, loadingMessages.Length - 1);
        return loadingMessages[messageIndex];
    }

    public void SetTargetScene(string sceneName)
    {
        if (!isLoading)
        {
            targetScene = sceneName;
        }
        else
        {
            Debug.LogWarning("[SceneLoadingUI] Non è possibile cambiare scena durante il caricamento!");
        }
    }

    public void SetMinLoadingTime(float time)
    {
        minLoadingTime = Mathf.Max(0, time);
    }

    public void SetShowProgressPercentage(bool show)
    {
        showProgressPercentage = show;
    }

    public void SetShowProgressBar(bool show)
    {
        showProgressBar = show;
    }

    public void SetLoadingMessages(string[] messages)
    {
        loadingMessages = messages;
    }

    public bool IsLoading => isLoading;
    public float LoadingProgress => loadingOperation != null ? loadingOperation.progress : 0f;
    public string CurrentTargetScene => targetScene;

    // Metodo per fermare il caricamento (utile per debugging)
    public void StopLoading()
    {
        if (isLoading && loadingOperation != null)
        {
            loadingOperation.allowSceneActivation = false;
            StopAllCoroutines();
            
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
            
            isLoading = false;
            
            if (showDebugLogs)
            {
                Debug.Log("[SceneLoadingUI] Caricamento fermato manualmente.");
            }
        }
    }

    // Metodo per ricaricare la scena corrente
    public void ReloadCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadScene(currentScene);
    }

    void OnDestroy()
    {
        if (isLoading)
        {
            StopAllCoroutines();
        }
    }
}
